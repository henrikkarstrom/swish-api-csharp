namespace SwishApi.Models
{
    public enum RefundStatus
    {
        UNKNOWN,

        /// <summary>
        /// Refund created
        /// </summary>
        CREATED,
        /// <summary>
        /// Refund ongoing
        /// </summary>
        VALIDATED,
        /// <summary>
        /// Money has been withdrawn from your account
        /// </summary>
        DEBITED,
        /// <summary>
        /// The payment was successful
        /// </summary>
        PAID,

        /// <summary>
        /// An error occurred.See list of error codes for all potential error conditions.
        /// </summary>
        ERROR

    }
}