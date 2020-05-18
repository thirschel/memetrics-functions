using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using Moq;
using Serilog;
using Xunit;

namespace MeMetrics.Updater.Application.Tests
{
    public class CacheUpdaterTests
    {
        [Fact]
        public async Task CacheMeMetrics_ShouldCallMeMetricsApiCache()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var loggerMock = new Mock<ILogger>();
            var updater = new CacheUpdater(loggerMock.Object, memetricsApiMock.Object);
            await updater.CacheMeMetrics();

            memetricsApiMock.Verify(x => x.Cache(), Times.Once);
        }
    }
}
