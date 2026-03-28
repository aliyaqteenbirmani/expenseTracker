namespace SpendwiseSystem.Domain.Entities
{
    public class Spendwise
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public Guid UserId { get; set; }
    }
}



