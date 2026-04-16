using Allure.Net.Commons;
using System.Text;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common
{
    internal static class AllureJson
    {
        public static void AttachObject(string title, object obj, JsonSerializerOptions baseOptions)
        {
            var prettyOptions = new JsonSerializerOptions(baseOptions)
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(obj, prettyOptions);
            AllureApi.AddAttachment(title, "application/json", Encoding.UTF8.GetBytes(json), ".json");
        }

        public static void AttachRawJson(string title, string json)
        {
            var pretty = TryPretty(json);
            AllureApi.AddAttachment(title, "application/json", Encoding.UTF8.GetBytes(pretty), ".json");
        }

        private static string TryPretty(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return json;
            }
        }
    }
}
