using SpendwiseSystem.Application.Services.SplitExpenses;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SpendwiseSystem.Tests
{
    public class EqualSplitCalculatorTests
    {
        private readonly EqualSplitCalculatorService _calculator = new();

        [Fact]
        public void CalculateEqualSplit_100DividedBy3_ProducesExactSumAndCorrectDistribution()
        {
            // Arrange
            const decimal amount = 100.00m;
            var user1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var user2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var user3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var participants = new List<Guid> { user1, user2, user3 };

            // Act
            var shares = _calculator.CalculateEqualSplit(amount, participants);

            // Assert
            Assert.Equal(3, shares.Count);
            Assert.Equal(33.34m, shares[user1]); // lowest sorted guid gets extra remainder cent
            Assert.Equal(33.33m, shares[user2]);
            Assert.Equal(33.33m, shares[user3]);
            Assert.Equal(amount, shares.Values.Sum());
        }

        [Fact]
        public void CalculateEqualSplit_100DividedBy6_ProducesExactSumAndFourRemainders()
        {
            // Arrange
            const decimal amount = 100.00m;
            var participants = Enumerable.Range(1, 6)
                .Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D12}"))
                .ToList();

            // Act
            var shares = _calculator.CalculateEqualSplit(amount, participants);

            // Assert
            Assert.Equal(6, shares.Count);
            Assert.Equal(amount, shares.Values.Sum());

            // 100 / 6 = 16.66 base; 100 - (16.66 * 6) = 0.04 remainder => four 16.67 and two 16.66
            var count1667 = shares.Values.Count(v => v == 16.67m);
            var count1666 = shares.Values.Count(v => v == 16.66m);

            Assert.Equal(4, count1667);
            Assert.Equal(2, count1666);
        }

        [Fact]
        public void CalculateEqualSplit_10DividedBy3_ProducesExactSum()
        {
            // Arrange
            const decimal amount = 10.00m;
            var participants = Enumerable.Range(1, 3)
                .Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D12}"))
                .ToList();

            // Act
            var shares = _calculator.CalculateEqualSplit(amount, participants);

            // Assert
            Assert.Equal(3, shares.Count);
            Assert.Equal(amount, shares.Values.Sum());
            Assert.Equal(3.34m, shares.Values.Max());
            Assert.Equal(3.33m, shares.Values.Min());
        }

        [Fact]
        public void CalculateEqualSplit_OneCentDividedBy3_ProducesExactSumWithoutUnderflow()
        {
            // Arrange
            const decimal amount = 0.01m;
            var participants = Enumerable.Range(1, 3)
                .Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D12}"))
                .ToList();

            // Act
            var shares = _calculator.CalculateEqualSplit(amount, participants);

            // Assert
            Assert.Equal(3, shares.Count);
            Assert.Equal(amount, shares.Values.Sum());
            Assert.Equal(1, shares.Values.Count(v => v == 0.01m));
            Assert.Equal(2, shares.Values.Count(v => v == 0.00m));
        }

        [Theory]
        [InlineData(1000.00, 1)]
        [InlineData(500.00, 2)]
        [InlineData(4000.00, 3)]
        [InlineData(10000.00, 7)]
        [InlineData(9999999.99, 7)]
        [InlineData(1000000.00, 3)]
        [InlineData(50000000.00, 17)]
        [InlineData(98765432.10, 43)]
        [InlineData(0.05, 7)]
        [InlineData(12.37, 5)]
        public void CalculateEqualSplit_VariousAmountsAndCounts_AlwaysGuaranteesSumEqualsTotal(
            decimal amount, int participantCount)
        {
            // Arrange
            var participants = Enumerable.Range(1, participantCount)
                .Select(_ => Guid.NewGuid())
                .ToList();

            // Act
            var shares = _calculator.CalculateEqualSplit(amount, participants);

            // Assert
            Assert.Equal(participantCount, shares.Count);
            Assert.Equal(amount, shares.Values.Sum());

            // Ensure no share difference is greater than 1 cent (0.01)
            var maxShare = shares.Values.Max();
            var minShare = shares.Values.Min();
            Assert.True(maxShare - minShare <= 0.01m, "Difference between any two shares cannot exceed 0.01");
        }

        [Fact]
        public void CalculateEqualSplit_OnlyPayerSelected_ReturnsSingleFullShare()
        {
            // Arrange
            const decimal amount = 1000.00m;
            var payerId = Guid.NewGuid();
            var participants = new List<Guid> { payerId };

            // Act
            var shares = _calculator.CalculateEqualSplit(amount, participants);

            // Assert
            Assert.Single(shares);
            Assert.Equal(amount, shares[payerId]);
            Assert.Equal(amount, shares.Values.Sum());
        }

        [Fact]
        public void CalculateEqualSplit_IsDeterministicRegardlessOfInputOrder()
        {
            // Arrange
            const decimal amount = 100.00m;
            var userA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var userB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var userC = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

            var order1 = new List<Guid> { userC, userA, userB };
            var order2 = new List<Guid> { userB, userC, userA };

            // Act
            var shares1 = _calculator.CalculateEqualSplit(amount, order1);
            var shares2 = _calculator.CalculateEqualSplit(amount, order2);

            // Assert
            Assert.Equal(shares1[userA], shares2[userA]);
            Assert.Equal(shares1[userB], shares2[userB]);
            Assert.Equal(shares1[userC], shares2[userC]);
            Assert.Equal(33.34m, shares1[userA]); // userA is lowest Guid, gets extra cent
            Assert.Equal(33.33m, shares1[userB]);
            Assert.Equal(33.33m, shares1[userC]);
        }

        [Fact]
        public void CalculateEqualSplit_EmptyOrNullParticipants_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _calculator.CalculateEqualSplit(100m, new List<Guid>()));

            Assert.Throws<ArgumentException>(() =>
                _calculator.CalculateEqualSplit(100m, null!));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void CalculateEqualSplit_NonPositiveAmount_ThrowsArgumentException(decimal invalidAmount)
        {
            var participants = new List<Guid> { Guid.NewGuid() };
            Assert.Throws<ArgumentException>(() =>
                _calculator.CalculateEqualSplit(invalidAmount, participants));
        }
    }
}
