using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.PersonalCapital;
using MeMetrics.Updater.Application.Profiles;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Serilog;
using Xunit;

namespace MeMetrics.Updater.Application.Tests
{
    public class TransactionUpdaterTests
    {

        public static Mock<ILogger> _loggerMock;
        public static Mock<IMeMetricsApi> _memetricsApiMock;
        public static Mock<IGmailApi> _gmailApiMock;
        public static Mock<IPersonalCapitalApi> _personalCapitalApiMock;
        public static IMapper _mapper;
        public TransactionUpdaterTests()
        {
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<TransactionProfile>(); });
            _loggerMock = new Mock<ILogger>();
            _mapper = new Mapper(configuration);
            _memetricsApiMock = new Mock<IMeMetricsApi>();
            _gmailApiMock = new Mock<IGmailApi>();
            _personalCapitalApiMock = new Mock<IPersonalCapitalApi>();
        }
        [Fact]
        public async Task GetAndSaveTransactions_ShouldSaveTransactionsCorrectly()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Personal_Capital_PMData = "Data",
                Personal_Capital_Password = "Password",
                Personal_Capital_Username = "UserName"
            });

            var updater = new TransactionUpdater(
                _loggerMock.Object, 
                config, 
                _mapper,
                _gmailApiMock.Object,
                _personalCapitalApiMock.Object,
                _memetricsApiMock.Object);


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

            _personalCapitalApiMock.Setup(x => x.GetUserTransactions(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new UserTransactions()
            {
                SpData = new TransactionData()
                {
                    Transactions = new List<Transaction>()
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

            Func<List<Objects.MeMetrics.Transaction>, bool> validate = transactions => {
                Assert.Equal(JsonConvert.SerializeObject(expectedTransaction), JsonConvert.SerializeObject(transactions.First()));
                return true;
            };

            await updater.GetAndSaveTransactions();

            _personalCapitalApiMock.Verify(x => x.Authenticate(config.Value.Personal_Capital_Username, config.Value.Personal_Capital_Password, config.Value.Personal_Capital_PMData), Times.Once);
            _memetricsApiMock.Verify(x => x.SaveTransactions(It.Is<List<Objects.MeMetrics.Transaction>>(transactions => validate(transactions))), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveTransactions_ShouldReturnSuccessfully_WhenCatchingException()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Personal_Capital_PMData = "Data",
                Personal_Capital_Password = "Password",
                Personal_Capital_Username = "UserName"
            });

            _personalCapitalApiMock.Setup(x => x.GetUserTransactions(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception());

            var updater = new TransactionUpdater(
                _loggerMock.Object,
                config,
                _mapper,
                _gmailApiMock.Object,
                _personalCapitalApiMock.Object,
                _memetricsApiMock.Object);


            var response = await updater.GetAndSaveTransactions();

            Assert.False(response.Successful);
        }
    }
}
