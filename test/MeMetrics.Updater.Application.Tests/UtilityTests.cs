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
        public void FormatStringToPhoneNumber_Formats_String_Correctly(string str, string expected)
        {
            Assert.Equal(expected, Utility.FormatStringToPhoneNumber(str));
        }
    }
}
