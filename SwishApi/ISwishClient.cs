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

        Task<(LocationResponse Response, ErrorResponse Error)> PaymentRequestAsync(Guid paymentId, string phoneNumber, decimal amount, string message, string orderId);

        Task<(CheckPaymentRequestStatusResponse Response, ErrorResponse Error)> CheckPaymentStatusAsync(Guid paymentId, CancellationToken cancellationToken = default(CancellationToken));

        Task<(CheckPaymentRequestStatusResponse Response, ErrorResponse Error)> CancelPaymentAsync(Guid paymentId);

        Task<(LocationResponse Response, ErrorResponse Error)> RefundAsync(Guid originalPaymentId, Guid refundId, decimal amount, string message);
        
        Task<(CheckRefundStatusResponse Response, ErrorResponse Error)> CheckRefundStatusAsync(Guid refundId, CancellationToken cancellationToken = default(CancellationToken));
    }
}