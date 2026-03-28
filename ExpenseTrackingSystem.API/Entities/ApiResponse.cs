namespace SpendwiseSystem.Domain.Entities
{
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public bool Success { get; set; } = true;
        public string Message { get; set; } = null;
        public string ErrorCode { get; set; }
        public T Data { get; set; }
    }
}


