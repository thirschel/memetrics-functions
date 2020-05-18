using System.Collections.Generic;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects.PersonalCapital;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IPersonalCapitalApi
    {
        Task Authenticate(string username, string password, string pmData);
        Task<AccountsOverview> GetAccounts();
        Task<UserTransactions> GetUserTransactions(string startDate, string endDate, List<string> userAccountIds);
    }
}