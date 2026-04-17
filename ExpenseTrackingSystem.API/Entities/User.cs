using SpendwiseSystem.Domain.Enums;

namespace SpendwiseSystem.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int Gender { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }

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


