using System.Threading.Tasks;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface ICallUpdater
    {
        Task GetAndSaveCalls();
    }
}