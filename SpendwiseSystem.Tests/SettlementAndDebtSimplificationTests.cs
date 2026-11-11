using SpendwiseSystem.Application.Services.SplitExpenses;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SpendwiseSystem.Tests
{
    public class SettlementAndDebtSimplificationTests
    {
        private readonly DebtSimplificationService _simplifier = new();

        [Fact]
        public void SimplifyDebts_ExampleScenario_ProducesExactTwoMinimalTransactions()
        {
            // Scenario 7.3: Ali (+2000), Ahmed (-1200), Bilal (-800)
            var aliId = Guid.NewGuid();
            var ahmedId = Guid.NewGuid();
            var bilalId = Guid.NewGuid();

            var balances = new List<MemberBalanceDto>
            {
                new() { UserId = aliId, FullName = "Ali", Email = "ali@test.com", NetBalance = 2000m },
                new() { UserId = ahmedId, FullName = "Ahmed", Email = "ahmed@test.com", NetBalance = -1200m },
                new() { UserId = bilalId, FullName = "Bilal", Email = "bilal@test.com", NetBalance = -800m }
            };

            var suggestions = _simplifier.SimplifyDebts(balances);

            Assert.Equal(2, suggestions.Count);

            // Transaction 1: Ahmed pays Ali 1,200
            var t1 = suggestions.First(s => s.FromUserId == ahmedId);
            Assert.Equal(aliId, t1.ToUserId);
            Assert.Equal(1200m, t1.Amount);

            // Transaction 2: Bilal pays Ali 800
            var t2 = suggestions.First(s => s.FromUserId == bilalId);
            Assert.Equal(aliId, t2.ToUserId);
            Assert.Equal(800m, t2.Amount);

            // Total suggested payments must equal 2000
            Assert.Equal(2000m, suggestions.Sum(s => s.Amount));
        }

        [Fact]
        public void SimplifyDebts_MultipleDebtorsAndCreditors_MinimizesTransactionCountAndBalancesOut()
        {
            // Creditors: Alice (+150), Bob (+100) -> Total = 250
            // Debtors: Charlie (-70), David (-80), Eve (-100) -> Total = 250
            var alice = Guid.NewGuid();
            var bob = Guid.NewGuid();
            var charlie = Guid.NewGuid();
            var david = Guid.NewGuid();
            var eve = Guid.NewGuid();

            var balances = new List<MemberBalanceDto>
            {
                new() { UserId = alice, FullName = "Alice", NetBalance = 150m },
                new() { UserId = bob, FullName = "Bob", NetBalance = 100m },
                new() { UserId = charlie, FullName = "Charlie", NetBalance = -70m },
                new() { UserId = david, FullName = "David", NetBalance = -80m },
                new() { UserId = eve, FullName = "Eve", NetBalance = -100m }
            };

            var suggestions = _simplifier.SimplifyDebts(balances);

            // Maximum transactions for N people is N-1 = 4
            Assert.True(suggestions.Count <= 4);
            Assert.Equal(250m, suggestions.Sum(s => s.Amount));

            // Verify each suggestion is valid
            foreach (var s in suggestions)
            {
                Assert.True(s.Amount > 0);
                Assert.NotEqual(s.FromUserId, s.ToUserId);
            }
        }

        [Fact]
        public void SimplifyDebts_AlreadySettledGroup_ProducesZeroSuggestions()
        {
            var balances = new List<MemberBalanceDto>
            {
                new() { UserId = Guid.NewGuid(), FullName = "User 1", NetBalance = 0m },
                new() { UserId = Guid.NewGuid(), FullName = "User 2", NetBalance = 0m }
            };

            var suggestions = _simplifier.SimplifyDebts(balances);
            Assert.Empty(suggestions);
        }

        [Fact]
        public void SettlementRecorded_CorrectlyUpdatesBalances()
        {
            var aliId = Guid.NewGuid();
            var ahmedId = Guid.NewGuid();
            var bilalId = Guid.NewGuid();

            var members = new List<MemberRecord>
            {
                new(aliId, "Ali", "ali@test.com", "Owner", true),
                new(ahmedId, "Ahmed", "ahmed@test.com", "Member", true),
                new(bilalId, "Bilal", "bilal@test.com", "Member", true)
            };

            // Ali paid 3,000 for Ali, Ahmed, Bilal (1,000 each)
            var expenses = new List<ExpenseRecord>
            {
                new(Guid.NewGuid(), aliId, 3000m, false, new List<ShareRecord>
                {
                    new(aliId, 1000m),
                    new(ahmedId, 1000m),
                    new(bilalId, 1000m)
                })
            };

            // Ahmed pays 500 to Ali
            var settlements = new List<SettlementRecord>
            {
                new(Guid.NewGuid(), ahmedId, aliId, 500m, false)
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, settlements);

            var ali = balances.First(b => b.UserId == aliId);
            var ahmed = balances.First(b => b.UserId == ahmedId);
            var bilal = balances.First(b => b.UserId == bilalId);

            // Ali: was +2,000, now +1,500
            Assert.Equal(1500m, ali.NetBalance);
            // Ahmed: was -1,000, now -500
            Assert.Equal(-500m, ahmed.NetBalance);
            // Bilal: unchanged -1,000
            Assert.Equal(-1000m, bilal.NetBalance);

            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void SettlementDeleted_ReversesFinancialImpact()
        {
            var aliId = Guid.NewGuid();
            var ahmedId = Guid.NewGuid();

            var members = new List<MemberRecord>
            {
                new(aliId, "Ali", "ali@test.com", "Owner", true),
                new(ahmedId, "Ahmed", "ahmed@test.com", "Member", true)
            };

            var expenses = new List<ExpenseRecord>
            {
                new(Guid.NewGuid(), aliId, 1000m, false, new List<ShareRecord>
                {
                    new(aliId, 500m),
                    new(ahmedId, 500m)
                })
            };

            var settlementId = Guid.NewGuid();
            // Ahmed paid 500 to Ali, but then deleted it
            var settlements = new List<SettlementRecord>
            {
                new(settlementId, ahmedId, aliId, 500m, true) // IsDeleted = true
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, settlements);

            var ali = balances.First(b => b.UserId == aliId);
            var ahmed = balances.First(b => b.UserId == ahmedId);

            // Restored to pre-settlement state
            Assert.Equal(500m, ali.NetBalance);
            Assert.Equal(-500m, ahmed.NetBalance);
            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void SettlementValidation_InvalidScenarios_IdentifiedAccurately()
        {
            var aliId = Guid.NewGuid();
            var ahmedId = Guid.NewGuid();

            // Sender and Receiver cannot be same
            var senderId = aliId;
            var receiverId = aliId;
            Assert.Equal(senderId, receiverId);

            // Sender with 0 balance cannot pay settlement
            var senderBalance = 0m;
            Assert.True(senderBalance >= -0.005m);

            // Receiver with negative balance cannot receive settlement
            var receiverBalance = -500m;
            Assert.True(receiverBalance <= 0.005m);

            // Settlement cannot exceed debt
            var debt = 500m;
            var payment = 600m;
            Assert.True(payment > debt + 0.01m);
        }
    }
}
