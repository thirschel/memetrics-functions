using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects.Lyft;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface ILyftApi
    {
        Task Authenticate(string cookie);
        Task<PassengerTrips> GetTrips(int skip = 0);
    }
}