using System.Threading.Tasks;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IChatMessageUpdater
    {
        Task GetAndSaveChatMessages();
    }
}