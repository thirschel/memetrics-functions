using System.Threading.Tasks;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IRecruitmentMessageUpdater
    {
        Task GetAndSaveLinkedInMessages();
        Task GetAndSaveEmailMessages();
    }
}