using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.Entities
{
    public class ReceiptDraft
    {
        public Guid Id { get; set; }
        public Guid CashBookId { get; set; }
        public Guid UserId { get; set; }
        [MaxLength(260)]
        public string StoredFileName { get; set; }
        [MaxLength(100)]
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        [MaxLength(30)]
        public string Status { get; set; }
        public string OcrText { get; set; }
        public double? OcrConfidence { get; set; }
        public string SuggestedMerchant { get; set; }
        public decimal? SuggestedAmount { get; set; }
        public DateTime? SuggestedTransactionDate { get; set; }
        [MaxLength(50)]
        public string SuggestedTransactionType { get; set; }
        [MaxLength(50)]
        public string SuggestedCategory { get; set; }
        public double? ParsingConfidence { get; set; }
        public Guid? CreatedTransactionId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ExpiresOn { get; set; }
    }
}
