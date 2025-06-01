using System.Collections.Generic;

namespace LoginSystem
{
    public static class BackendErrorMapper
    {
        private static readonly Dictionary<string, string> errorMappings = new Dictionary<string, string>
        {
            { "incorrect_password", "Incorrect password. Please try again." },
            { "account_not_found", "No account found with this email." },
            { "email_exists", "An account with this email already exists." },
            { "server_error", "A server error occurred. Please try again later." },
            { "network_error", "Network error. Check your internet connection." },
            { "parse_error", "Unexpected response from server. Please contact support." },
        };

        public static string GetFriendlyMessage(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode))
                return "An unknown error occurred.";

            if (errorMappings.TryGetValue(errorCode, out string friendlyMessage))
                return friendlyMessage;

            return "An unexpected error occurred. Please try again.";
        }
    }
}