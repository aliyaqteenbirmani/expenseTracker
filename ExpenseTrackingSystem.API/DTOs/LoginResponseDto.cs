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

        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }


    }
}
