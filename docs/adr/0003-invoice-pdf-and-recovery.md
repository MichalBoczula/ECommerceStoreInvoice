# ADR-0003: Invoice claim, PDF generation and recovery

- Status: Accepted; Azure Blob support added 2026-10-25
- Date: 2026-09-26

## Context

Two requests for one paid order can race, PDF generation can fail, and the database may commit completion while its acknowledgment is lost. An invoice must not be exposed before the PDF is ready.

## Decision

Generation reserves an invoice document keyed by unique `OrderId`. The reservation has a `Generating` state, attempt ID and ten-minute lease. A `Failed` reservation or expired `Generating` lease may be claimed for a new attempt; an active claim and a completed invoice cannot. Only the current attempt may mark the document `Completed` or release it as `Failed`. Ordinary invoice reads return completed documents (and legacy documents without generation state), hiding incomplete work.

The PDF service renders HTML with the Chromium already installed in the image. With Blob configuration it uploads an immutable PDF named with invoice and attempt IDs to the private `invoices` container, deletes the temporary local file, and returns a non-SAS Blob URL. Upload completes before MongoDB invoice completion. Local Development/Testing without Blob settings retains `file://` storage; Production requires Blob configuration.

On PDF/upload failure the service releases the claim and tries to delete only the attempt's Blob/file. If database completion throws after being attempted, it reads the invoice back: when the matching completed invoice and storage URL exist, it returns that result, preserving the PDF. If the outcome cannot be confirmed, it attempts to release the claim and preserves the PDF because completion may have committed.

`GET /invoices/{invoiceId}/pdf` validates the ID, loads a completed invoice, and streams `application/pdf` with an attachment name and private/no-store caching. The storage adapter reads only valid attempt names in its configured container, rather than fetching arbitrary URLs. Missing/incomplete invoices and missing PDFs return `404`; storage failures use the safe `500` contract. Existing metadata routes and DTOs remain compatible.

STRIPE/3 adds `GET /invoices/by-order/{orderId}` through the existing completed-only repository read. It lets callers reconcile ambiguous creation results without changing the creation conflict or lease policy.

## Consequences

The unique index guarantees at most one invoice document per order, while lease-based recovery permits another attempt after failure or expiry. A crash or ambiguous completion can leave an orphaned Blob; automated orphan cleanup remains deferred. Blob PDFs survive API replacement and are accessed through the API/BFF without making the container public. Local-mode files remain instance-local and are not migrated automatically. Readiness checks MongoDB, not Blob permissions or rendering, so deployed PDF creation and download must be verified separately.

See `InvoiceService`, `InvoiceGenerationRepository`, `InvoiceRepository`, `InvoicePdfService` and their integration/acceptance tests.

## Alternatives considered

- Generate the PDF before reserving an order in MongoDB: racing requests could both produce a file and attempt to create the invoice.
- Treat every completion exception as a failed write: a lost acknowledgment after a committed update could cause an unnecessary retry or deletion of the completed file.
- Keep `file://` storage for multiple API instances: an invoice read on another instance would not make the local file accessible; shared durable storage remains follow-up work.
