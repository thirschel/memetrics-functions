using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects.MeMetrics;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IMeMetricsApi
    {
        Task SaveCall(Call call);
        Task SaveMessage(Message call);
        Task SaveChatMessage(ChatMessage call);
        Task SaveRide(Ride call);
        Task SaveTransaction(Transaction call);
        Task SaveRecruitmentMessage(RecruitmentMessage call);
        Task Cache();
    }
}