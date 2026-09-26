# ADR 0001: MongoDB documents and product snapshots

Status: Accepted (2026-09-26)

## Context

Orders must retain the product information used at checkout even if ProductsCatalog changes later. Client contact data can have multiple versions, while one cart per client and one invoice per order need database-level enforcement.

## Decision

Infrastructure maps Domain aggregates to separate MongoDB collections for shopping carts, orders, product versions, invoices and client data versions. Checkout fetches products by ID from ProductsCatalog and saves new product version documents; order lines refer to those saved version IDs. Later order reads join saved versions, and invoice generation reads the same snapshots. Client data versions are appended; reading by client returns the latest `CreatedAt` entry.

Startup creates a unique cart `ClientId` index, a unique invoice `OrderId` index, and a `(ClientId asc, CreatedAt desc)` client-data-version index. The repository creates named indexes during startup and fails if MongoDB initialization fails. MongoDB must support sessions and transactions; readiness checks a writable replica-set primary and a database ping.

## Consequences

ProductsCatalog availability affects checkout, while order reads and invoice creation use persisted snapshots. The unique indexes prevent duplicate carts/invoices even under concurrent requests; application checks alone do not enforce uniqueness. This repository does not keep a separate order-history collection: `Order` holds the current status and timestamps, and client data versions are historical records in their own collection. Changing product snapshot fields or MongoDB document mappings requires migration and compatibility review.

See `src/ECommerceStoreInvoice.Infrastructure/Configuration/MongoInitializer.cs`, `Repositories/OrderRepository.cs`, `Repositories/ClientDataVersionRepository.cs` and the corresponding Infrastructure integration tests.
