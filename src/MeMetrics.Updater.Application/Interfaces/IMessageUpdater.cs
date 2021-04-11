using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IMessageUpdater
    {
        Task<UpdaterResponse> GetAndSaveMessages();
    }
}