using ExpenseTrackingSystem.Domain.Enums;

namespace ExpenseTrackingSystem.Domain.DTOs
{
    public class LoginResponseDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public Gender Gender { get; set; }
        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public string Token { get; set; }

    }
}
