namespace ECommerceStoreInvoice.Application.Common.ResponsesDto
{
    /// <summary>Complete published invoice metadata. This response intentionally preserves the existing clietDataVersionId wire spelling. Incomplete reservations are not published.</summary>
    public sealed class InvoiceResponseDto
    {
        /// <summary>Invoice UUID.</summary>
        public Guid Id { get; init; }
        /// <summary>Paid order UUID associated with the invoice.</summary>
        public Guid OrderId { get; init; }
        /// <summary>Historical client-version UUID referenced by this invoice. The existing clietDataVersionId wire spelling is preserved.</summary>
        public Guid ClietDataVersionId { get; init; }
        /// <summary>Stored location text. Generated storage URIs contain no SAS token; legacy text is not URI-validated by metadata reads. Local file URIs are instance-local; use the PDF download endpoint.</summary>
        public string StorageUrl { get; init; } = string.Empty;
        /// <summary>UTC invoice creation timestamp, retained through reservation retries.</summary>
        public DateTime CreatedAt { get; init; }
    }
}
