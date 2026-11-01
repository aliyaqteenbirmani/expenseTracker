namespace SpendwiseSystem.Domain.DTOs.CashTransactionDtos
{
    public class ReceiptDraftUploadResponseDto
    {
        public Guid DraftId { get; set; }
        public Guid CashBookId { get; set; }
        public string Status { get; set; }
        public DateTime ExpiresOn { get; set; }
    }
}
