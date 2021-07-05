using EnsureThat;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SwishApi.Helpers;
using SwishApi.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SwishApi
{
    public class SwishClient : ISwishClient
    {
        private static readonly Uri MSSEndpointUri = new Uri("https://mss.cpc.getswish.net");
        private static readonly Uri ProductionEndpointUri = new Uri("https://cpc.getswish.net");
        private const string ApplicationJsonMediaType = "application/json";
        private const string Currency = "SEK";
        private const string SwishNumberFormat = @"CN=(123\d{7})";
        private readonly Uri _callbackUrl;
        private readonly HttpClient _client;
        private readonly HttpMessageHandler _handler;
        private readonly ILogger<ISwishClient> _logger;
        private readonly JsonSerializerOptions _options;
        private readonly string _payeeAlias;
        private bool _useMSS;

        
        public SwishClient(ISwishCertificateProvider swishCertificateProvider, ISwishSettingsProvider settingsProvider, ILogger<ISwishClient> logger = null) :
            this(swishCertificateProvider, settingsProvider?.CallbackUri, logger)
        {
        }

        public SwishClient(ISwishCertificateProvider swishCertificateProvider, Uri callbackUri, ILogger<ISwishClient> logger = null)
        {
            EnsureArg.IsNotNull(swishCertificateProvider);
            EnsureArg.IsNotNull(callbackUri);

            _logger = logger ?? NullLogger<SwishClient>.Instance;
            _logger.LogInformation("Swish Client Initization started");
            _callbackUrl = callbackUri;
            var certificates = swishCertificateProvider.GetSwishCertificates();

            foreach (var certificate in certificates)
            {
                if (certificate.HasPrivateKey && Regex.IsMatch(certificate.Subject, SwishNumberFormat))
                {
                    
                    _payeeAlias = Regex.Match(certificate.Subject, SwishNumberFormat).Groups[1].Value;
                }
            }

            if (string.IsNullOrEmpty(_payeeAlias))
            {
                var exception =  new ArgumentException("Payee Alias not found in certificate");
                _logger.LogError(exception, "Payee Alias not found in certificates");
                throw exception;
            }

            _handler = CreateHttpMessageHandler(certificates);
            _client = CreateHttpClient(_handler, ProductionEndpointUri);

            logger.LogInformation("Swish Client Created. Endpoint Uri {EndpointUri}. PayeeAlias {payeeAlias}", ProductionEndpointUri, _payeeAlias);

            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
        }

        public string PayeeAlias => _payeeAlias;

        public bool UseMSS
        {
            get => _useMSS;
            set
            {
                if (value != _useMSS)
                {
                    if (value)
                    {
                        _client.BaseAddress = MSSEndpointUri;
                        _logger.LogInformation("Swish Client Changed to MSS Endpoint");
                    }
                    else
                    {
                        _client.BaseAddress = ProductionEndpointUri;
                        _logger.LogInformation("Swish Client Changed to Production Endpoint");
                    }
                    _useMSS = value;
                }
            }
        }

        public async Task<(CheckPaymentRequestStatusResponse Response, ErrorResponse Error)> CancelPaymentAsync(Guid paymentId)
        {
            try
            {
                JsonPatchDocument jsonPatchDocument = new JsonPatchDocument();
                jsonPatchDocument.Operations.Add(new Operation() { op = "replace", path = "/status", value = "cancelled" });
                var url = new Uri(_client.BaseAddress, $"swish-cpcapi/api/v1/paymentrequests/{paymentId.ToSwishId()}");

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

        public async Task<(CheckPaymentRequestStatusResponse Response, ErrorResponse Error)> CheckPaymentStatusAsync(Guid paymentId, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(_client.BaseAddress, $"swish-cpcapi/api/v1/paymentrequests/{paymentId.ToSwishId()}"));

                var response = await _client.SendAsync(httpRequestMessage, cancellationToken);

                return await HandleResponseAsync<CheckPaymentRequestStatusResponse>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                return HandleException<CheckPaymentRequestStatusResponse>(ex);
            }
        }

        public async Task<(CheckRefundStatusResponse Response, ErrorResponse Error)> CheckRefundStatusAsync(Guid refundId, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_client.BaseAddress, $"swish-cpcapi/api/v1/refunds/{refundId.ToSwishId()}"),
                };

                var response = await _client.SendAsync(httpRequestMessage, cancellationToken);

                return await HandleResponseAsync<CheckRefundStatusResponse>(response);
            }
            catch (Exception ex)
            {
                return HandleException<CheckRefundStatusResponse>(ex);
            }
        }

        /// <inheritdoc/>
        public async Task<(LocationResponse Response, ErrorResponse Error)> PaymentRequestAsync(Guid paymentIdentifier, string payerAlias, decimal amount, string message, string payeePaymentReference, Uri callbackUrl = null)
        {
            EnsureArg.IsGt(amount, 0, nameof(amount));
            EnsureArg.IsNotNullOrEmpty(message);
            EnsureArg.IsNotDefault(paymentIdentifier, nameof(paymentIdentifier));
            EnsureArg.IsNotNullOrEmpty(payerAlias, nameof(payerAlias));
            EnsureArg.HasLengthBetween(payerAlias, 8, 15);
            EnsureArg.IsNotNullOrEmpty(message, nameof(message));
            
            try
            {
                var requestData = new PaymentRequest()
                {
                    payeePaymentReference = payeePaymentReference,
                    callbackUrl = callbackUrl ?? _callbackUrl,
                    payerAlias = payerAlias,
                    payeeAlias = _payeeAlias,
                    amount = amount.ToSwishAmount(),
                    currency = Currency,
                    message = message
                };
                _logger.LogDebug("Json {Json}", JsonSerializer.Serialize(requestData, _options));
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(_client.BaseAddress, $"swish-cpcapi/api/v2/paymentrequests/{paymentIdentifier.ToSwishId()}"),
                    Content = new StringContent(JsonSerializer.Serialize(requestData, _options), Encoding.UTF8, ApplicationJsonMediaType),
                };

                _logger.LogInformation("Sending Request {Verb} {Uri}", httpRequestMessage.Method.ToString(), httpRequestMessage.RequestUri.ToString());

                var response = await _client.SendAsync(httpRequestMessage);

                return await HandleResponseAsync<LocationResponse>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to send or handle Swish request : {ex.Message}");
                if(ex.InnerException != null)
                {
                    _logger.LogError(ex, $"Inner exception : {ex.InnerException.Message}");
                }
                return HandleException<LocationResponse>(ex);
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
                var checkPaymentResponse = await CheckPaymentStatusAsync(paymentId);

                if (checkPaymentResponse.Error != null)
                {
                    return (null, checkPaymentResponse.Error);
                }

                if (checkPaymentResponse.Response.Status != PaymentStatus.PAID)
                {
                    var errors = new ErrorResponse(new Error() { ErrorCode = "JBS1", ErrorMessage = "Payment not in status PAID" });
                    return (null, errors);
                }

                var requestData = new RefundData()
                {
                    OriginalPaymentReference = checkPaymentResponse.Response.PaymentReference,
                    CallbackUrl = _callbackUrl.ToString(),
                    PayerAlias = _payeeAlias,
                    Amount = amount.ToSwishAmount(),
                    Currency = Currency,
                    Message = message
                };

                var uri = new Uri(_client.BaseAddress, $"swish-cpcapi/api/v2/refunds/{refundId.ToSwishId()}");
                _logger.LogInformation("Uri {Uri} Json {Json}", uri, JsonSerializer.Serialize(requestData, _options));

                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = uri,
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

        private static HttpClient CreateHttpClient(HttpMessageHandler messageHandler, Uri baseApiUri)
        {
            var client = new HttpClient(messageHandler) { BaseAddress = baseApiUri };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationJsonMediaType));
            return client;
        }

        private HttpMessageHandler CreateHttpMessageHandler(X509Certificate2Collection certificateCollection)
        {
            var handler = new HttpClientHandler();
            foreach (var certificate in certificateCollection)
            {
                _logger.LogTrace("Adding Client Certificate '{Subject}'. Issuer '{Issuer}'. Has Private Key '{HasPrivateKey}'", certificate.Subject, certificate.IssuerName, certificate.HasPrivateKey);
            }
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;

            handler.SslProtocols = SslProtocols.Tls12;
            handler.ClientCertificates.AddRange(certificateCollection);

            handler.ServerCertificateCustomValidationCallback += ServerCertificateCustomValidationCallback;
            return handler;
        }

        private (T Response, ErrorResponse Error) HandleException<T>(Exception exception)
        {
            _logger.LogError(exception, "Exception catch");
            var errorResponse = new ErrorResponse(new Error() { ErrorCode = "InternalException", ErrorMessage = exception.Message, Exception = exception });
            return (default, errorResponse);
        }

        private async Task<(T Response, ErrorResponse Error)> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default) where T : class, new()
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                        var errorResponse = new ErrorResponse(new Error() { ErrorCode = "500", ErrorMessage = "Internal Server Error" });
                        return (default, errorResponse);
                    }
                default:
                    {
                        var responsePayload = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"HTTP {response.StatusCode} {responsePayload}");
                        var errorResponse = new ErrorResponse(new Error() { ErrorCode = response.StatusCode.ToString(), ErrorMessage = "Unknown error", AdditionalInformation = responsePayload });
                        return (default, errorResponse);
                    }
            }
        }

        private async Task<(T Response, ErrorResponse Error)> ReturnErrorsAsync<T>(HttpResponseMessage response)
        {
            
            var error = await response.Content.ReadAsStringAsync();
            var errorResponse = new ErrorResponse(JsonSerializer.Deserialize<Error[]>(error, _options));
            return (default, errorResponse);
        }

        private bool ServerCertificateCustomValidationCallback(HttpRequestMessage request, X509Certificate2 certíficate, X509Chain certificateChain, SslPolicyErrors policyErrors)
        {
            if (policyErrors == SslPolicyErrors.None)
            {
                _logger.LogInformation($"Validating Certificate {certíficate.Subject}. Errors {policyErrors}");
                return true;
            }

            _logger.LogError("Certificate validation error Certificate {CertificateSubject}. Errors {PolicyErrors}", certíficate.Subject, policyErrors);
            return false;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}