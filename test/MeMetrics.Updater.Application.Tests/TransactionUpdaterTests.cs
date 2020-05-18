using System;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.PersonalCapital;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Serilog;
using Xunit;

namespace MeMetrics.Updater.Application.Tests
{
    public class TransactionUpdaterTests
    {
        [Fact]
        public async Task GetAndSaveTransactions_ShouldSaveTransactionsCorrectly()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var loggerMock = new Mock<ILogger>();
            var gmailApiMock = new Mock<IGmailApi>();
            var personalCapitalApiMock = new Mock<IPersonalCapitalApi>();

            var config = Options.Create(new EnvironmentConfiguration()
            {
                Personal_Capital_PMData = "Data",
                Personal_Capital_Password = "Password",
                Personal_Capital_Username = "UserName"
            });
            var updater = new TransactionUpdater(
                loggerMock.Object, 
                config, 
                gmailApiMock.Object, 
                personalCapitalApiMock.Object, 
                memetricsApiMock.Object);


            var userTransactionId = "1";
            var accountId = "2";
            var accountName = "Name";
            var amount = 3.50;
            var categoryId = 1;
            var description = "Desc";
            var isCashIn = false;
            var isCashOut = true;
            var merchantId = "Merchant";
            var transactionDate = DateTimeOffset.Now;

            personalCapitalApiMock.Setup(x => x.GetUserTransactions(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new UserTransactions()
            {
                SpData = new TransactionData()
                {
                    Transactions = new Transaction[]
                    {
                        new Transaction()
                        {
                            UserTransactionId = userTransactionId,
                            AccountId = accountId,
                            AccountName = accountName,
                            Amount = amount,
                            CategoryId = categoryId,
                            Description = description,
                            IsCashIn = isCashIn,
                            IsCashOut = isCashOut,
                            MerchantId = merchantId,
                            TransactionDate = transactionDate,
                        }
                    }
                }
            });

            var expectedTransaction = new Objects.MeMetrics.Transaction()
            {
                TransactionId = userTransactionId,
                AccountId = accountId,
                AccountName = accountName,
                Amount = (decimal) amount,
                CategoryId = (int) categoryId,
                Description = description,
                IsCashIn = isCashIn,
                IsCashOut = isCashOut,
                MerchantId = merchantId,
                OccurredDate = transactionDate,
            };

            Func<Objects.MeMetrics.Transaction, bool> validate = transaction => {
                Assert.Equal(JsonConvert.SerializeObject(expectedTransaction), JsonConvert.SerializeObject(transaction));
                return true;
            };

            await updater.GetAndSaveTransactions();

            personalCapitalApiMock.Verify(x => x.Authenticate(config.Value.Personal_Capital_Username, config.Value.Personal_Capital_Password, config.Value.Personal_Capital_PMData), Times.Once);
            memetricsApiMock.Verify(x => x.SaveTransaction(It.Is<Objects.MeMetrics.Transaction>(x => validate(x))), Times.Once);
        }
    }
}
