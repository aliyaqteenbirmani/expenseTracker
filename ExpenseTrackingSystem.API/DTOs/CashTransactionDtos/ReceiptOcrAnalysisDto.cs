namespace SpendwiseSystem.Domain.DTOs.CashTransactionDtos
{
    public class ReceiptOcrAnalysisDto
    {
        public Guid DraftId { get; set; }
        public string Status { get; set; }
        public string RecognizedText { get; set; }
        public double Confidence { get; set; }
    }
}
