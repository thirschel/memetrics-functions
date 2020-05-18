using System.Threading.Tasks;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface ITransactionUpdater
    {
        Task GetAndSaveTransactions();
    }
}