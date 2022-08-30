using System.Collections.Generic;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects.MeMetrics;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IMeMetricsApi
    {
        Task SaveCalls(IList<Call> calls);
        Task SaveMessages(IList<Message> messages);
        Task SaveChatMessage(ChatMessage chatMessage);
        Task SaveRide(Ride ride);
        Task SaveTransactions(List<Transaction> transactions);
        Task SaveRecruitmentMessages(List<RecruitmentMessage> recruitmentMessages);
        Task Cache();
    }
}