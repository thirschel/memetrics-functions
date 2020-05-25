using System;
using System.IO;
using Bogus;
using MeMetrics.Updater.Application.Helpers;
using MeMetrics.Updater.Application.Tests.Helpers;
using Xunit;

namespace MeMetrics.Updater.Application.Tests
{
    public class UtilityTests
    {
        [Theory]
        [InlineData("13128675309", "13128675309")]
        [InlineData("1(312)8675309", "13128675309")]
        [InlineData("1(312)867-5309", "13128675309")]
        [InlineData("+1(312)867-5309", "13128675309")]
        [InlineData("+1 (312) 867 5309", "13128675309")]
        [InlineData("+1 (312) 867-5309", "13128675309")]
        [InlineData("+1 312 867 5309", "13128675309")]
        public void FormatStringToPhoneNumber_FormatsStringCorrectly(string str, string expected)
        {
            // ACT
            var formattedStr = Utility.FormatStringToPhoneNumber(str);

            // ASSERT
            Assert.Equal(expected, formattedStr);
        }

        [Fact]
        public void Decode_ShouldDecodeBase64StringCorrectly()
        {
            // ARRANGE
            var str = new Faker().Random.String2(100);
            var strBytes = System.Text.Encoding.UTF8.GetBytes(str);

            // ACT
            var decodedStr = Utility.Decode(Convert.ToBase64String(strBytes));

            // ASSERT
            Assert.Equal(str, decodedStr);
        }

        [Fact]
        public void Decode_ShouldReturnEmptyString_IfInputIsEmpty()
        {
            // ARRANGE
            var str = string.Empty;

            // ACT
            var decodedStr = Utility.Decode(str);

            // ASSERT
            Assert.Equal(str, decodedStr);
        }

        [Fact]
        public void AddPadding_ShouldAddPadding_IfInputIsASingleCharacterNumber()
        {
            // ARRANGE
            var number = 1;

            // ACT
            var numberString = Utility.AddPadding(number);

            // ASSERT
            Assert.Equal("01", numberString);
        }

        [Fact]
        public void AddPadding_ShouldNotAddPadding_IfInputIsAMultipleCharacterNumber()
        {
            // ARRANGE
            var number = 10;

            // ACT
            var numberString = Utility.AddPadding(number);

            // ASSERT
            Assert.Equal("10", numberString);
        }

        [Fact]
        public void GetCoordinatesFromGoogleMapsUrl_GetsCoordinatesCorrectly()
        {
            // ARRANGE
            var faker = new Faker();
            var originLong = faker.Address.Longitude().ToString();
            var destinationLong = faker.Address.Longitude().ToString();
            var originLat = faker.Address.Latitude().ToString();
            var destinationLat = faker.Address.Latitude().ToString();
            var url = TestHelpers.GenerateGoogleMapsUrl(originLat, originLong, destinationLat, destinationLong);

            // ACT
            var coords = Utility.GetCoordinatesFromGoogleMapsUrl(url);

            // ASSERT
            Assert.Equal(originLat, coords.OriginLat);
            Assert.Equal(originLong, coords.OriginLong);
            Assert.Equal(destinationLat, coords.DestinationLat);
            Assert.Equal(destinationLong, coords.DestinationLong);
        }
    }
}
