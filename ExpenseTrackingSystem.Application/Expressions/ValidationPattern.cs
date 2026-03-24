using System.Text.RegularExpressions;

namespace ExpenseTrackingSystem.Application.Expressions
{
    public static class ValidationPattern
    {
        public static bool ValidatePhone(string phone)
        {
            string numPattern = @"^\+?(\d{1,4})?[-.\s]?(\(?\d{1,4}\)?)?[-.\s]?\d{1,4}[-.\s]?\d{1,4}[-.\s]?\d{1,9}$";
            if (Regex.IsMatch(phone, numPattern))
                return true;
            else
                return false;
        }

        public static bool ValidateEmail(string email)
        {
            string emailpattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

            if (Regex.IsMatch(email, emailpattern))
                return true;
            else
                return false;
        }
    }
}
