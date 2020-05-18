using System.Threading.Tasks;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface ICacheUpdater
    {
        Task CacheMeMetrics();
    }
}