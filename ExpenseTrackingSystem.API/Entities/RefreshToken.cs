namespace SpendwiseSystem.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Revoked { get; set; }
        public string UserId { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsActive => Revoked == null && !IsExpired;
    }
}
