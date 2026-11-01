namespace SpendwiseSystem.Application.Services.ReceiptParsingService
{
    public class ReceiptDraftSuggestion
    {
        public string Merchant { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string Category { get; set; }
        public double Confidence { get; set; }
    }
}
