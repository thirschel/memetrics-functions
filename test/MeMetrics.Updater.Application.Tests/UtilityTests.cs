using System.IO;
using MeMetrics.Updater.Application.Helpers;
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
            Assert.Equal(expected, Utility.FormatStringToPhoneNumber(str));
        }

        [Fact]
        public void Decode_ShouldDecodeBase64StringCorrectly()
        {
            Assert.Equal("Test", Utility.Decode("VGVzdA=="));
        }

        [Fact]
        public void Decode_ShouldReturnEmptyString_IfInputIsEmpty()
        {
            Assert.Equal(string.Empty, Utility.Decode(string.Empty));
        }

        [Fact]
        public void AddPadding_ShouldAddPadding_IfInputIsASingleCharacterNumber()
        {
            Assert.Equal("01", Utility.AddPadding(1));
        }

        [Fact]
        public void AddPadding_ShouldNotAddPadding_IfInputIsAMultipleCharacterNumber()
        {
            Assert.Equal("10", Utility.AddPadding(10));
        }
    }
}
