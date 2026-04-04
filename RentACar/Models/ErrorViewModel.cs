namespace RentACar.Models
{
    /// <summary>
    /// View model used by the error page.
    /// Contains request correlation information used for diagnostics and conditional display.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Unique identifier for the current HTTP request (may be null).
        /// This value is useful for tracing and correlating a specific request with logs.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Indicates whether the request id should be shown in the error page.
        /// Returns true when <see cref="RequestId"/> is not null or empty.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
