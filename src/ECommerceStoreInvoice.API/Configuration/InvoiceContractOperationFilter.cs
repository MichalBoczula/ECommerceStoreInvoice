using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerceStoreInvoice.API.Configuration;

/// <summary>Documents existing body-error alternatives, route parameters and binary PDF headers.</summary>
public sealed class InvoiceContractOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            parameter.Description = parameter.Name switch
            {
                "clientId" => "Client UUID used to isolate carts, orders and profile versions; it does not query the Users service.",
                "orderId" => "Order UUID within invoice ownership or order read/status operations.",
                "invoiceId" => "Published invoice UUID; incomplete reservations are not readable.",
                _ => throw new InvalidOperationException("Undocumented invoice route parameter: " + parameter.Name)
            };
            parameter.Example = JsonValue.Create("11111111-1111-4111-8111-111111111111");
        }
        if (operation.RequestBody is OpenApiRequestBody body)
        {
            body.Description = "Required JSON object. Required members must be present even when nullable; member names are case-insensitive and unknown root/nested members are ignored. Schema numbers show canonical JSON values; web binding also accepts quoted int32 text for cart quantities. Business validation remains separate from binding.";
            if (operation.Responses?["400"] is OpenApiResponse response)
            {
                var media = response.Content!["application/problem+json"];
                media.Schema = new OpenApiSchema { OneOf = [media.Schema!, context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository)] };
                response.Description = "Business validation returns its complete seven-field problem; malformed, missing or unbindable bodies return the complete six-field safe problem.";
            }
        }
        if (operation.OperationId != "DownloadInvoicePdf") return;
        var pdf = (OpenApiResponse)operation.Responses!["200"];
        pdf.Description = "Raw completed invoice PDF bytes, not a JSON string or base64 representation. A cancelled transfer has no fabricated completed HTTP outcome.";
        pdf.Content!["application/pdf"].Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary", Description = "Raw PDF byte stream beginning with the PDF signature." };
        pdf.Headers ??= new Dictionary<string, IOpenApiHeader>();
        pdf.Headers["Content-Disposition"] = Header("Attachment with filename invoice-{invoiceId:D}.pdf; filename* carries the same name.", "attachment; filename=invoice-11111111-1111-4111-8111-111111111111.pdf; filename*=UTF-8''invoice-11111111-1111-4111-8111-111111111111.pdf");
        pdf.Headers["Cache-Control"] = Header("Both private and no-store cache directives are present; directive order is not significant.", "private, no-store");
    }

    private static OpenApiHeader Header(string description, string example) => new()
    {
        Description = description,
        Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        Example = JsonValue.Create(example)
    };
}
