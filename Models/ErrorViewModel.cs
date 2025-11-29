namespace TravelAgencyProject.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        /// A clear and concise error title for the user
        public string? ErrorTitle { get; set; }

        /// to provide a relative error message for all constraints.
        public string? ErrorMessage { get; set; } 

        /// Optional internal error code for debugging and technical support purposes.
        public int? ErrorCode { get; set; }
    }
}
