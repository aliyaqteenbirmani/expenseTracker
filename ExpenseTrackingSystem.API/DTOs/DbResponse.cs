namespace ExpenseTrackingSystem.Domain.DTOs
{
    public class DbResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
