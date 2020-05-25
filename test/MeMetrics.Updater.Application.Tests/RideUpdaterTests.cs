using System;
using System.Threading.Tasks;
using AutoMapper;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.Lyft;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using MeMetrics.Updater.Application.Objects.Uber;
using MeMetrics.Updater.Application.Profiles;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using Uber.API.Objects;
using Xunit;
using Trip = MeMetrics.Updater.Application.Objects.Lyft.Trip;

namespace MeMetrics.Updater.Application.Tests
{
    public class RideUpdaterTests
    {
        public static Mock<ILogger> _loggerMock;
        public static IMapper _mapper;
        public RideUpdaterTests()
        {
            var configuration = new MapperConfiguration(cfg => {  cfg.AddProfile<RideProfile>(); });
            _loggerMock = new Mock<ILogger>();
            _mapper = new Mapper(configuration);
        }

        [Fact]
        public async Task GetAndSaveUberRides_ShouldSaveRidesSuccessfully()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var lyftApiMock = new Mock<ILyftApi>();
            var uberRidersApiMock = new Mock<IUberRidersApi>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Uber_Cookie = "cookie",
                Uber_User_Id = "1",
            });

            var tripId = Guid.NewGuid();
            uberRidersApiMock.Setup(x => x.GetTrips(0)).ReturnsAsync(new TripsResponse()
            {
                Data = new TripsResponseData()
                {
                    Trips = new Trips()
                    {
                        TripsTrips = new[]
                        {
                            new Uber.API.Objects.Trip()
                            {
                                Uuid = tripId,
                                RequestTime = DateTimeOffset.Now,
                                Status = "COMPLETED",
                            } 
                        }
                    }
                }
            });

            uberRidersApiMock.Setup(x => x.GetTripDetails(It.IsAny<string>())).ReturnsAsync(new TripsDetailResponse()
            {
                Data = new TripsDetail()
                {
                    Trip = new Uber.API.Objects.Trip()
                    {
                        VehicleViewName = string.Empty,
                        DropoffTime = DateTimeOffset.Now
                    },
                    Receipt = new Receipt()
                    {
                        Duration = "(00:00:00)"
                    }
                },

            });

            var updater = new RideUpdater(_loggerMock.Object, config, lyftApiMock.Object, uberRidersApiMock.Object, memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveUberRides();

            uberRidersApiMock.Verify(x => x.Authenticate(config.Value.Uber_Cookie, config.Value.Uber_User_Id), Times.Once);
            memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveUberRides_ShouldOnlySaveRide_IfRideIsNotOlderThanTwoDays()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var lyftApiMock = new Mock<ILyftApi>();
            var uberRidersApiMock = new Mock<IUberRidersApi>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Uber_Cookie = "cookie",
                Uber_User_Id = "1",
            });

            var tripId = Guid.NewGuid();
            uberRidersApiMock.Setup(x => x.GetTrips(0)).ReturnsAsync(new TripsResponse()
            {
                Data = new TripsResponseData()
                {
                    Trips = new Trips()
                    {
                        TripsTrips = new[]
                        {
                            new Uber.API.Objects.Trip()
                            {
                                Uuid = tripId,
                                RequestTime = DateTimeOffset.Now,
                                Status = Constants.UberStatuses.Completed,
                            },
                            new Uber.API.Objects.Trip()
                            {
                                Uuid = tripId,
                                RequestTime = DateTimeOffset.Now.AddDays(-3),
                                Status = Constants.UberStatuses.Completed,
                            }
                        }
                    }
                }
            });

            uberRidersApiMock.Setup(x => x.GetTripDetails(It.IsAny<string>())).ReturnsAsync(new TripsDetailResponse()
            {
                Data = new TripsDetail()
                {
                    Trip = new Uber.API.Objects.Trip()
                    {
                        VehicleViewName = string.Empty,
                        DropoffTime = DateTimeOffset.Now
                    },
                    Receipt = new Receipt()
                    {
                        Duration = "(00:00:00)"
                    }
                },

            });

            var updater = new RideUpdater(_loggerMock.Object, config, lyftApiMock.Object, uberRidersApiMock.Object, memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveUberRides();

            memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Once);
        }

        [Theory]
        [InlineData("CANCELLED")]
        [InlineData("NOT_COMPLETED")]
        public async Task GetAndSaveUberRides_ShouldNotSaveRides_WithBadStatusCodes(string statusCode)
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var lyftApiMock = new Mock<ILyftApi>();
            var uberRidersApiMock = new Mock<IUberRidersApi>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Uber_Cookie = "cookie",
                Uber_User_Id = "1",
            });

            var tripId = Guid.NewGuid();
            uberRidersApiMock.Setup(x => x.GetTrips(0)).ReturnsAsync(new TripsResponse()
            {
                Data = new TripsResponseData()
                {
                    Trips = new Trips()
                    {
                        TripsTrips = new[]
                        {
                            new Uber.API.Objects.Trip()
                            {
                                Uuid = tripId,
                                RequestTime = DateTimeOffset.Now,
                                Status = statusCode,
                            },
                        }
                    }
                }
            });

            uberRidersApiMock.Setup(x => x.GetTripDetails(It.IsAny<string>())).ReturnsAsync(new TripsDetailResponse()
            {
                Data = new TripsDetail()
                {
                    Trip = new Uber.API.Objects.Trip()
                    {
                        VehicleViewName = string.Empty,
                        DropoffTime = DateTimeOffset.Now
                    },
                    Receipt = new Receipt()
                    {
                        Duration = "(00:00:00)"
                    }
                },

            });

            var updater = new RideUpdater(_loggerMock.Object, config, lyftApiMock.Object, uberRidersApiMock.Object, memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveUberRides();

            memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveUberRides_ShouldNotSaveRides_RelatedToUberEats()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var lyftApiMock = new Mock<ILyftApi>();
            var uberRidersApiMock = new Mock<IUberRidersApi>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Uber_Cookie = "cookie",
                Uber_User_Id = "1",
            });

            var tripId = Guid.NewGuid();
            uberRidersApiMock.Setup(x => x.GetTrips(0)).ReturnsAsync(new TripsResponse()
            {
                Data = new TripsResponseData()
                {
                    Trips = new Trips()
                    {
                        TripsTrips = new[]
                        {
                            new Uber.API.Objects.Trip()
                            {
                                Uuid = tripId,
                                RequestTime = DateTimeOffset.Now,
                                Status = Constants.UberStatuses.Completed,
                            },
                        }
                    }
                }
            });

            uberRidersApiMock.Setup(x => x.GetTripDetails(It.IsAny<string>())).ReturnsAsync(new TripsDetailResponse()
            {
                Data = new TripsDetail()
                {
                    Trip = new Uber.API.Objects.Trip()
                    {
                        VehicleViewName = Constants.UberStatuses.UberEats,
                        DropoffTime = DateTimeOffset.Now
                    },
                    Receipt = new Receipt()
                    {
                        Duration = "(00:00:00)"
                    }
                },

            });

            var updater = new RideUpdater(_loggerMock.Object, config, lyftApiMock.Object, uberRidersApiMock.Object, memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveUberRides();

            memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveLyftRides_ShouldSaveRidesSuccessfully()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var lyftApiMock = new Mock<ILyftApi>();
            var uberRidersApiMock = new Mock<IUberRidersApi>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            lyftApiMock.Setup(x => x.GetTrips(It.IsAny<int>())).ReturnsAsync(new PassengerTrips()
            {
                Data = new Trip[]
                {
                    new Trip()
                    {
                        RequestTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                    },
                }
            });

            var updater = new RideUpdater(_loggerMock.Object, config, lyftApiMock.Object, uberRidersApiMock.Object, memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveLyftRides();

            memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveLyftRides_ShouldOnlySaveRide_IfRideIsNotOlderThanTwoDays()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var lyftApiMock = new Mock<ILyftApi>();
            var uberRidersApiMock = new Mock<IUberRidersApi>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            lyftApiMock.Setup(x => x.GetTrips(It.IsAny<int>())).ReturnsAsync(new PassengerTrips()
            {
                Data = new Trip[]
                {
                    new Trip()
                    {
                        RequestTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                    },
                    new Trip()
                    {
                        RequestTimestamp = DateTimeOffset.Now.AddDays(-3).ToUnixTimeMilliseconds()
                    },
                }
            });

            var updater = new RideUpdater(_loggerMock.Object, config, lyftApiMock.Object, uberRidersApiMock.Object, memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveLyftRides();

            memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Once);
        }
    }
}
