using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MeMetrics.Updater.Functions
{
    [ExcludeFromCodeCoverage]
    public class MeMetricsFunctions
    {
        private readonly IMessageUpdater _messageUpdater;
        private readonly ICallUpdater _callUpdater;
        private readonly IChatMessageUpdater _chatMessageUpdater;
        private readonly IRecruitmentMessageUpdater _recruitmentMessageUpdater;
        private readonly IRideUpdater _rideUpdater;
        private readonly ITransactionUpdater _transactionUpdater;
        private readonly ICacheUpdater _cacheUpdater;

        public MeMetricsFunctions(
            IMessageUpdater messageUpdater,
            ICallUpdater callUpdater,
            IChatMessageUpdater chatMessageUpdater,
            IRecruitmentMessageUpdater recruitmentMessageUpdater,
            IRideUpdater rideUpdater,
            ITransactionUpdater transactionUpdater,
            ICacheUpdater cacheUpdater
            )
        {
            _messageUpdater = messageUpdater;
            _callUpdater = callUpdater;
            _chatMessageUpdater = chatMessageUpdater;
            _recruitmentMessageUpdater = recruitmentMessageUpdater;
            _rideUpdater = rideUpdater;
            _transactionUpdater = transactionUpdater;
            _cacheUpdater = cacheUpdater;
        }

        // Timer function set to run at 1AM everyday. Time zone is set using WEBSITE_TIME_ZONE
        [FunctionName("UpdateMeMetrics")]
        public async Task Run(
            [TimerTrigger("0 0 1 * * *")]
            TimerInfo timer,
            ILogger logger)
        {
            // https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-error-pages#use-structured-error-handling
            try
            {
                var responses = new List<UpdaterResponse>
                {
                    await _messageUpdater.GetAndSaveMessages(),
                    await _callUpdater.GetAndSaveCalls(),
                    await _chatMessageUpdater.GetAndSaveChatMessages(),
                    await _recruitmentMessageUpdater.GetAndSaveEmailMessages(),
                    await _recruitmentMessageUpdater.GetAndSaveLinkedInMessages(),
                    await _transactionUpdater.GetAndSaveTransactions(),
                    await _rideUpdater.GetAndSaveUberRides(),
                    await _rideUpdater.GetAndSaveLyftRides()
                };

                await _cacheUpdater.CacheMeMetrics();
                if (responses.Exists(r => !r.Successful))
                {
                    throw new Exception($"{responses.Count(r => !r.Successful)} update(s) failed.");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message + " stackTrack={stackTrace}", e?.StackTrace);
                throw (e);
            }
        }
    }
}