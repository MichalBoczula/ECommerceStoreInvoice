using NJsonSchema;

namespace ECommerceStoreInvoice.API.Configuration.Extensions
{
    public static class OpenApiExtension
    {
        public static void FixGuidFormats(this JsonSchema schema)
        {
            if (schema.Type.HasFlag(JsonObjectType.String) &&
                string.Equals(schema.Format, "guid", StringComparison.OrdinalIgnoreCase))
            {
                schema.Format = "uuid";
            }

            foreach (var property in schema.Properties.Values)
            {
                property.FixGuidFormats();
            }

            if (schema.Item is not null)
            {
                schema.Item.FixGuidFormats();
            }

            foreach (var allOf in schema.AllOf)
            {
                allOf.FixGuidFormats();
            }

            foreach (var anyOf in schema.AnyOf)
            {
                anyOf.FixGuidFormats();
            }

            foreach (var oneOf in schema.OneOf)
            {
                oneOf.FixGuidFormats();
            }
        }
    }
}
