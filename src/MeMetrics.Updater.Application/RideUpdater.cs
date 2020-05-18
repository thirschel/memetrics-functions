using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using MeMetrics.Updater.Application.Helpers;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.Enums;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Serilog;

namespace MeMetrics.Updater.Application
{
    public class RideUpdater : IRideUpdater
    {
        private int uberTransactionCount = 0;
        private int lyftTransactionCount = 0;

        private readonly ILogger _logger;
        private readonly IOptions<EnvironmentConfiguration> _configuration;
        private readonly ILyftApi _lyftApi;
        private readonly IUberRidersApi _uberRidersApi;
        private readonly IMeMetricsApi _memetricsApi;

        public RideUpdater(
            ILogger logger, 
            IOptions<EnvironmentConfiguration> configuration,
            ILyftApi lyftApi,
            IUberRidersApi uberRidersApi,
            IMeMetricsApi memetricsApi)
        {
            _logger = logger;
            _configuration = configuration;
            _lyftApi = lyftApi;
            _uberRidersApi = uberRidersApi;
            _memetricsApi = memetricsApi;
        }

        public async Task GetAndSaveUberRides()
        {
            await _uberRidersApi.Authenticate(_configuration.Value.Uber_Cookie, _configuration.Value.Uber_User_Id);
            await ProcessUberRides(0);
            _logger.Information($"{uberTransactionCount} uber transactions successfully saved");
        }

        public async Task ProcessUberRides(int offset)
        {
            var trips = await _uberRidersApi.GetTrips(offset);
            var hasFoundAllTodaysRides = false;
            if (!trips.Data.Trips.TripsTrips.Any())
            {
                return;
            }

            foreach (var trip in trips.Data.Trips.TripsTrips)
            {
                if (hasFoundAllTodaysRides)
                {
                    continue;
                }

                hasFoundAllTodaysRides = DateTimeOffset.FromUnixTimeSeconds(trip.RequestTime.ToUnixTimeSeconds()).Date <
                                         DateTimeOffset.UtcNow.AddDays(-2);
                var details = await _uberRidersApi.GetTripDetails(trip.Uuid.ToString());
                
                if (details.Data.Trip.VehicleViewName.Contains("UberEATS") || trip.Status != "COMPLETED" || trip.Status == "CANCELED")
                {
                    continue;
                }

                var pickUpCoordinates = new[] {"0.00000000", "0.00000000"};
                var dropOffCoordinates = new[] {"0.00000000", "0.00000000"};
                //The rider api doesn't provider the lat and long as properties but they are a part of the google maps url
                if (details.Data.TripMap != null && details.Data.TripMap.Url != null)
                {
                    var mapString = HttpUtility.UrlDecode(details.Data.TripMap.Url.ToString());
                    var regex = new Regex("(-?\\d{1,3}.\\d+,-?\\d{1,3}.\\d+)");
                    pickUpCoordinates = regex.Match(mapString).Groups[0].ToString().Split(',');
                    dropOffCoordinates = regex.Match(mapString).Groups[1].ToString().Split(',');
                }

                var tripDurations = Regex.Match(details.Data.Receipt.Duration, @"(\d+):(\d+):(\d+)");
                var timespan = new TimeSpan(0, int.Parse(tripDurations.Groups[1].Value),
                    int.Parse(tripDurations.Groups[2].Value), int.Parse(tripDurations.Groups[3].Value));
                var pickupDate = new DateTimeOffset(trip.DropoffTime.Value.DateTime, trip.DropoffTime.Value.Offset)
                    .Subtract(timespan);


                var ride = new Ride()
                {
                    RideId = trip.Uuid.ToString(),
                    RideType = RideType.Uber,
                    Distance = decimal.Parse(details.Data.Receipt.Distance),
                    RequestDate = trip.RequestTime,
                    PickupDate = pickupDate,
                    DropoffDate = trip.DropoffTime,
                    Price = (decimal) details.Data.Trip.ClientFare,
                    OriginLat = pickUpCoordinates[0].Substring(0, Math.Min(pickUpCoordinates[0].Length, 10)),
                    OriginLong = pickUpCoordinates[1].Substring(0, Math.Min(pickUpCoordinates[1].Length, 10)),
                    DestinationLat = dropOffCoordinates[0].Substring(0, Math.Min(dropOffCoordinates[0].Length, 10)),
                    DestinationLong = dropOffCoordinates[1].Substring(0, Math.Min(dropOffCoordinates[1].Length, 10)),
                };

                await _memetricsApi.SaveRide(ride);
                uberTransactionCount++;
            }

            if (!hasFoundAllTodaysRides)
            {
                await ProcessUberRides(offset + trips.Data.Trips.TripsTrips.Length);
            }
        }

        public async Task GetAndSaveLyftRides()
        {
            //await _lyftApi.RefreshAndSetOAuthToken();

            await ProcessLyftRides(DateTime.Now);
            _logger.Information($"{lyftTransactionCount} lyft transactions successfully saved");
        }

        public async Task ProcessLyftRides(DateTime endDate)
        {
            var startDate = endDate.AddDays(-200);
            var startTime = $"{startDate.Year}-{Utility.AddPadding(startDate.Month)}-{Utility.AddPadding(startDate.Day)}T00:00:00Z";
            var endTime = $"{endDate.Year}-{Utility.AddPadding(endDate.Month)}-{Utility.AddPadding(endDate.Day)}T00:00:00Z";

            var rides = await _lyftApi.GetRides(startTime, endTime);
            if (rides?.RideHistory != null)
                foreach (var ride in rides?.RideHistory)
                {
                    var insertRide = new Ride()
                    {
                        RideId = ride.RideId,
                        RideType = RideType.Lyft,
                        OriginLat = (ride.Pickup?.Lat).ToString(),
                        OriginLong = (ride.Pickup?.Lng).ToString(),
                        DestinationLat = ride.Destination.Lat.ToString(),
                        DestinationLong = ride.Destination.Lng.ToString(),
                        Distance = (decimal?) ride.DistanceMiles,
                        Price = (decimal) ride.Price.Amount / 100,
                        RequestDate = ride.RequestedAt,
                        PickupDate = ride.Pickup?.Time,
                        DropoffDate = ride.Dropoff?.Time,
                    };
                    await _memetricsApi.SaveRide(insertRide);
                    lyftTransactionCount++;
                }
        }
    }
}