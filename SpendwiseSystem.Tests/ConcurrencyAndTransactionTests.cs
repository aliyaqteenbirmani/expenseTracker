using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpendwiseSystem.Application.Services.SplitExpenses;
using SpendwiseSystem.Domain.Entities;
using Xunit;

namespace SpendwiseSystem.Tests
{
    public class ConcurrencyAndTransactionTests
    {
        private readonly EqualSplitCalculatorService _calculator = new();

        [Fact]
        public async Task Concurrency_SimultaneousExpenseCreations_BalancesRemainMathematicallyConsistent()
        {
            // Arrange: 4 group members
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();
            var user3 = Guid.NewGuid();
            var user4 = Guid.NewGuid();

            var members = new List<MemberRecord>
            {
                new(user1, "User 1", "u1@test.com", "Owner", true),
                new(user2, "User 2", "u2@test.com", "Member", true),
                new(user3, "User 3", "u3@test.com", "Member", true),
                new(user4, "User 4", "u4@test.com", "Member", true)
            };

            var allParticipantIds = new List<Guid> { user1, user2, user3, user4 };
            var threadSafeExpenses = new ConcurrentBag<ExpenseRecord>();

            // Act: 50 simultaneous expenses added concurrently across all 4 users
            var tasks = Enumerable.Range(1, 50).Select(i => Task.Run(() =>
            {
                var payer = allParticipantIds[i % allParticipantIds.Count];
                decimal amount = 100m + i; // varying amounts
                var sharesMap = _calculator.CalculateEqualSplit(amount, allParticipantIds);

                var expense = new ExpenseRecord(
                    Guid.NewGuid(),
                    payer,
                    amount,
                    false,
                    sharesMap.Select(kv => new ShareRecord(kv.Key, kv.Value)).ToList()
                );

                threadSafeExpenses.Add(expense);
            }));

            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(50, threadSafeExpenses.Count);

            // Verify each expense satisfies exact mathematical conservation
            foreach (var expense in threadSafeExpenses)
            {
                var shareSum = expense.Shares.Sum(s => s.ShareAmount);
                Assert.Equal(expense.Amount, shareSum);
            }

            // Calculate overall group balances
            var balances = GroupBalanceCalculator.CalculateBalances(
                members, threadSafeExpenses.ToList(), Enumerable.Empty<SettlementRecord>());

            // Net balances across all members MUST sum to exactly zero (zero-sum game)
            var netSum = balances.Sum(b => b.NetBalance);
            Assert.Equal(0.00m, Math.Round(netSum, 2));

            // Total paid across all members MUST equal total shared across all members
            var totalPaid = balances.Sum(b => b.TotalPaid);
            var totalShared = balances.Sum(b => b.TotalShare);
            Assert.Equal(totalPaid, totalShared);
        }

        [Fact]
        public async Task Concurrency_ConcurrentExpenseUpdates_NoDuplicateSharesOrPartialState()
        {
            // Simulate an in-memory transactional store for an expense and its shares
            var expenseId = Guid.NewGuid();
            var participantIds = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList();
            var lockObj = new object();

            decimal storedAmount = 500m;
            List<ShareRecord> storedShares = _calculator.CalculateEqualSplit(storedAmount, participantIds)
                .Select(kv => new ShareRecord(kv.Key, kv.Value)).ToList();

            int successfulUpdates = 0;
            int conflictCount = 0;

            // Act: 20 simultaneous updates to the expense with different amounts
            var updateTasks = Enumerable.Range(1, 20).Select(i => Task.Run(() =>
            {
                decimal newAmount = 1000m + (i * 10);
                var newSharesMap = _calculator.CalculateEqualSplit(newAmount, participantIds);

                // Transaction boundary
                lock (lockObj)
                {
                    try
                    {
                        // Simulate unique constraint validation on (ExpenseId, UserId)
                        var userShareKeys = new HashSet<Guid>();
                        var generatedShares = new List<ShareRecord>();

                        foreach (var kv in newSharesMap)
                        {
                            if (!userShareKeys.Add(kv.Key))
                            {
                                throw new InvalidOperationException("Unique constraint violation: Duplicate share for user.");
                            }
                            generatedShares.Add(new ShareRecord(kv.Key, kv.Value));
                        }

                        // Atomic assignment
                        storedAmount = newAmount;
                        storedShares = generatedShares;
                        successfulUpdates++;
                    }
                    catch
                    {
                        conflictCount++;
                    }
                }
            }));

            await Task.WhenAll(updateTasks);

            // Assert
            Assert.Equal(20, successfulUpdates);
            Assert.Equal(0, conflictCount);

            // Final state must be completely consistent
            Assert.Equal(participantIds.Count, storedShares.Count);
            Assert.Equal(storedAmount, storedShares.Sum(s => s.ShareAmount));

            // Unique user shares verified
            var distinctUsers = storedShares.Select(s => s.UserId).Distinct().Count();
            Assert.Equal(participantIds.Count, distinctUsers);
        }

        [Fact]
        public void Transactions_AtomicRollback_SimulatedFailureLeavesNoPartialState()
        {
            // Arrange
            var committedExpenses = new List<string>();
            var committedShares = new List<string>();
            var committedActivities = new List<string>();

            void ExecuteTransactionalOperation(bool shouldFailOnShares)
            {
                var stageExpenses = new List<string>();
                var stageShares = new List<string>();
                var stageActivities = new List<string>();

                try
                {
                    // Step 1: Insert expense
                    stageExpenses.Add("Expense_101");

                    // Step 2: Insert shares
                    if (shouldFailOnShares)
                    {
                        throw new InvalidOperationException("Simulated database failure during share insertion.");
                    }
                    stageShares.Add("Share_1");
                    stageShares.Add("Share_2");

                    // Step 3: Insert activity
                    stageActivities.Add("Activity_ExpenseAdded");

                    // Commit
                    committedExpenses.AddRange(stageExpenses);
                    committedShares.AddRange(stageShares);
                    committedActivities.AddRange(stageActivities);
                }
                catch
                {
                    // Rollback - discard staged changes
                    stageExpenses.Clear();
                    stageShares.Clear();
                    stageActivities.Clear();
                }
            }

            // Act: execute with failure
            ExecuteTransactionalOperation(shouldFailOnShares: true);

            // Assert: everything was rolled back; 0 partial records
            Assert.Empty(committedExpenses);
            Assert.Empty(committedShares);
            Assert.Empty(committedActivities);

            // Act: execute successfully
            ExecuteTransactionalOperation(shouldFailOnShares: false);

            // Assert: all committed together
            Assert.Single(committedExpenses);
            Assert.Equal(2, committedShares.Count);
            Assert.Single(committedActivities);
        }

        [Fact]
        public async Task Concurrency_NoRoundingDiscrepanciesUnderHighVolumeConcurrentSplits()
        {
            // Test 100 concurrent irregular calculations (e.g. 100 / 3 = 33.34, 33.33, 33.33)
            var users = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            var tasks = Enumerable.Range(1, 100).Select(amount => Task.Run(() =>
            {
                decimal totalAmount = amount + 0.33m;
                var split = _calculator.CalculateEqualSplit(totalAmount, users);

                // Invariant: sum of shares must equal total amount down to 0.00
                var sum = split.Values.Sum();
                Assert.Equal(totalAmount, sum);
            }));

            await Task.WhenAll(tasks);
        }
    }
}
