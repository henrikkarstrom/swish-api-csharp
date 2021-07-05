using System;
using System.Threading;
using System.Threading.Tasks;
using SwishApi.Models;

namespace SwishApi
{
    public interface ISwishClient : IDisposable
    {
        /// <summary>
        /// Returns the Payee Alias configured from the certificates
        /// </summary>
        string PayeeAlias { get; }

        /// <summary>
        /// Retruns true if the client is configured to use the Merchant Swish Simulator
        /// </summary>
        bool UseMSS { get; }

        /// <summary>
        /// A payment request is a transaction sent from a merchant to the Swish system to initiate an e-commerce or m-commerce payment.
        /// </summary>
        /// <param name="instructionUUID">The identifier of the payment request to be saved</param>
        /// <param name="payeeAlias">The Swish number of the payee.</param>
        /// <param name="amount">The amount of money to pay. The amount cannot be less than 0.01 SEK and not more than 999999999999.99 SEK.</param>
        /// <param name="message">Merchant supplied message about the payment/order. Max 50 chars. Allowed characters are the letter a-ö, A-Ö, the numbers 0-9 ant the special characters :;.,?!()”</param>
        /// <param name="payeePaymentReference">Payment reference of the payee, which is the merchant that receives the payment. This reference could be order id or similar. Allowed characters are a-z A-Z 0-9 -_.+*/ and lenght must be between 1 and 35 characters.</param>
        /// <param name="callbackUrl">URL that Swish will use to notify caller about the outcome of the Payment request. The URL has to use HTTPS. Optional, if null default Callback Uri from configuration will be used.</param>
        /// <returns>Response or error</returns>
        Task<(LocationResponse Response, ErrorResponse Error)> PaymentRequestAsync(Guid instructionUUID, string payeeAlias, decimal amount, string message, string payeePaymentReference, Uri callbackUrl = null);

        Task<(CheckPaymentRequestStatusResponse Response, ErrorResponse Error)> CheckPaymentStatusAsync(Guid paymentId, CancellationToken cancellationToken = default(CancellationToken));

        Task<(CheckPaymentRequestStatusResponse Response, ErrorResponse Error)> CancelPaymentAsync(Guid paymentId);

        Task<(LocationResponse Response, ErrorResponse Error)> RefundAsync(Guid originalPaymentId, Guid refundId, decimal amount, string message);
        
        Task<(CheckRefundStatusResponse Response, ErrorResponse Error)> CheckRefundStatusAsync(Guid refundId, CancellationToken cancellationToken = default(CancellationToken));
    }
}