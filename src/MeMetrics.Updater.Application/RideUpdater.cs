﻿using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Serilog;

namespace MeMetrics.Updater.Application
{
    public class RideUpdater : IRideUpdater
    {
        private readonly ILogger _logger;
        private readonly IOptions<EnvironmentConfiguration> _configuration;
        private readonly ILyftApi _lyftApi;
        private readonly IUberRidersApi _uberRidersApi;
        private readonly IMeMetricsApi _memetricsApi;
        private readonly IMapper _mapper;

        public RideUpdater(
            ILogger logger,
            IOptions<EnvironmentConfiguration> configuration,
            ILyftApi lyftApi,
            IUberRidersApi uberRidersApi,
            IMeMetricsApi memetricsApi,
            IMapper mapper)
        {
            _logger = logger;
            _configuration = configuration;
            _lyftApi = lyftApi;
            _uberRidersApi = uberRidersApi;
            _memetricsApi = memetricsApi;
            _mapper = mapper;
        }

        public async Task GetAndSaveUberRides()
        {
            await _uberRidersApi.Authenticate(_configuration.Value.Uber_Cookie, _configuration.Value.Uber_User_Id);
            var transactions = await ProcessUberRides(0, 0);
            _logger.Information($"{transactions} uber transactions successfully saved");
        }

        public async Task<int> ProcessUberRides(int offset, int transactionCount)
        {
            var trips = await _uberRidersApi.GetTrips(offset);
            var hasFoundAllTodaysRides = false;
            if (trips?.Data?.Trips?.TripsTrips == null)
            {
                return transactionCount;
            }

            foreach (var trip in trips.Data.Trips.TripsTrips)
            {
                hasFoundAllTodaysRides = trip.RequestTime < DateTimeOffset.UtcNow.AddDays(-2);
                var details = await _uberRidersApi.GetTripDetails(trip.Uuid.ToString());

                if (details.Data.Trip.VehicleViewName.Contains(Constants.UberStatuses.UberEats) || trip.Status != Constants.UberStatuses.Completed ||
                    trip.Status == Constants.UberStatuses.Canceled || hasFoundAllTodaysRides)
                {
                    continue;
                }

                var ride = _mapper.Map<Ride>(details.Data);

                await _memetricsApi.SaveRide(ride);
                transactionCount++;
            }

            if (!hasFoundAllTodaysRides)
            {
                return await ProcessUberRides(offset + trips.Data.Trips.TripsTrips.Length, transactionCount);
            }

            return transactionCount;
        }

        public async Task GetAndSaveLyftRides()
        {
            await _lyftApi.Authenticate(_configuration.Value.Lyft_Cookie);
            var transactions = await ProcessLyftRides(0);
            _logger.Information($"{transactions} lyft transactions successfully saved");
        }

        public async Task<int> ProcessLyftRides(int transactions)
        {
            var trips = await _lyftApi.GetTrips();
            if (trips?.Data == null)
            {
                return transactions;
            }

            foreach (var trip in trips?.Data)
            {
                var hasFoundAllTodaysRides = DateTimeOffset.FromUnixTimeMilliseconds(trip.RequestTimestamp).Date < DateTimeOffset.UtcNow.AddDays(-2);
                if (hasFoundAllTodaysRides)
                {
                    return transactions;
                }

                // This api does not provide coordinate data
                var ride = _mapper.Map<Ride>(trip);

                await _memetricsApi.SaveRide(ride);
                transactions++;
            }

            return transactions;
        }
    }
}