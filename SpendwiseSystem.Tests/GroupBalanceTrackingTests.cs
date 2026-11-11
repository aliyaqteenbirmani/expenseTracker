using SpendwiseSystem.Application.Services.SplitExpenses;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SpendwiseSystem.Tests
{
    public class GroupBalanceTrackingTests
    {
        private readonly Guid _aliId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private readonly Guid _ahmedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private readonly Guid _bilalId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        private List<MemberRecord> CreateDefaultMembers() => new()
        {
            new MemberRecord(_aliId, "Ali", "ali@spendwise.com", "Owner", true),
            new MemberRecord(_ahmedId, "Ahmed", "ahmed@spendwise.com", "Member", true),
            new MemberRecord(_bilalId, "Bilal", "bilal@spendwise.com", "Member", true)
        };

        [Fact]
        public void OneMemberPaysForEveryone_BalancesReflectCorrectCreditAndDebts()
        {
            // Scenario 6.1: Ali paid Rs. 3,000. Ali, Ahmed, Bilal share equally (1,000 each)
            var members = CreateDefaultMembers();
            var expenses = new List<ExpenseRecord>
            {
                new(
                    Guid.NewGuid(),
                    _aliId,
                    3000m,
                    false,
                    new List<ShareRecord>
                    {
                        new(_aliId, 1000m),
                        new(_ahmedId, 1000m),
                        new(_bilalId, 1000m)
                    }
                )
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, Enumerable.Empty<SettlementRecord>());

            var ali = balances.First(b => b.UserId == _aliId);
            var ahmed = balances.First(b => b.UserId == _ahmedId);
            var bilal = balances.First(b => b.UserId == _bilalId);

            Assert.Equal(2000m, ali.NetBalance);
            Assert.Equal(-1000m, ahmed.NetBalance);
            Assert.Equal(-1000m, bilal.NetBalance);

            // Sum of all net balances in any group must equal zero
            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void MultipleMembersPaySeparateExpenses_BalancesCombineAccurately()
        {
            // Expense 1: Ali pays 3,000 shared by Ali, Ahmed, Bilal (1,000 each)
            // Expense 2: Ahmed pays 1,500 shared by Ali, Ahmed, Bilal (500 each)
            var members = CreateDefaultMembers();
            var expenses = new List<ExpenseRecord>
            {
                new(
                    Guid.NewGuid(),
                    _aliId,
                    3000m,
                    false,
                    new List<ShareRecord>
                    {
                        new(_aliId, 1000m),
                        new(_ahmedId, 1000m),
                        new(_bilalId, 1000m)
                    }
                ),
                new(
                    Guid.NewGuid(),
                    _ahmedId,
                    1500m,
                    false,
                    new List<ShareRecord>
                    {
                        new(_aliId, 500m),
                        new(_ahmedId, 500m),
                        new(_bilalId, 500m)
                    }
                )
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, Enumerable.Empty<SettlementRecord>());

            var ali = balances.First(b => b.UserId == _aliId);
            var ahmed = balances.First(b => b.UserId == _ahmedId);
            var bilal = balances.First(b => b.UserId == _bilalId);

            // Ali: Paid 3,000, Total Shares (1,000 + 500 = 1,500) => +1,500
            Assert.Equal(1500m, ali.NetBalance);
            // Ahmed: Paid 1,500, Total Shares (1,000 + 500 = 1,500) => 0.00
            Assert.Equal(0m, ahmed.NetBalance);
            // Bilal: Paid 0, Total Shares (1,000 + 500 = 1,500) => -1,500
            Assert.Equal(-1500m, bilal.NetBalance);

            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void PayerExcludedFromSplit_PayerGetsFullCreditAndParticipantsOweFullAmount()
        {
            // Scenario 5.3: Ali pays Rs. 3,000. Ahmed and Bilal share (1,500 each). Ali is excluded.
            var members = CreateDefaultMembers();
            var expenses = new List<ExpenseRecord>
            {
                new(
                    Guid.NewGuid(),
                    _aliId,
                    3000m,
                    false,
                    new List<ShareRecord>
                    {
                        new(_ahmedId, 1500m),
                        new(_bilalId, 1500m)
                    }
                )
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, Enumerable.Empty<SettlementRecord>());

            var ali = balances.First(b => b.UserId == _aliId);
            var ahmed = balances.First(b => b.UserId == _ahmedId);
            var bilal = balances.First(b => b.UserId == _bilalId);

            Assert.Equal(3000m, ali.NetBalance);
            Assert.Equal(-1500m, ahmed.NetBalance);
            Assert.Equal(-1500m, bilal.NetBalance);
            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void MemberParticipatesWithoutPaying_AccumulatesDebt()
        {
            // Bilal never pays, only shares
            var members = CreateDefaultMembers();
            var expenses = new List<ExpenseRecord>
            {
                new(Guid.NewGuid(), _aliId, 1000m, false, new List<ShareRecord> { new(_aliId, 500m), new(_bilalId, 500m) }),
                new(Guid.NewGuid(), _ahmedId, 800m, false, new List<ShareRecord> { new(_ahmedId, 400m), new(_bilalId, 400m) })
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, Enumerable.Empty<SettlementRecord>());

            var bilal = balances.First(b => b.UserId == _bilalId);
            Assert.Equal(0m, bilal.TotalPaid);
            Assert.Equal(900m, bilal.TotalShare);
            Assert.Equal(-900m, bilal.NetBalance);
            Assert.Equal("Bilal owes Rs. 900.00", bilal.StatusDescription);
            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void ExpenseEdited_RecalculatedBalancesMatchNewSharesAndAmounts()
        {
            var members = CreateDefaultMembers();
            var expenseId = Guid.NewGuid();

            // Initial: Ali paid 3,000 for Ali, Ahmed, Bilal (1,000 each)
            var initialExpenses = new List<ExpenseRecord>
            {
                new(expenseId, _aliId, 3000m, false, new List<ShareRecord>
                {
                    new(_aliId, 1000m),
                    new(_ahmedId, 1000m),
                    new(_bilalId, 1000m)
                })
            };

            var initialBalances = GroupBalanceCalculator.CalculateBalances(members, initialExpenses, Enumerable.Empty<SettlementRecord>());
            Assert.Equal(-1000m, initialBalances.First(b => b.UserId == _bilalId).NetBalance);

            // Expense edited to 6,000 for Ali and Ahmed only (3,000 each; Bilal excluded)
            var editedExpenses = new List<ExpenseRecord>
            {
                new(expenseId, _aliId, 6000m, false, new List<ShareRecord>
                {
                    new(_aliId, 3000m),
                    new(_ahmedId, 3000m)
                })
            };

            var updatedBalances = GroupBalanceCalculator.CalculateBalances(members, editedExpenses, Enumerable.Empty<SettlementRecord>());

            var ali = updatedBalances.First(b => b.UserId == _aliId);
            var ahmed = updatedBalances.First(b => b.UserId == _ahmedId);
            var bilal = updatedBalances.First(b => b.UserId == _bilalId);

            Assert.Equal(3000m, ali.NetBalance);
            Assert.Equal(-3000m, ahmed.NetBalance);
            Assert.Equal(0m, bilal.NetBalance); // Bilal now settled up
            Assert.Equal(0m, updatedBalances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void ExpenseDeleted_ReversesFinancialImpact()
        {
            var members = CreateDefaultMembers();
            var expense1Id = Guid.NewGuid();
            var expense2Id = Guid.NewGuid();

            var expenses = new List<ExpenseRecord>
            {
                // Expense 1: Ali paid 3,000 for Ali, Ahmed, Bilal (1,000 each) - DELETED
                new(expense1Id, _aliId, 3000m, true, new List<ShareRecord>
                {
                    new(_aliId, 1000m),
                    new(_ahmedId, 1000m),
                    new(_bilalId, 1000m)
                }),
                // Expense 2: Ahmed paid 1,500 for Ali, Ahmed, Bilal (500 each) - ACTIVE
                new(expense2Id, _ahmedId, 1500m, false, new List<ShareRecord>
                {
                    new(_aliId, 500m),
                    new(_ahmedId, 500m),
                    new(_bilalId, 500m)
                })
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, Enumerable.Empty<SettlementRecord>());

            var ali = balances.First(b => b.UserId == _aliId);
            var ahmed = balances.First(b => b.UserId == _ahmedId);
            var bilal = balances.First(b => b.UserId == _bilalId);

            // Only Expense 2 counts
            Assert.Equal(-500m, ali.NetBalance);
            Assert.Equal(1000m, ahmed.NetBalance);
            Assert.Equal(-500m, bilal.NetBalance);
            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void MemberRemovedWithOutstandingBalance_HistoricalBalancePreserved()
        {
            // Ahmed is removed from the group (IsActive = false), but participated in an expense
            var members = new List<MemberRecord>
            {
                new(_aliId, "Ali", "ali@spendwise.com", "Owner", true),
                new(_ahmedId, "Ahmed", "ahmed@spendwise.com", "Member", false), // Removed
                new(_bilalId, "Bilal", "bilal@spendwise.com", "Member", true)
            };

            var expenses = new List<ExpenseRecord>
            {
                new(_aliId, _aliId, 3000m, false, new List<ShareRecord>
                {
                    new(_aliId, 1000m),
                    new(_ahmedId, 1000m),
                    new(_bilalId, 1000m)
                })
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, Enumerable.Empty<SettlementRecord>());

            var ahmed = balances.First(b => b.UserId == _ahmedId);
            Assert.False(ahmed.IsActive);
            Assert.Equal(-1000m, ahmed.NetBalance); // Ahmed still owes 1000
            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void SettlementRecorded_ReducesPayerDebtAndReceiverCredit()
        {
            // Initial: Ali paid 3,000 for Ali, Ahmed, Bilal (1,000 each) => Ali +2,000, Ahmed -1,000, Bilal -1,000
            // Ahmed pays settlement of 1,000 to Ali
            var members = CreateDefaultMembers();
            var expenses = new List<ExpenseRecord>
            {
                new(Guid.NewGuid(), _aliId, 3000m, false, new List<ShareRecord>
                {
                    new(_aliId, 1000m),
                    new(_ahmedId, 1000m),
                    new(_bilalId, 1000m)
                })
            };

            var settlements = new List<SettlementRecord>
            {
                new(Guid.NewGuid(), _ahmedId, _aliId, 1000m, false)
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, settlements);

            var ali = balances.First(b => b.UserId == _aliId);
            var ahmed = balances.First(b => b.UserId == _ahmedId);
            var bilal = balances.First(b => b.UserId == _bilalId);

            // Ahmed paid 1,000 settlement: goes from -1,000 to 0 (Settled up!)
            Assert.Equal(0m, ahmed.NetBalance);
            Assert.Equal("Settled up", ahmed.StatusDescription);

            // Ali received 1,000 settlement: goes from +2,000 to +1,000 (still owed 1,000 by Bilal)
            Assert.Equal(1000m, ali.NetBalance);

            // Bilal didn't pay yet, still owes 1,000
            Assert.Equal(-1000m, bilal.NetBalance);

            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }
    }
}
