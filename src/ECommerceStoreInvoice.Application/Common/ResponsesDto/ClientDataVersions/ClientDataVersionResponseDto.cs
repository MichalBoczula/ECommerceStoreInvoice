namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions
{
    /// <summary>Complete immutable client profile version. Nullable members are emitted explicitly; reads choose the latest version by creation time.</summary>
    public sealed record ClientDataVersionResponseDto
    {
        /// <summary>Immutable profile-version UUID.</summary>
        public required Guid Id { get; init; }
        /// <summary>Client UUID owning this version.</summary>
        public required Guid ClientId { get; init; }
        /// <summary>Stored client display name; may be null.</summary>
        public required string? ClientName { get; init; }
        /// <summary>Stored postal code text.</summary>
        public required string PostalCode { get; init; }
        /// <summary>Stored city text; may be null.</summary>
        public required string? City { get; init; }
        /// <summary>Stored street text; may be null.</summary>
        public required string? Street { get; init; }
        /// <summary>Stored building identifier text; may be null.</summary>
        public required string? BuildingNumber { get; init; }
        /// <summary>Stored apartment identifier text; may be null.</summary>
        public required string? ApartmentNumber { get; init; }
        /// <summary>Stored phone digits as text.</summary>
        public required string PhoneNumber { get; init; }
        /// <summary>Stored phone prefix digits as text.</summary>
        public required string PhonePrefix { get; init; }
        /// <summary>Stored email address text.</summary>
        public required string AddressEmail { get; init; }
        /// <summary>UTC timestamp when this version was created.</summary>
        public required DateTime CreatedAt { get; init; }
    }
}
