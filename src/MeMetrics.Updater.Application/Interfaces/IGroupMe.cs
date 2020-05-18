using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects.GroupMe;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IGroupMeApi
    {
        void Authenticate(string token);
        Task<GroupResponse> GetGroups();
        Task<MessageResponse> GetMessages(string groupId, string lastMessageId = null);
    }
}