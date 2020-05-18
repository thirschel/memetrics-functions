using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using Serilog;

namespace MeMetrics.Updater.Application
{
    public class CacheUpdater : ICacheUpdater
    {

        private readonly ILogger _logger;
        private readonly IMeMetricsApi _memetricsApi;

        public CacheUpdater(
            ILogger logger, 
            IMeMetricsApi memetricsApi)
        {
            _logger = logger;
            _memetricsApi = memetricsApi;
        }

        public async Task CacheMeMetrics()
        {
            await _memetricsApi.Cache();
            _logger.Information($"Cache successfully set");
        }
    }
}