namespace SpendwiseSystem.Domain.DTOs.CashTransactionDtos
{
    public class ReceiptDraftDetailsDto
    {
        public Guid DraftId { get; set; }
        public Guid CashBookId { get; set; }
        public string Status { get; set; }
        public string RecognizedText { get; set; }
        public double? OcrConfidence { get; set; }
        public string SuggestedMerchant { get; set; }
        public decimal? SuggestedAmount { get; set; }
        public DateTime? SuggestedTransactionDate { get; set; }
        public string SuggestedTransactionType { get; set; }
        public string SuggestedCategory { get; set; }
        public double? ParsingConfidence { get; set; }
        public Guid? CreatedTransactionId { get; set; }
        public DateTime ExpiresOn { get; set; }
    }
}
