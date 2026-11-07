namespace SpendwiseSystem.Domain.DTOs.MemberDto
{
    public class UserWorkspaceAccessDto
    {
        public bool IsOwner { get; set; }
        public List<string> BusinessPermissions { get; set; } = new();
        public List<string> CashbookPermissions { get; set; } = new();
    }
}
