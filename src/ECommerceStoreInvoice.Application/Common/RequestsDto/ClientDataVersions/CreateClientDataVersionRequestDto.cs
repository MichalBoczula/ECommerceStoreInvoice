namespace ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions
{
    /// <summary>Creates an immutable client profile version. Every member must be present; names are case-insensitive and unknown members are ignored.</summary>
    public sealed record CreateClientDataVersionRequestDto
    {
        /// <summary>Client display name. The current policy permits null, empty and whitespace values.</summary>
        public required string? ClientName { get; init; }
        /// <summary>Postal code checked by the active address rule, for example 00-001.</summary>
        public required string PostalCode { get; init; }
        /// <summary>City text. Null, empty and whitespace values are accepted by the current policy.</summary>
        public required string? City { get; init; }
        /// <summary>Street text. Null, empty and whitespace values are accepted by the current policy.</summary>
        public required string? Street { get; init; }
        /// <summary>Building identifier text. Null, empty and whitespace values are accepted by the current policy.</summary>
        public required string? BuildingNumber { get; init; }
        /// <summary>Apartment identifier text. The member must be present even when its value is null.</summary>
        public required string? ApartmentNumber { get; init; }
        /// <summary>Phone digits checked by the active phone rule; this is text, preserving leading zeroes.</summary>
        public required string PhoneNumber { get; init; }
        /// <summary>International phone prefix digits stored as text and checked by the active phone rule.</summary>
        public required string PhonePrefix { get; init; }
        /// <summary>Email text checked by the active email rule.</summary>
        public required string AddressEmail { get; init; }
    }
}
