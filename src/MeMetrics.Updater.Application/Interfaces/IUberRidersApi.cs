using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects.Uber;
using Uber.API.Objects;

namespace MeMetrics.Updater.Application.Interfaces
{
    public interface IUberRidersApi
    {
        Task Authenticate(string cookie, string userId);
        Task<TripsResponse> GetTrips(int offset = 0);
        Task<TripsDetailResponse> GetTripDetails(string tripId);
    }
}