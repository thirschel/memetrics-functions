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
        public static Mock<IMeMetricsApi> _memetricsApiMock;
        public static Mock<ILyftApi> _lyftApiMock;
        public static Mock<IUberRidersApi> _uberRidersApiMock;
        public static IMapper _mapper;
        public RideUpdaterTests()
        {
            var configuration = new MapperConfiguration(cfg => {  cfg.AddProfile<RideProfile>(); });
            _loggerMock = new Mock<ILogger>();
            _mapper = new Mapper(configuration);
            _memetricsApiMock = new Mock<IMeMetricsApi>();
            _lyftApiMock = new Mock<ILyftApi>();
            _uberRidersApiMock = new Mock<IUberRidersApi>();
        }

        [Fact]
        public async Task GetAndSaveUberRides_ShouldSaveRidesSuccessfully()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Uber_Cookie = "cookie",
                Uber_User_Id = "1",
            });

            var tripId = Guid.NewGuid();
            _uberRidersApiMock.Setup(x => x.GetTrips(0)).ReturnsAsync(new TripsResponse()
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

            _uberRidersApiMock.Setup(x => x.GetTripDetails(It.IsAny<string>())).ReturnsAsync(new TripsDetailResponse()
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
                        DurationIso = "00:00:00"
                    }
                },

            });

            var updater = new RideUpdater(_loggerMock.Object, config, _lyftApiMock.Object, _uberRidersApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveUberRides();

            _uberRidersApiMock.Verify(x => x.Authenticate(config.Value.Uber_Cookie, config.Value.Uber_User_Id), Times.Once);
            _memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveUberRides_ShouldOnlySaveRide_IfRideIsNotOlderThanTwoDays()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Uber_Cookie = "cookie",
                Uber_User_Id = "1",
            });

            var tripId = Guid.NewGuid();
            _uberRidersApiMock.Setup(x => x.GetTrips(0)).ReturnsAsync(new TripsResponse()
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

            _uberRidersApiMock.Setup(x => x.GetTripDetails(It.IsAny<string>())).ReturnsAsync(new TripsDetailResponse()
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
                        DurationIso = "00:00:00"
                    }
                },

            });

            var updater = new RideUpdater(_loggerMock.Object, config, _lyftApiMock.Object, _uberRidersApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveUberRides();

            _memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Once);
        }

        [Theory]
        [InlineData("CANCELLED")]
        [InlineData("NOT_COMPLETED")]
        public async Task GetAndSaveUberRides_ShouldNotSaveRides_WithBadStatusCodes(string statusCode)
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Uber_Cookie = "cookie",
                Uber_User_Id = "1",
            });

            var tripId = Guid.NewGuid();
            _uberRidersApiMock.Setup(x => x.GetTrips(0)).ReturnsAsync(new TripsResponse()
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

            _uberRidersApiMock.Setup(x => x.GetTripDetails(It.IsAny<string>())).ReturnsAsync(new TripsDetailResponse()
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
                        DurationIso = "00:00:00"
                    }
                },

            });

            var updater = new RideUpdater(_loggerMock.Object, config, _lyftApiMock.Object, _uberRidersApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveUberRides();

            _memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveUberRides_ShouldNotSaveRides_RelatedToUberEats()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Uber_Cookie = "cookie",
                Uber_User_Id = "1",
            });

            var tripId = Guid.NewGuid();
            _uberRidersApiMock.Setup(x => x.GetTrips(0)).ReturnsAsync(new TripsResponse()
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

            _uberRidersApiMock.Setup(x => x.GetTripDetails(It.IsAny<string>())).ReturnsAsync(new TripsDetailResponse()
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
                        DurationIso = "00:00:00"
                    }
                },

            });

            var updater = new RideUpdater(_loggerMock.Object, config, _lyftApiMock.Object, _uberRidersApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveUberRides();

            _memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveLyftRides_ShouldSaveRidesSuccessfully()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            _lyftApiMock.Setup(x => x.GetTrips(0)).ReturnsAsync(new PassengerTrips()
            {
                Data = new Trip[]
                {
                    new Trip()
                    {
                        RequestTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    },
                }
            });

            var updater = new RideUpdater(_loggerMock.Object, config, _lyftApiMock.Object, _uberRidersApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveLyftRides();

            _memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveLyftRides_ShouldOnlySaveRide_IfRideIsNotOlderThanTwoDays()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            _lyftApiMock.Setup(x => x.GetTrips(0)).ReturnsAsync(new PassengerTrips()
            {
                Data = new Trip[]
                {
                    new Trip()
                    {
                        RequestTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                    },
                    new Trip()
                    {
                        RequestTimestamp = DateTimeOffset.Now.AddDays(-3).ToUnixTimeSeconds()
                    },
                }
            });

            var updater = new RideUpdater(_loggerMock.Object, config, _lyftApiMock.Object, _uberRidersApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveLyftRides();

            _memetricsApiMock.Verify(x => x.SaveRide(It.IsAny<Ride>()), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveLyftRides_ShouldReturnSuccessfully_WhenCatchingException()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            _lyftApiMock.Setup(x => x.GetTrips(0)).ThrowsAsync(new Exception());

            var updater = new RideUpdater(_loggerMock.Object, config, _lyftApiMock.Object, _uberRidersApiMock.Object, _memetricsApiMock.Object, _mapper);
            var response  = await updater.GetAndSaveLyftRides();

            Assert.False(response.Successful);
        }

        [Fact]
        public async Task GetAndSaveUberRides_ShouldReturnSuccessfully_WhenCatchingException()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Uber_Cookie = "cookie",
                Uber_User_Id = "1",
            });

            _uberRidersApiMock.Setup(x => x.GetTrips(0)).ThrowsAsync(new Exception());

            var updater = new RideUpdater(_loggerMock.Object, config, _lyftApiMock.Object, _uberRidersApiMock.Object, _memetricsApiMock.Object, _mapper);
            var response = await updater.GetAndSaveUberRides();

            Assert.False(response.Successful);
        }
    }
}
