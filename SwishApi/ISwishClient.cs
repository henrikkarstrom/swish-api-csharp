using System;
using System.Threading;
using System.Threading.Tasks;
using SwishApi.Models;

namespace SwishApi
{
    public interface ISwishClient : IDisposable
    {
        

         Task<(LocationResponse Response, ErrorResponse Error)> MakePaymentRequestAsync(Guid paymentId, string phoneNumber, decimal amount, string message, string orderId);

        Task<(CheckPaymentRequestStatusResponse Response, ErrorResponse Error)> CheckPaymentStatusAsync(Guid paymentId, CancellationToken cancellationToken = default(CancellationToken));

        Task<(CheckPaymentRequestStatusResponse Response, ErrorResponse Error)> CancelPaymentAsync(Guid paymentId);

        Task<(LocationResponse Response, ErrorResponse Error)> RefundAsync(Guid originalPaymentId, Guid refundId, decimal amount, string message);
        
        Task<(CheckRefundStatusResponse Response, ErrorResponse Error)> CheckRefundStatusAsync(Guid refundId, CancellationToken cancellationToken = default(CancellationToken));

        Task<(QRCodeResponse Response, ErrorResponse Error)> GetQRCodeAsync(string token, string format = "svg", int size = 300, int border = 0, bool transparent = true, CancellationToken cancellationToken = default(CancellationToken));
    }
}