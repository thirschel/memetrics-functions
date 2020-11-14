using System;
using AutoMapper;
using Bogus;
using MeMetrics.Updater.Application.Objects.Enums;
using MeMetrics.Updater.Application.Objects.Lyft;
using MeMetrics.Updater.Application.Objects.Uber;
using MeMetrics.Updater.Application.Profiles;
using MeMetrics.Updater.Application.Tests.Helpers;
using Xunit;

namespace MeMetrics.Updater.Application.Tests.Profiles
{
    public class RideProfileTests
    {
        [Fact]
        public void UberRide_ShouldMapTo_Ride()
        {
            // ARRANGE
            var faker = new Faker();
            var duration = faker.Date.Timespan(new TimeSpan(0, 23, 59, 59));
            var originLong = faker.Address.Longitude().ToString();
            var destinationLong = faker.Address.Longitude().ToString();
            var originLat = faker.Address.Latitude().ToString();
            var destinationLat = faker.Address.Latitude().ToString();
            var recieptFaker = new Faker<Receipt>()
                .RuleFor(f => f.Distance, f => f.Random.Decimal().ToString())
                .RuleFor(f => f.DurationIso, f => $"{duration:hh\\:mm\\:ss}");

            var tripFaker = new Faker<Uber.API.Objects.Trip>()
                .RuleFor(f => f.Uuid, f => f.Random.Guid())
                .RuleFor(f => f.RequestTime, f => f.Date.PastOffset())
                .RuleFor(f => f.DropoffTime, f => f.Date.SoonOffset())
                .RuleFor(f => f.ClientFare, f => f.Random.Double());
            var trip = tripFaker.Generate();
            var reciept = recieptFaker.Generate();

            var uberRideFaker = new Faker<TripsDetail>()
                .RuleFor(f => f.Trip, f => trip)
                .RuleFor(f => f.Receipt, f => reciept)
                .RuleFor(f => f.TripMap, f =>
                {
                    return new TripMap()
                    {
                        Url = new Uri(TestHelpers.GenerateGoogleMapsUrl(originLat, originLong, destinationLat, destinationLong))
                    };
                });

            var uberRide = uberRideFaker.Generate();
            var pickup = uberRide.Trip.DropoffTime.Value.Subtract(new TimeSpan(duration.Days, duration.Hours, duration.Minutes, duration.Seconds));
            // Remove milliseconds without having an issue with ticks
            pickup = new DateTimeOffset(pickup.Year, pickup.Month, pickup.Day, pickup.Hour, pickup.Minute, pickup.Second, pickup.Offset);
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<RideProfile>(); });
            var mapper = new Mapper(configuration);

            // ACT
            var ride = mapper.Map<Objects.MeMetrics.Ride>(uberRide);
            
            // ASSERT
            Assert.Equal(uberRide.Trip.Uuid.ToString(), ride.RideId);
            Assert.Equal(RideType.Uber, ride.RideType);
            Assert.Equal(decimal.Parse(uberRide.Receipt.Distance), ride.Distance);
            Assert.Equal(uberRide.Trip.RequestTime, ride.RequestDate);
            Assert.Equal(uberRide.Trip.DropoffTime, ride.DropoffDate);
            Assert.Equal((decimal) uberRide.Trip.ClientFare, ride.Price);
            Assert.Equal(originLat, ride.OriginLat);
            Assert.Equal(originLong, ride.OriginLong);
            Assert.Equal(destinationLat, ride.DestinationLat);
            Assert.Equal(destinationLong, ride.DestinationLong);
            Assert.Equal(pickup, ride.PickupDate);

        }

        [Fact]
        public void LyftTrip_ShouldMapTo_Ride()
        {
            // ARRANGE
            var tripFaker = new Faker<Trip>()
                .RuleFor(f => f.RideId, f => f.Random.String2(32))
                .RuleFor(f => f.Distance, f => f.Random.Long(0))
                .RuleFor(f => f.RequestTimestamp, f => f.Date.PastOffset().ToUnixTimeSeconds())
                .RuleFor(f => f.DropoffTimestamp, f => f.Date.PastOffset().ToUnixTimeSeconds())
                .RuleFor(f => f.PickupTimestamp, f => f.Date.PastOffset().ToUnixTimeSeconds())
                .RuleFor(f => f.TotalMoney, f =>
                {
                    return new TotalMoney()
                    {
                        Amount = f.Random.Long(0)
                    };
                });

            var trip = tripFaker.Generate();
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<RideProfile>(); });
            var mapper = new Mapper(configuration);

            // ACT
            var ride = mapper.Map<Objects.MeMetrics.Ride>(trip);

            // ASSERT
            Assert.Equal(trip.RideId, ride.RideId);
            Assert.Equal(RideType.Lyft, ride.RideType);
            Assert.Equal((decimal?) Math.Round(trip.Distance * 0.00062137, 2), ride.Distance);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(trip.RequestTimestamp), ride.RequestDate);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(trip.DropoffTimestamp), ride.DropoffDate);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(trip.PickupTimestamp), ride.PickupDate);
            Assert.Equal((decimal)trip.TotalMoney.Amount / 100, ride.Price);
        }
    }
}