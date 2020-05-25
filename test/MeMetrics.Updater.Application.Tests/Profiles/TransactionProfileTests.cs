using AutoMapper;
using Bogus;
using MeMetrics.Updater.Application.Profiles;
using Xunit;

namespace MeMetrics.Updater.Application.Tests.Profiles
{
    public class TransactionProfileTests
    {
        [Fact]
        public void PersonalCapitalTransaction_ShouldMapTo_Transaction()
        {
            // ARRANGE
            var transactionFaker = new Faker<Objects.PersonalCapital.Transaction>()
                .RuleFor(f => f.UserTransactionId, f => f.Random.String2(32))
                .RuleFor(f => f.AccountId, f => f.Random.String2(32))
                .RuleFor(f => f.AccountName, f => f.Random.String2(32))
                .RuleFor(f => f.MerchantId, f => f.Random.String2(100))
                .RuleFor(f => f.Amount, f => f.Random.Double())
                .RuleFor(f => f.Description, f => f.Random.String2(100))
                .RuleFor(f => f.IsCashIn, f => f.Random.Bool())
                .RuleFor(f => f.IsCashIn, f => f.Random.Bool())
                .RuleFor(f => f.CategoryId, f => f.Random.Int(100))
                .RuleFor(f => f.TransactionDate, f => f.Date.PastOffset());

            var transactionData = transactionFaker.Generate();
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<TransactionProfile>(); });
            var mapper = new Mapper(configuration);

            // ACT
            var transaction = mapper.Map<Objects.MeMetrics.Transaction>(transactionData);

            // ASSERT
            Assert.Equal(transactionData.UserTransactionId, transaction.TransactionId);
            Assert.Equal(transactionData.AccountId, transaction.AccountId);
            Assert.Equal(transactionData.AccountName, transaction.AccountName);
            Assert.Equal(transactionData.MerchantId, transaction.MerchantId);
            Assert.Equal((decimal) transactionData.Amount, transaction.Amount);
            Assert.Equal(transactionData.Description, transaction.Description);
            Assert.Equal(transactionData.IsCashIn, transaction.IsCashIn);
            Assert.Equal(transactionData.IsCashOut, transaction.IsCashOut);
            Assert.Equal(transactionData.CategoryId, transaction.CategoryId);
            Assert.Equal(transactionData.TransactionDate, transaction.OccurredDate);
        }
    }
}