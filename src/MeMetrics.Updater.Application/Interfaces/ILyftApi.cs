using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects.Lyft;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface ILyftApi
    {
        Task<RideHistoryResponse> GetRides(string startTime, string endTime);

        Task<PassengerRidesResponse> GetPassengerRides();
    }
}