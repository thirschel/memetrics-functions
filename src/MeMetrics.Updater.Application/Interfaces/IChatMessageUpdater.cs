using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IChatMessageUpdater
    {
        Task<UpdaterResponse> GetAndSaveChatMessages();
    }
}