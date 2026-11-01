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
        public DateTime CreatedOn { get; set; }
        public DateTime ExpiresOn { get; set; }
    }
}
