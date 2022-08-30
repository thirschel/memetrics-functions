using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
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
        private readonly IOptions<EnvironmentConfiguration> _configuration;
        private readonly IMapper _mapper;
        private readonly IPersonalCapitalApi _personalCapitalApi;
        private readonly IMeMetricsApi _memetricsApi;
        private readonly int _daysToQuery = 2;


        public TransactionUpdater(
            ILogger logger, 
            IOptions<EnvironmentConfiguration> configuration,
            IMapper mapper,
            IGmailApi gmailApi,
            IPersonalCapitalApi personalCapitalApi,
            IMeMetricsApi memetricsApi)
        {
            _logger = logger;
            _configuration = configuration;
            _personalCapitalApi = personalCapitalApi;
            _memetricsApi = memetricsApi;
            _mapper = mapper;
        }

        public async Task<UpdaterResponse> GetAndSaveTransactions()
        {
            try {
                _logger.Information("Starting transaction updater");
                int transactionCount = 0;
                await _personalCapitalApi.Authenticate(_configuration.Value.Personal_Capital_Username, _configuration.Value.Personal_Capital_Password, _configuration.Value.Personal_Capital_PMData);

                var startDate = DateTime.Now.AddDays(-_daysToQuery);
                var endDate = DateTime.Now;
                var startTime = $"{startDate.Year}-{Utility.AddPadding(startDate.Month)}-{Utility.AddPadding(startDate.Day)}";
                var endTime = $"{endDate.Year}-{Utility.AddPadding(endDate.Month)}-{Utility.AddPadding(endDate.Day)}";
                var transactions = await _personalCapitalApi.GetUserTransactions(startTime, endTime);
                if (transactions.SpData?.Transactions != null)
                {
                    var transactionsToSave = new List<Transaction>();
                    foreach (var transactionDataChunk in Utility.SplitList(transactions.SpData?.Transactions, 100))
                    {
                        foreach(var transactionData in transactionDataChunk)
                        {
                            var transaction = _mapper.Map<Transaction>(transactionData);
                            transactionsToSave.Add(transaction);
                        }
                        await _memetricsApi.SaveTransactions(transactionsToSave);
                        transactionCount+= transactionsToSave.Count;
                    }
                }

                _logger.Information($"Finished transaction updater. {transactionCount} transactions successfully saved.");
                return new UpdaterResponse() { Successful = true };
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to get and save transactions");
                return new UpdaterResponse() { Successful = false, ErrorMessage = e.Message };
            }
        }
    }
}