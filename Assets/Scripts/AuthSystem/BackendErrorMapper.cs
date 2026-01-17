using System.Collections.Generic;

namespace LoginSystem
{
    public static class BackendErrorMapper
    {
        private static readonly Dictionary<string, string> errorMappings = new Dictionary<string, string>
        {
            { "incorrect_password", "Incorrect password. Please try again." },
            { "no_account", "No account found with this email." },
            { "email_exists", "An account with this email already exists." },
            { "server_error", "A server error occurred. Please try again later." },
            { "network_error", "Network error. Check your internet connection." },
            { "parse_error", "Unexpected response from server. Please contact support. contact@mprstudios.com" },

            // 🔒 Password Reset Specific
            { "password_reset_email_sent", "✅ Password reset email sent! Check your inbox." },
            { "account_not_found", "No account found with that email." },
            { "invalid_or_expired_token", "❌ This reset link is invalid or has expired." },
            { "password_updated", "✅ Your password was successfully updated!" }
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