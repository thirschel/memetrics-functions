using System.Collections.Generic;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects.MeMetrics;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IMeMetricsApi
    {
        Task SaveCall(Call call);
        Task SaveMessage(IList<Message> message);
        Task SaveChatMessage(ChatMessage chatMessage);
        Task SaveRide(Ride ride);
        Task SaveTransaction(Transaction transaction);
        Task SaveRecruitmentMessage(RecruitmentMessage recruitmentMessage);
        Task Cache();
    }
}