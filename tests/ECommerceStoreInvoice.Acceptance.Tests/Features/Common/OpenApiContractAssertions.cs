using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

/// <summary>Compares generated metadata with CLR/serializer contracts, not another OpenAPI document.</summary>
internal static class OpenApiContractAssertions
{
    internal static object Check(JsonDocument document)
    {
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        var nullability = new NullabilityInfoContext();
        var types = new[] { typeof(ApiProblemDetails).Assembly, typeof(CreateClientDataVersionRequestDto).Assembly, typeof(ValidationError).Assembly }
            .SelectMany(assembly => assembly.GetTypes()).Append(typeof(ProblemDetails)).ToArray();
        var checkedProperties = new List<object>();
        foreach (var entry in schemas.EnumerateObject())
        {
            var type = types.Single(type => type.Name == entry.Name);
            var request = type.Namespace?.Contains(".RequestsDto", StringComparison.Ordinal) == true;
            var problem = typeof(ProblemDetails).IsAssignableFrom(type);
            Text(entry.Value, "description", entry.Name);
            var metadata = options.GetTypeInfo(type).Properties.Where(property => !property.IsExtensionData).ToArray();
            var properties = entry.Value.GetProperty("properties");
            var names = metadata.Select(property => property.Name).Concat(type == typeof(ProblemDetails) ? new[] { "traceId" } : []).Order().ToArray();
            Require(properties.EnumerateObject().Select(property => property.Name).Order().SequenceEqual(names), "Serializer member drift: " + entry.Name);
            var expectedRequired = request ? metadata.Where(property => property.IsRequired).Select(property => property.Name).Order() : names.AsEnumerable();
            var required = entry.Value.TryGetProperty("required", out var fields) ? fields.EnumerateArray().Select(field => field.GetString()!).Order() : Enumerable.Empty<string>();
            Require(required.SequenceEqual(expectedRequired), "Required member drift: " + entry.Name);
            foreach (var property in metadata)
            {
                var rule = properties.GetProperty(property.Name);
                var member = (PropertyInfo)property.AttributeProvider!;
                var info = nullability.Create(member);
                var nullable = !problem && IsNullable(property.PropertyType, info);
                Require(Nullable(rule) == nullable, $"Nullability drift: {entry.Name}.{property.Name}");
                CheckType(rule, property.PropertyType, $"{entry.Name}.{property.Name}", problem ? null : info);
            }
            foreach (var property in properties.EnumerateObject())
            {
                Text(property.Value, "description", entry.Name + "." + property.Name);
                Require(property.Value.TryGetProperty("example", out var example), $"Missing typed example: {entry.Name}.{property.Name}");
                Valid(property.Value, example, entry.Name + "." + property.Name);
                checkedProperties.Add(new { schema = entry.Name, field = property.Name, required = required.Contains(property.Name), nullable = Nullable(property.Value), example = example.Clone() });
            }
            Require(entry.Value.TryGetProperty("example", out var rootExample), "Missing complete typed example: " + entry.Name);
            Valid(entry.Value, rootExample, entry.Name);
            var closed = entry.Value.TryGetProperty("additionalProperties", out var extra) && extra.ValueKind == JsonValueKind.False;
            Require(request ? !closed : closed, "Unknown-member contract drift: " + entry.Name);
        }
        var parameterCount = 0;
        var operations = document.RootElement.GetProperty("paths").EnumerateObject().SelectMany(path => path.Value.EnumerateObject()).Select(method => method.Value).ToArray();
        Require(operations.Length == Outputs.Count, "Operation inventory drift");
        foreach (var operation in operations)
        {
            var name = operation.GetProperty("operationId").GetString()!;
            Text(operation, "summary", name); Text(operation, "description", name);
            if (operation.TryGetProperty("parameters", out var parameters))
                foreach (var parameter in parameters.EnumerateArray())
                {
                    Require(parameter.GetProperty("in").GetString() == "path" && parameter.GetProperty("required").GetBoolean(), "Route parameter drift: " + name);
                    CheckType(parameter.GetProperty("schema"), typeof(Guid), name);
                    Text(parameter, "description", name);
                    Require(parameter.TryGetProperty("example", out var example), "Missing route example: " + name);
                    Valid(parameter.GetProperty("schema"), example, name);
                    parameterCount++;
                }
            var hasBody = Inputs.TryGetValue(name, out var input);
            Require(operation.TryGetProperty("requestBody", out var body) == hasBody, "Request body drift: " + name);
            if (hasBody)
            {
                Require(body.GetProperty("required").GetBoolean(), "Request body must be required: " + name);
                Text(body, "description", name);
                Require(body.GetProperty("content").EnumerateObject().Select(item => item.Name).SequenceEqual(new[] { "application/json" }), "Request media drift: " + name);
                CheckType(body.GetProperty("content").GetProperty("application/json").GetProperty("schema"), input!, name);
            }
            foreach (var response in operation.GetProperty("responses").EnumerateObject())
            {
                var status = int.Parse(response.Name, System.Globalization.CultureInfo.InvariantCulture);
                var media = status >= 400 ? "application/problem+json" : name == "DownloadInvoicePdf" ? "application/pdf" : "application/json";
                var content = response.Value.GetProperty("content");
                Require(content.EnumerateObject().Select(item => item.Name).SequenceEqual(new[] { media }), "Response media drift: " + name);
                var rule = content.GetProperty(media).GetProperty("schema");
                if (status == 400 && hasBody)
                {
                    var alternatives = rule.GetProperty("oneOf").EnumerateArray().ToArray();
                    Require(alternatives.Length == 2, "Body problem alternatives drift: " + name);
                    CheckType(alternatives[0], typeof(ApiProblemDetails), name);
                    CheckType(alternatives[1], typeof(ProblemDetails), name);
                }
                else if (status >= 400)
                    CheckType(rule, status switch { 400 => typeof(ApiProblemDetails), 404 => typeof(NotFoundProblemDetails), 409 => typeof(ConflictProblemDetails), _ => typeof(ProblemDetails) }, name);
                else if (name != "DownloadInvoicePdf") CheckType(rule, Outputs[name], name);
                else
                {
                    Require(rule.GetProperty("type").GetString() == "string" && rule.GetProperty("format").GetString() == "binary", "PDF must be binary");
                    Text(rule, "description", name);
                    var headers = response.Value.GetProperty("headers");
                    foreach (var field in new[] { "Content-Disposition", "Cache-Control" })
                    {
                        var header = headers.GetProperty(field); Text(header, "description", field);
                        CheckType(header.GetProperty("schema"), typeof(string), field);
                        Valid(header.GetProperty("schema"), header.GetProperty("example"), field);
                    }
                    Require(headers.GetProperty("Content-Disposition").GetProperty("example").GetString()!.Contains("invoice-11111111-1111-4111-8111-111111111111.pdf", StringComparison.Ordinal), "PDF filename example drift");
                    Require(headers.GetProperty("Cache-Control").GetProperty("example").GetString() == "private, no-store", "PDF cache example drift");
                }
            }
        }
        return new { parameters = parameterCount, schemas = schemas.EnumerateObject().Count(), properties = checkedProperties.Count, checkedProperties, completeSerializerComparison = true, bodyProblemAlternatives = Inputs.Count, binaryPdfAndHeaders = true };

        void Valid(JsonElement schema, JsonElement value, string path)
        {
            var errors = JsonContractValidator.Check(schema, value, schemas);
            Require(errors.Count == 0, "Invalid typed example at " + path + ": " + string.Join("; ", errors));
        }
        void CheckType(JsonElement rule, Type type, string path, NullabilityInfo? info = null)
        {
            if (info is not null) Require(Nullable(rule) == IsNullable(type, info), "Nested nullability drift: " + path);
            type = System.Nullable.GetUnderlyingType(type) ?? type;
            if (rule.TryGetProperty("allOf", out var all))
            {
                Require(all.GetArrayLength() == 1, "Unexpected composition at " + path);
                CheckType(all[0], type, path); return;
            }
            if (rule.TryGetProperty("$ref", out var reference))
            {
                Require(reference.GetString() == "#/components/schemas/" + type.Name, "Reference drift: " + path); return;
            }
            var dictionary = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
            var expected = type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) ? "string"
                : type == typeof(bool) ? "boolean" : type == typeof(int) ? "integer" : type == typeof(decimal) ? "number"
                : dictionary ? "object" : typeof(IEnumerable).IsAssignableFrom(type) ? "array" : "object";
            Require(rule.GetProperty("type").GetString() == expected, "Type drift: " + path);
            var format = type == typeof(Guid) ? "uuid" : type == typeof(DateTime) ? "date-time" : type == typeof(int) ? "int32" : type == typeof(decimal) ? "double" : null;
            if (format is not null) Require(rule.GetProperty("format").GetString() == format, "Format drift: " + path);
            if (expected == "array") CheckType(rule.GetProperty("items"), type.GenericTypeArguments.Single(), path + "[]", info?.GenericTypeArguments.Single());
            if (dictionary) CheckType(rule.GetProperty("additionalProperties"), type.GenericTypeArguments[1], path + ".*", info?.GenericTypeArguments[1]);
        }
    }

    private static bool Nullable(JsonElement schema) => schema.TryGetProperty("nullable", out var value) && value.GetBoolean();
    private static bool IsNullable(Type type, NullabilityInfo info) => System.Nullable.GetUnderlyingType(type) is not null || info.ReadState == NullabilityState.Nullable;
    private static void Text(JsonElement owner, string name, string path) => Require(owner.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()), "Missing " + name + ": " + path);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static readonly Dictionary<string, Type> Inputs = new()
    {
        ["CreateClientDataVersion"] = typeof(CreateClientDataVersionRequestDto),
        ["UpdateShoppingCart"] = typeof(UpdateShoppingCartRequestDto),
        ["UpdateOrderStatus"] = typeof(UpdateOrderStatusRequestDto)
    };
    private static readonly Dictionary<string, Type> Outputs = new()
    {
        ["GetClientDataVersionByClientId"] = typeof(ClientDataVersionResponseDto),
        ["CreateClientDataVersion"] = typeof(ClientDataVersionResponseDto),
        ["CreateShoppingCart"] = typeof(ShoppingCartResponseDto),
        ["UpdateShoppingCart"] = typeof(ShoppingCartResponseDto),
        ["GetShoppingCartByClientId"] = typeof(ShoppingCartResponseDto),
        ["CreateOrder"] = typeof(OrderResponseDto),
        ["GetOrderById"] = typeof(OrderResponseDto),
        ["UpdateOrderStatus"] = typeof(OrderResponseDto),
        ["GetOrdersByClientId"] = typeof(IReadOnlyCollection<OrderResponseDto>),
        ["CreateInvoiceForOrder"] = typeof(InvoiceResponseDto),
        ["GetInvoiceById"] = typeof(InvoiceResponseDto),
        ["GetInvoiceByOrderId"] = typeof(InvoiceResponseDto),
        ["DownloadInvoicePdf"] = typeof(Stream),
        ["GetFlowDocumentation"] = typeof(FlowDescriptorsResponseDto),
        ["GetValidationDocumentation"] = typeof(ValidationDescriptorsResponseDto)
    };
}
