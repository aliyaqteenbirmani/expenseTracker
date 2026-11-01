namespace SpendwiseSystem.Domain.DTOs.CashTransactionDtos
{
    public class ReceiptDraftSuggestionDto
    {
        public Guid DraftId { get; set; }
        public string Status { get; set; }
        public string Merchant { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string SuggestedCategory { get; set; }
        public double Confidence { get; set; }
        public string RecognizedText { get; set; }
    }
}
