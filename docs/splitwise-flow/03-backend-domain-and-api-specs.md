# Backend Domain & API Specifications

## 1. Current Architecture Review

In the current solution:
- **Domain Project**: `ExpenseTrackingSystem.API/SpendwiseSystem.Domain.csproj`
- **Application Project**: `ExpenseTrackingSystem.Application/SpendwiseSystem.Application.csproj`
- **Infrastructure Project**: `ExpenseTrackingSystem.Infrastructure/SpendwiseSystem.Infrastructure.csproj`
- **API Project**: `ExpenseTrackingSystem/SpendwiseSystem.API.csproj`

Currently, `ISplitCalculatorService` only supports:
```csharp
Dictionary<Guid, decimal> CalculateEqualSplit(decimal totalAmount, IReadOnlyList<Guid> participantUserIds);
```
And `CreateSplitExpenseDto` only accepts `participantUserIds` and divides the amount equally.

---

## 2. Domain Entities & Enums Enhancements

### 2.1 Split Calculation Type Enum
```csharp
namespace SpendwiseSystem.Domain.Enums
{
    public enum SplitCalculationType
    {
        Equal = 0,
        Exact = 1,
        Percentage = 2,
        Shares = 3,
        Adjustment = 4
    }
}
```

### 2.2 Expense Share Entity (`SplitExpenseShare`)
```csharp
namespace SpendwiseSystem.Domain.Entities
{
    public class SplitExpenseShare
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ExpenseId { get; set; }
        public Guid UserId { get; set; }
        
        /// <summary>
        /// Final calculated share in currency (cents safe).
        /// </summary>
        public decimal ShareAmount { get; set; }

        /// <summary>
        /// Raw percentage (e.g. 33.33) if SplitType == Percentage
        /// </summary>
        public decimal? Percentage { get; set; }

        /// <summary>
        /// Number of parts (e.g. 2) if SplitType == Shares
        /// </summary>
        public int? ShareParts { get; set; }

        /// <summary>
        /// Exact specified amount if SplitType == Exact
        /// </summary>
        public decimal? ExactAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual SplitExpense Expense { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
```

### 2.3 Multiple Payers Entity (`SplitExpensePayer`) (Optional / Advanced)
```csharp
namespace SpendwiseSystem.Domain.Entities
{
    public class SplitExpensePayer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ExpenseId { get; set; }
        public Guid UserId { get; set; }
        public decimal AmountPaid { get; set; }

        public virtual SplitExpense Expense { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
```

---

## 3. Data Transfer Objects (DTOs)

### 3.1 Participant Split Input DTO
```csharp
namespace SpendwiseSystem.Domain.DTOs.SplitExpenseDtos
{
    public class ParticipantSplitRequestDto
    {
        public Guid UserId { get; set; }

        /// <summary>
        /// Required when SplitType == Exact
        /// </summary>
        public decimal? ExactAmount { get; set; }

        /// <summary>
        /// Required when SplitType == Percentage (0 to 100)
        /// </summary>
        public decimal? Percentage { get; set; }

        /// <summary>
        /// Required when SplitType == Shares (integer >= 1)
        /// </summary>
        public int? ShareParts { get; set; }
    }

    public class ExpensePayerRequestDto
    {
        public Guid UserId { get; set; }
        public decimal AmountPaid { get; set; }
    }
}
```

### 3.2 Enhanced `CreateSplitExpenseDto`
```csharp
namespace SpendwiseSystem.Domain.DTOs.SplitExpenseDtos
{
    public class CreateSplitExpenseDto
    {
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime? ExpenseDate { get; set; }
        public string? Category { get; set; }
        public string? Note { get; set; }
        public string? ReceiptUrl { get; set; }

        /// <summary>
        /// Primary single payer (backwards compatible).
        /// </summary>
        public Guid? PayerId { get; set; }

        /// <summary>
        /// Optional: List of multiple payers (sum must equal Amount).
        /// </summary>
        public List<ExpensePayerRequestDto>? Payers { get; set; }

        /// <summary>
        /// Split method used: Equal, Exact, Percentage, Shares.
        /// Defaults to Equal if omitted.
        /// </summary>
        public SplitCalculationType SplitType { get; set; } = SplitCalculationType.Equal;

        /// <summary>
        /// Detailed participant split configuration.
        /// </summary>
        public List<ParticipantSplitRequestDto>? ParticipantSplits { get; set; }

        /// <summary>
        /// Deprecated: kept for backwards compatibility with legacy Equal-only clients.
        /// </summary>
        public List<Guid>? ParticipantUserIds { get; set; }
    }
}
```

---

## 4. Application Service: `ISplitCalculatorService` Expansion

```csharp
namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitCalculatorService
    {
        /// <summary>
        /// Divides totalAmount equally across participant user IDs (deterministic rounding).
        /// </summary>
        Dictionary<Guid, decimal> CalculateEqualSplit(decimal totalAmount, IReadOnlyList<Guid> participantUserIds);

        /// <summary>
        /// Validates and computes exact amount splits.
        /// </summary>
        Dictionary<Guid, decimal> CalculateExactSplit(decimal totalAmount, IReadOnlyList<ParticipantSplitRequestDto> splits);

        /// <summary>
        /// Computes percentage-based splits with remainder resolution.
        /// </summary>
        Dictionary<Guid, decimal> CalculatePercentageSplit(decimal totalAmount, IReadOnlyList<ParticipantSplitRequestDto> splits);

        /// <summary>
        /// Computes proportional parts/shares splits.
        /// </summary>
        Dictionary<Guid, decimal> CalculateSharesSplit(decimal totalAmount, IReadOnlyList<ParticipantSplitRequestDto> splits);
    }
}
```

### 4.1 Service Validation Logic
In `SplitExpenseService.cs`:
```csharp
public async Task<ApiResponse<SplitExpenseDetailDto>> AddExpenseAsync(
    Guid groupId, CreateSplitExpenseDto dto, Guid currentUserId, string currentUserName)
{
    // 1. Verify group & active membership
    // 2. Validate amount > 0 & title not empty
    // 3. Resolve calculation type & participants
    Dictionary<Guid, decimal> calculatedShares;

    switch (dto.SplitType)
    {
        case SplitCalculationType.Exact:
            calculatedShares = _calculatorService.CalculateExactSplit(dto.Amount, dto.ParticipantSplits!);
            break;
        case SplitCalculationType.Percentage:
            calculatedShares = _calculatorService.CalculatePercentageSplit(dto.Amount, dto.ParticipantSplits!);
            break;
        case SplitCalculationType.Shares:
            calculatedShares = _calculatorService.CalculateSharesSplit(dto.Amount, dto.ParticipantSplits!);
            break;
        case SplitCalculationType.Equal:
        default:
            var ids = dto.ParticipantSplits?.Select(p => p.UserId).ToList() 
                      ?? dto.ParticipantUserIds 
                      ?? new List<Guid>();
            calculatedShares = _calculatorService.CalculateEqualSplit(dto.Amount, ids);
            break;
    }

    // 4. Save Expense and ExpenseShares atomically
    // 5. Update Balance Cache / Repository
    // 6. Broadcast Realtime SignalR event "SplitExpenseAdded"
}
```

---

## 5. API Endpoints Specification

### 5.1 Add Expense
- **Endpoint**: `POST /api/split-groups/{groupId}/expenses`
- **Authorization**: Bearer JWT; user must be active group member.
- **Payload**: `CreateSplitExpenseDto`
- **Response**: `200 OK` with `ApiResponse<SplitExpenseDetailDto>`

### 5.2 Edit Expense
- **Endpoint**: `PUT /api/split-groups/{groupId}/expenses/{expenseId}`
- **Authorization**: Bearer JWT; user must be expense creator or group owner.
- **Payload**: `UpdateSplitExpenseDto` (same structure as create).
- **Response**: `200 OK` with updated details.

### 5.3 Simplified Debt Graph
- **Endpoint**: `GET /api/split-groups/{groupId}/balances/simplified`
- **Authorization**: Bearer JWT
- **Response**:
```json
{
  "groupId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "totalTransactions": 2,
  "simplifiedDebts": [
    {
      "fromUserId": "guid-usman",
      "fromUserName": "Usman Khan",
      "toUserId": "guid-ali",
      "toUserName": "Ali Raza",
      "amount": 2500.00
    },
    {
      "fromUserId": "guid-bilal",
      "fromUserName": "Bilal Ahmed",
      "toUserId": "guid-ali",
      "toUserName": "Ali Raza",
      "amount": 1000.00
    }
  ]
}
```
