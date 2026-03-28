using ExpenseTrackingSystem.Domain.Enums;

namespace ExpenseTrackingSystem.Domain.DBOs
{
    public class LoginResponseDbo
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Guid? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public Gender Gender { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
    }
}
