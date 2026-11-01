namespace SpendwiseSystem.Application.Services.ReceiptOcrService
{
    public class ReceiptOcrResult
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
    }

    public interface IReceiptOcrService
    {
        Task<ReceiptOcrResult> ExtractTextAsync(string filePath, string contentType);
    }
}
