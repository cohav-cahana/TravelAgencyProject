using System.Text.Json;

namespace TravelAgencyProject.Extensions
{
    public static class SessionExtensions
    {
        // Method to save a complex object into the Session
        public static void SetComplexObject(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        // Method to retrieve a complex object from the Session
        public static T? GetComplexObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}