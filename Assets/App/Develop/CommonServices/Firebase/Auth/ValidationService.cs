namespace App.Develop.CommonServices.Firebase.Auth
{
    public class ValidationService
    {
        public bool IsValidEmail(string email)
        {
            try { return new System.Net.Mail.MailAddress(email).Address == email; }
            catch { return false; }
        }

        public bool IsValidPassword(string password)
        {
            if (password.Length < 8 || password.Length > 12) return false;
            bool hasUpper = false, hasLower = false, hasDigit = false;
            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                if (char.IsLower(c)) hasLower = true;
                if (char.IsDigit(c)) hasDigit = true;
            }
            return hasUpper && hasLower && hasDigit;
        }
    }
}
