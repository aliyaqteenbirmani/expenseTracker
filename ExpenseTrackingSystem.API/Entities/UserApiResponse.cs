namespace ExpenseTrackingSystem.Domain.Entities
{
    public class UserApiResponse<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = null;    
        public T Data { get; set; }
        public string Token { get; set; } = null;

    }
}
