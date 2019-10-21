using Newtonsoft.Json.Linq;

namespace E.CON.TROL.CHECK.DEMO
{
    static class Extensions
    {
        public static string GetPropertyValueFromJObject(this JObject json, string propertyName)
        {
            JToken token = null;
            if (json?.TryGetValue(propertyName, out token) == true)
            {
                return token?.ToString();
            }
            else
            {
                return null;
            }
        }
    }
}
