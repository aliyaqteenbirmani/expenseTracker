namespace SpendwiseSystem.Application.Services.ReceiptParsingService
{
    public interface IReceiptParsingService
    {
        ReceiptDraftSuggestion Parse(string recognizedText);
    }
}
