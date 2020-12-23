using SwishApi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.Extensions.Logging;
using SwishApi.Helpers;

namespace SwishApi
{
    public abstract class SwishClient : ISwishClient
    {
        private readonly string _payeeAlias;
        private readonly string _payeePaymentReference;
        private readonly Uri _callbackUrl;
        private readonly HttpClient _client;
        private readonly ILogger<SwishClient> _logger;
        private readonly HttpMessageHandler _handler;
        private readonly JsonSerializerOptions _options;
        private const string Currency = "SEK";
        
        protected SwishClient(X509Certificate2 certificate, X509Certificate2Collection certificateCollection, Uri callbackUri, string payeeAlias, string payeePaymentReference, Uri endpointUri, ILogger<SwishClient> logger)
        {
            _logger = logger;
            _payeeAlias = payeeAlias;
            _payeePaymentReference = payeePaymentReference;
            _callbackUrl = callbackUri;

            _handler = CreateHttpMessageHandler(certificate, certificateCollection);
            _client = CreateHttpClient(_handler, endpointUri);

            logger.LogInformation("Swish Client Created. Endpoint Uri {EndpointUri}. PayeeAlias {payeeAlias}", endpointUri, payeeAlias);

            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

        }
        
        private static HttpClient CreateHttpClient(HttpMessageHandler messageHandler, Uri baseApiUri)
        {
            var client =  new HttpClient(messageHandler) { BaseAddress = baseApiUri };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private HttpMessageHandler CreateHttpMessageHandler(X509Certificate2 certificate, X509Certificate2Collection certificateCollection)
        {
            var handler = new HttpClientHandler();
            _logger.LogTrace("Adding Client Certificate '{Subject}'. Issuer '{Issuer}'. Has Private Key '{HasPrivateKey}'", certificate.Subject, certificate.Issuer , certificate.HasPrivateKey);
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;

            handler.ClientCertificates.Add(certificate);
            handler.SslProtocols = SslProtocols.Tls12;
            handler.ClientCertificates.AddRange(certificateCollection);

            handler.ServerCertificateCustomValidationCallback += ServerCertificateCustomValidationCallback;
            return handler;
        }

        private bool ServerCertificateCustomValidationCallback(HttpRequestMessage arg1, X509Certificate2 arg2, X509Chain arg3, SslPolicyErrors policyErrors)
        {
            _logger.LogTrace($"Validating Certificate {arg2.Subject}. Errors {policyErrors}");
            return policyErrors == SslPolicyErrors.None; ;
        }
        
        public async Task<(LocationResponse Response, ErrorResponse Error)> MakePaymentRequestAsync(Guid paymentIdentifier, string phoneNumber, decimal amount, string message)
        {
            EnsureArg.IsGt(amount, 0, nameof(amount));
            EnsureArg.IsNotNullOrEmpty(message);
            EnsureArg.IsNotDefault(paymentIdentifier, nameof(paymentIdentifier));
            EnsureArg.IsNotNullOrEmpty(phoneNumber, nameof(phoneNumber));
            EnsureArg.IsNotNullOrEmpty(message, nameof(message));

            try
            {
                var requestData = new PaymentRequest()
                {
                    payeePaymentReference = _payeePaymentReference,
                    callbackUrl = _callbackUrl,
                    payerAlias = phoneNumber,
                    payeeAlias = _payeeAlias,
                    amount = amount.ToSwishAmount(),
                    currency = Currency,
                    message = message
                };
                _logger.LogTrace("Json {Json}", JsonSerializer.Serialize(requestData, _options));
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(_client.BaseAddress, $"swish-cpcapi/api/v2/paymentrequests/{paymentIdentifier}"),
                    Content = new JsonContent(requestData)
                };

                _logger.LogInformation("Sending Request {Verb} {Uri}", httpRequestMessage.Method.ToString(), httpRequestMessage.RequestUri.ToString());

                var response = await _client.SendAsync(httpRequestMessage);

                return await HandleResponseAsync<LocationResponse>(response);

            }
            catch (Exception ex)
            {
                return HandleException<LocationResponse>(ex);
            }
        }

        public async Task<(CheckPaymentRequestStatusResponse Response, ErrorResponse Error)> CancelPaymentAsync(Guid paymentId)
        {
            try
            {
                JsonPatchDocument jsonPatchDocument = new JsonPatchDocument();
                jsonPatchDocument.Operations.Add(new Operation(){op = "replace", path = "/status", value = "cancelled" });
                var url = new Uri(_client.BaseAddress, $"swish-cpcapi/api/v1/paymentrequests/{paymentId}");

                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(jsonPatchDocument, _options), Encoding.UTF8, "application/json-patch+json")
                };

                var response = await _client.SendAsync(httpRequestMessage);

                return await HandleResponseAsync<CheckPaymentRequestStatusResponse>(response);
            }
            catch (Exception ex)
            {
                return HandleException<CheckPaymentRequestStatusResponse>(ex);
            }
        }

        private async Task<(T Response, ErrorResponse Error)> HandleResponseAsync<T>(HttpResponseMessage response) where T : class, new()
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var readAsStringAsync = await response.Content.ReadAsStringAsync();
                    return (JsonSerializer.Deserialize<T>(readAsStringAsync, _options), null);
                case HttpStatusCode.NoContent:
                case HttpStatusCode.Created:
                    if (typeof(LocationResponse).IsAssignableFrom(typeof(T)))
                    {
                        var responseLocation = new T();
                        if (responseLocation is LocationResponse locationResponse)
                        {
                            locationResponse.Location = response.Headers.Location;
                        }

                        return (responseLocation, null);
                    }

                    return (new T(), null);
                case HttpStatusCode.UnprocessableEntity:
                    return await ReturnErrorsAsync<T>(response);
                case HttpStatusCode.InternalServerError:
                {
                    var errorResponse = new ErrorResponse();
                    errorResponse.Errors.Add(new Error() {ErrorCode = "500", ErrorMessage = "Internal Server Error"});
                    return (default, errorResponse);
                }
                default:
                {
                    var responsePayload = await response.Content.ReadAsStringAsync();
                    var errorResponse = new ErrorResponse();
                    errorResponse.Errors.Add(new Error() { ErrorCode = response.StatusCode.ToString(), ErrorMessage = "Unknown error", AdditionalInformation = responsePayload });
                    return (default, errorResponse);
                }
            }
        }

        public async Task<(CheckPaymentRequestStatusResponse Response, ErrorResponse Error)> CheckPaymentStatusAsync(Guid paymentId, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(_client.BaseAddress, $"swish-cpcapi/api/v1/paymentrequests/{paymentId}"));

                var response = await _client.SendAsync(httpRequestMessage, cancellationToken);

                return await HandleResponseAsync<CheckPaymentRequestStatusResponse>(response);
            }
            catch (Exception ex)
            {
                return HandleException<CheckPaymentRequestStatusResponse>(ex);
            }
        }

        public async Task<(LocationResponse Response, ErrorResponse Error)> RefundAsync(Guid paymentId, Guid refundId, decimal amount, string message)
        {
            EnsureArg.IsGt(amount, 0, nameof(amount));
            EnsureArg.IsNotNullOrEmpty(message);
            EnsureArg.IsNotDefault(paymentId, nameof(paymentId));
            EnsureArg.IsNotDefault(refundId, nameof(refundId));
            EnsureArg.IsNotEqualTo(paymentId.ToString(), refundId.ToString());

            try
            {
                var requestData = new RefundData()
                {
                    OriginalPaymentReference = paymentId.ToString(),
                    CallbackUrl = _callbackUrl.ToString(),
                    PayerAlias = _payeeAlias,
                    Amount = amount.ToSwishAmount(),
                    Currency = Currency,
                    Message = message
                };

                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(_client.BaseAddress, $"swish-cpcapi/api/v2/refunds/{refundId}"),
                    Content = new JsonContent(requestData)
                };

                var response = await _client.SendAsync(httpRequestMessage);

                return await HandleResponseAsync<LocationResponse>(response);
            }
            catch (Exception ex)
            {
                return HandleException<LocationResponse>(ex);
            }
        }


        public async Task<(CheckRefundStatusResponse Response, ErrorResponse Error)> CheckRefundStatusAsync(Guid refundId, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_client.BaseAddress, $"swish-cpcapi/api/v1/refunds/{refundId}"),
                };

                var response = await _client.SendAsync(httpRequestMessage, cancellationToken);

                return await HandleResponseAsync<CheckRefundStatusResponse>(response);
            }
            catch (Exception ex)
            {
                return HandleException<CheckRefundStatusResponse>(ex);
            }
        }

        public async Task<(QRCodeResponse Response, ErrorResponse Error)> GetQRCodeAsync(string token, string format = "svg", int size = 300, int border = 0, bool transparent = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var requestData = new QRCodeData()
                {
                    token = token,
                    format = format,
                    size = size,
                    border = border,
                    transparent = transparent
                };

               
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(_client.BaseAddress,"qrg-swish/api/v1/commerce"),
                    Content = new JsonContent(requestData)
                };

                var response = await _client.SendAsync(httpRequestMessage, cancellationToken);

                return await ReturnErrorsAsync<QRCodeResponse>(response);
            }
            catch (Exception ex)
            {
                return HandleException<QRCodeResponse>(ex);
            }
        }

        private async Task<(T Response, ErrorResponse Error)> ReturnErrorsAsync<T>(HttpResponseMessage response)
        {
            var errorResponse = new ErrorResponse();
            errorResponse.Errors.AddRange(JsonSerializer.Deserialize<List<Error>>(await response.Content.ReadAsByteArrayAsync(), _options));
            return (default, errorResponse);
        }

        private (T Response, ErrorResponse Error) HandleException<T>(Exception exception)
        {
            _logger.LogError(exception, "Exception catch");
            var errorResponse = new ErrorResponse();
            errorResponse.Errors.Add(new Error() { ErrorCode = "InternalException", ErrorMessage = exception.Message, Exception = exception });
            return (default, errorResponse);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
