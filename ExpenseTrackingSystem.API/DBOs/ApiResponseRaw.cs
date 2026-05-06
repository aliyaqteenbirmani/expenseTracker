namespace SpendwiseSystem.Domain.DBOs
{
    public class ApiResponseRaw
    {
        public int ResponseCode { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Data { get; set; }
    }
}
