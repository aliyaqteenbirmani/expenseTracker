using System.Globalization;
using System.Text.RegularExpressions;
using SpendwiseSystem.Application.Services.ReceiptParsingService;

namespace SpendwiseSystem.Infrastructure.Services.ReceiptParsingService
{
    /// <summary>
    /// Conservative parser for common receipt layouts. Every extracted value is a suggestion
    /// that must be reviewed before it is used to create a transaction.
    /// </summary>
    public class RuleBasedReceiptParsingService : IReceiptParsingService
    {
        private static readonly string[] DateFormats =
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd",
            "dd/MM/yy", "d/M/yy", "dd-MM-yy", "d-M-yy"
        };

        private static readonly (string Category, string[] Keywords)[] CategoryKeywords =
        {
            ("Food", new[] { "food", "restaurant", "cafe", "coffee", "bakery", "grocery", "groceries", "supermarket", "meal", "pizza", "burger", "chai", "khana", "nashta", "dinner", "lunch" }),
            ("Transport", new[] { "fuel", "petrol", "diesel", "gas station", "uber", "careem", "taxi", "transport", "bus", "train", "parking", "toll" }),
            ("Shopping", new[] { "clothing", "clothes", "shoes", "store", "shop", "mall", "retail", "garments" }),
            ("Healthcare", new[] { "pharmacy", "medical", "hospital", "clinic", "doctor", "medicine", "dawa" }),
            ("Utilities", new[] { "electricity", "water bill", "gas bill", "internet", "utility", "ptcl", "lesco", "k-electric" })
        };

        public ReceiptDraftSuggestion Parse(string recognizedText)
        {
            if (string.IsNullOrWhiteSpace(recognizedText))
                return new ReceiptDraftSuggestion { TransactionType = "CashOut", Confidence = 0 };

            var lines = recognizedText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
                .Where(line => line.Length > 0)
                .ToArray();

            var merchant = ExtractMerchant(lines);
            var date = ExtractDate(recognizedText);
            var amount = ExtractAmount(lines);
            var category = ExtractCategory(recognizedText);

            // Receipts usually represent spending, but confidence is reduced unless the text
            // resembles a receipt with a recognized total or payment marker.
            var isReceiptLike = amount.HasValue || Regex.IsMatch(recognizedText,
                @"\b(total|grand total|amount due|paid|payment|subtotal|kul|raqam)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            var confidence = 0.15;
            if (merchant != null) confidence += 0.2;
            if (amount.HasValue) confidence += 0.4;
            if (date.HasValue) confidence += 0.15;
            if (category != null) confidence += 0.1;
            if (!isReceiptLike) confidence *= 0.5;

            return new ReceiptDraftSuggestion
            {
                Merchant = merchant,
                Amount = amount,
                TransactionDate = date,
                TransactionType = "CashOut",
                Category = category,
                Confidence = Math.Clamp(confidence, 0, 1)
            };
        }

        private static string ExtractMerchant(string[] lines)
        {
            foreach (var line in lines.Take(6))
            {
                if (line.Length < 2 || line.Length > 80)
                    continue;
                if (Regex.IsMatch(line, @"\b(invoice|receipt|cash memo|tax invoice|date|time|phone|tel|ntn|gst|strn|www\.|https?://)\b", RegexOptions.IgnoreCase))
                    continue;
                if (!Regex.IsMatch(line, @"\p{L}"))
                    continue;

                return line;
            }

            return null;
        }

        private static DateTime? ExtractDate(string text)
        {
            var matches = Regex.Matches(text,
                @"(?<!\d)(?:\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|\d{4}-\d{2}-\d{2})(?!\d)");
            foreach (Match match in matches)
            {
                if (DateTime.TryParseExact(match.Value, DateFormats, CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces, out var parsed)
                    || DateTime.TryParse(match.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                {
                    if (parsed.Date <= DateTime.UtcNow.Date.AddDays(1))
                        return parsed.Date;
                }
            }

            return null;
        }

        private static decimal? ExtractAmount(string[] lines)
        {
            var preferred = lines.Where(line => Regex.IsMatch(line,
                @"\b(grand\s+total|total|amount\s+due|balance\s+due|paid|payable|kul|raqam)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

            foreach (var line in preferred.Concat(lines.Reverse()))
            {
                var matches = Regex.Matches(line,
                    @"(?<![\w/])(?:PKR\s*|Rs\.?\s*|₨\s*)?((?:\d{1,3}(?:,\d{3})+|\d+)(?:\.\d{1,2})?)(?!\w)",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (matches.Count == 0)
                    continue;

                var candidate = matches[matches.Count - 1].Groups[1].Value.Replace(",", string.Empty);
                if (decimal.TryParse(candidate, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)
                    && amount > 0 && amount <= 100_000_000)
                    return amount;
            }

            return null;
        }

        private static string ExtractCategory(string text)
        {
            var normalizedText = text.ToLowerInvariant();
            foreach (var (category, keywords) in CategoryKeywords)
            {
                if (keywords.Any(keyword => Regex.IsMatch(normalizedText,
                        $@"(?<!\p{{L}}){Regex.Escape(keyword)}(?!\p{{L}})",
                        RegexOptions.CultureInvariant)))
                    return category;
            }

            return null;
        }
    }
}
