using System.Threading.Tasks;
using Google.Apis.Gmail.v1.Data;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IGmailApi
    {
        Task Authenticate(string refreshToken);
        Task<Message> GetEmail(string id);
        Task<string> GetAttachment(string messageId, string attachmentId);
        Task<ListLabelsResponse> GetLabels();
        Task<ListMessagesResponse> GetEmails(string labelId, string pageToken = null);
    }
}