using System.Text.RegularExpressions;

namespace LoginSystem
{
    /// <summary>
    /// Centralized auth input validation with consistent user-facing reason strings.
    /// No UI dependencies; callers decide how to display the returned message.
    /// </summary>
    public static class AuthInputValidator
    {
        // Keep messages consistent across Login / SignUp / Forgot Password.
        public const string EmailRequiredMessage = "Please enter your email.";
        public const string PasswordRequiredMessage = "Please enter your password.";
        public const string NameRequiredMessage = "Please enter your name.";
        public const string InvalidEmailMessage = "Please enter a valid email address.";
        public const string PasswordMismatchMessage = "Passwords do not match.";
        public const string WeakPasswordMessage =
            "Password must be at least 8 characters long, contain uppercase, lowercase, and a number.";
        public const string NameTooShortMessage = "Name must be at least 2 characters long.";

        // Simple format check (matches what you already used in both managers)
        private static readonly Regex EmailRegex =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private static readonly Regex UpperRegex = new Regex("[A-Z]", RegexOptions.Compiled);
        private static readonly Regex LowerRegex = new Regex("[a-z]", RegexOptions.Compiled);
        private static readonly Regex DigitRegex = new Regex("[0-9]", RegexOptions.Compiled);

        public static bool TryValidateEmail(string email, out string reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(email))
            {
                reason = EmailRequiredMessage;
                return false;
            }

            if (!EmailRegex.IsMatch(email.Trim()))
            {
                reason = InvalidEmailMessage;
                return false;
            }

            return true;
        }

        public static bool TryValidateLogin(string email, string password, out string reason)
        {
            reason = null;

            if (!TryValidateEmail(email, out reason))
                return false;

            if (string.IsNullOrWhiteSpace(password))
            {
                reason = PasswordRequiredMessage;
                return false;
            }

            return true;
        }

        public static bool TryValidateSignUp(
            string name,
            string email,
            string password,
            string confirmPassword,
            out string reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                reason = NameRequiredMessage;
                return false;
            }

            if (name.Trim().Length < 2)
            {
                reason = NameTooShortMessage;
                return false;
            }

            if (!TryValidateEmail(email, out reason))
                return false;

            if (!TryValidatePasswordStrength(password, out reason))
                return false;

            if (password != confirmPassword)
            {
                reason = PasswordMismatchMessage;
                return false;
            }

            return true;
        }

        public static bool TryValidatePasswordStrength(string password, out string reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(password))
            {
                reason = PasswordRequiredMessage;
                return false;
            }

            // Matches your existing rules in SignUpManager
            if (password.Length < 8 ||
                !UpperRegex.IsMatch(password) ||
                !LowerRegex.IsMatch(password) ||
                !DigitRegex.IsMatch(password))
            {
                reason = WeakPasswordMessage;
                return false;
            }

            return true;
        }
    }
}
