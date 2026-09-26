# ADR 0003: Invoice claim, PDF generation and recovery

Status: Accepted with limitation (2026-09-26)

## Context

Two requests for one paid order can race, PDF generation can fail, and the database may commit completion while its acknowledgment is lost. An invoice must not be exposed before the PDF is ready.

## Decision

Generation reserves an invoice document keyed by unique `OrderId`. The reservation has a `Generating` state, attempt ID and ten-minute lease. A `Failed` reservation or expired `Generating` lease may be claimed for a new attempt; an active claim and a completed invoice cannot. Only the current attempt may mark the document `Completed` or release it as `Failed`. Ordinary invoice reads return completed documents (and legacy documents without generation state), hiding incomplete work.

The PDF service renders HTML with Playwright Chromium and stores a file named with invoice and attempt IDs on the API instance's local disk. It returns a `file://` URL. On PDF failure the service releases the claim and tries to delete the attempt's file. If database completion throws after being attempted, it reads the invoice back: when the matching completed invoice and storage URL exist, it returns that result, preserving the file. If the outcome cannot be confirmed, it attempts to release the claim and preserves the file because completion may have committed.

## Consequences and follow-up

The unique index guarantees at most one invoice document per order, while lease-based recovery permits another attempt after failure or expiry. A crash can leave an orphaned local PDF. More importantly, `file://` is an instance-local path: another API instance or an external consumer cannot rely on accessing it. Multi-instance deployment requires shared durable object storage, an accessible download contract and orphan cleanup before treating invoice files as available across instances. Readiness currently checks MongoDB, not PDF storage.

See `InvoiceService`, `InvoiceGenerationRepository`, `InvoiceRepository`, `InvoicePdfService` and their integration/acceptance tests.
