namespace MeMetrics.Updater.Application.Tests.Helpers
{
    public class TestHelpers
    {
        public static string GenerateGoogleMapsUrl(string originLat, string originLong, string destLat, string destLong)
        {
            return $"https://maps.googleapis.com/maps/api/staticmap?size=580x267&markers=%7Cicon%3Ahttps%3A%2F%2Fd1a3f4spazzrp4.cloudfront.net%2Fmaps%2Fhelix%2Fpickup.png%7Cscale%3A2%7C{originLat}%2C{originLong}&markers=%7Cicon%3Ahttps%3A%2F%2Fd1a3f4spazzrp4.cloudfront.net%2Fmaps%2Fhelix%2Fdropoff.png%7Cscale%3A2%7C{destLat}%2C{destLong}&path=color%3A0x2DBAE4%7Cweight%3A4%7Cenc%3Asbn%7EFb%60yuOtVKI%7BVf%40mCnO_AdHgFN%5CzAff%40fD%7ESLvFmDbb%40%7CA%7EKIhUsBnMqDxFuFlCgKhB%7BGUsJsEgMF%7D%7BA%7CDiZzCuk%40VcJx%40kJtD_I%7EGmc%40zs%40yFzGkGzDuM%7CEwLvAkEBWk%40e%40clA%7D%7D%40v%40%5DmM&style=feature%3Alandscape%7Cvisibility%3Aoff&style=feature%3Apoi%7Cvisibility%3Aoff&style=feature%3Atransit%7Cvisibility%3Aoff&style=feature%3Aroad.highway%7Celement%3Ageometry%7Clightness%3A39.0&style=feature%3Aroad.local%7Celement%3Ageometry&style=feature%3Aroad%7Celement%3Alabels&style=feature%3Aadministrative%7Cvisibility%3Aoff&style=feature%3Aadministrative.locality%7Cvisibility%3Aon&style=feature%3Alandscape.natural%7Cvisibility%3Aon&style=feature%3Aadministrative%7Cvisibility%3Aoff&client=gme-ubertechnologies1&signature=zmudB8wHpwaah66LawW0WeYOvDo=";
        }
    }
}