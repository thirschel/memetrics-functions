using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects.LinkedIn;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface ILinkedInApi
    {
        Task<bool> Authenticate(string username, string password);

        Task SubmitPin(string pin);

        Task<ConversationList> GetConversations(long createdBeforeTime);

        Task<ConversationEvents> GetConversationEvents(string conversationId);
    }
}