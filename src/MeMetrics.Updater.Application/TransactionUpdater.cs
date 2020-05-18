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

            var result = await _personalCapitalApi.GetAccounts();
            var accountIds = result.SpData.Accounts.Select(a => a.UserAccountId).ToList();
            var startDate = DateTime.Now.AddDays(-2);
            var endDate = DateTime.Now;
            var startTime = $"{startDate.Year}-{Utility.AddPadding(startDate.Month)}-{Utility.AddPadding(startDate.Day)}";
            var endTime = $"{endDate.Year}-{Utility.AddPadding(endDate.Month)}-{Utility.AddPadding(endDate.Day)}";
            var transactions = await _personalCapitalApi.GetUserTransactions(startTime, endTime, accountIds);
            if (transactions.SpData?.Transactions != null)
            {
                foreach (var trans in transactions.SpData?.Transactions)
                {
                    var transaction = new Transaction()
                    {
                        TransactionId = trans.UserTransactionId,
                        AccountId = trans.AccountId,
                        AccountName = trans.AccountName,
                        Amount = (decimal) trans.Amount,
                        CategoryId = (int) trans.CategoryId,
                        Description = trans.Description,
                        IsCashIn = trans.IsCashIn,
                        IsCashOut = trans.IsCashOut,
                        MerchantId = trans.MerchantId,
                        OccurredDate = trans.TransactionDate.Date,
                    };
                    await _memetricsApi.SaveTransaction(transaction);
                    transactionCount++;
                }
            }

            _logger.Information($"{transactionCount} transactions successfully saved.");
        }

        private async Task AuthenticateChallenge()
        {
            // Wait for the email to be sent and received
            await Task.Delay(15000);
            
            var messages = await _gmailApi.GetEmails(_configuration.Value.Gmail_Personal_Capital_Label);
            var message = await _gmailApi.GetEmail(messages.Messages[0].Id);
            var body = Utility.Decode(message.Payload.Parts[0].Parts[0].Parts[0].Body.Data);
            var regex = new Regex("4-Digit Authorization Code: (\\d+)");
            var code = regex.Match(body).Groups[1].ToString();
            
            //await _personalCapitalApi.AuthenticateChallenge(code);
        }
    }
}