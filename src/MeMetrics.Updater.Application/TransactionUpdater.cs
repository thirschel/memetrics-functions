using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Helpers;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Serilog;

namespace MeMetrics.Updater.Application
{
    public class TransactionUpdater : ITransactionUpdater
    {
        private readonly ILogger _logger;
        private readonly IGmailApi _gmailApi;
        private readonly IPersonalCapitalApi _personalCapitalApi;
        private readonly IMeMetricsApi _memetricsApi;
        private readonly IOptions<EnvironmentConfiguration> _configuration;

        public TransactionUpdater(
            ILogger logger, 
            IOptions<EnvironmentConfiguration> configuration, 
            IGmailApi gmailApi,
            IPersonalCapitalApi personalCapitalApi,
            IMeMetricsApi memetricsApi)
        {
            _logger = logger;
            _configuration = configuration;
            _gmailApi = gmailApi;
            _personalCapitalApi = personalCapitalApi;
            _memetricsApi = memetricsApi;
        }

        public async Task GetAndSaveTransactions()
        {
            int transactionCount = 0;
            await _personalCapitalApi.Authenticate(_configuration.Value.Personal_Capital_Username, _configuration.Value.Personal_Capital_Password, _configuration.Value.Personal_Capital_PMData);

            var startDate = DateTime.Now.AddDays(-2);
            var endDate = DateTime.Now;
            var startTime = $"{startDate.Year}-{Utility.AddPadding(startDate.Month)}-{Utility.AddPadding(startDate.Day)}";
            var endTime = $"{endDate.Year}-{Utility.AddPadding(endDate.Month)}-{Utility.AddPadding(endDate.Day)}";
            var transactions = await _personalCapitalApi.GetUserTransactions(startTime, endTime);
            if (transactions.SpData?.Transactions != null)
            {
                foreach (var transactionData in transactions.SpData?.Transactions)
                {
                    var transaction = new Transaction()
                    {
                        TransactionId = transactionData.UserTransactionId,
                        AccountId = transactionData.AccountId,
                        AccountName = transactionData.AccountName,
                        Amount = (decimal) transactionData.Amount,
                        CategoryId = (int) transactionData.CategoryId,
                        Description = transactionData.Description,
                        IsCashIn = transactionData.IsCashIn,
                        IsCashOut = transactionData.IsCashOut,
                        MerchantId = transactionData.MerchantId,
                        OccurredDate = transactionData.TransactionDate,
                    };
                    await _memetricsApi.SaveTransaction(transaction);
                    transactionCount++;
                }
            }

            _logger.Information($"{transactionCount} transactions successfully saved.");
        }
    }
}