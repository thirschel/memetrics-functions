using System.Threading.Tasks;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IRideUpdater
    {
        Task GetAndSaveUberRides();
        Task GetAndSaveLyftRides();
    }
}