# ADR-0005: Generated OpenAPI and source-linked documentation

- Status: Accepted
- Date: 2026-09-26

## Context

A manually maintained specification can drift from endpoint metadata, executed application flows, validation policies and HTTP acceptance behavior.

## Decision

Swashbuckle generates OpenAPI from Minimal API routes, DTOs and `.Produces` declarations. Application services execute flow descriptors, and Domain policies expose validation descriptions through `/orders-documentation/flows` and `/orders-documentation/validations`. The operation-link check derives links from endpoint source to called service, executed descriptor, registered policy and matching `.feature` scenario. The verification script exports OpenAPI from the API, lints it with Redocly and compares its declared responses to real acceptance HTTP outcomes recorded by the tests.

The canonical create-order route is `POST /orders/client/{clientId}`. The legacy `POST /orders/{clientId}` alias is excluded from the generated description. Generated OpenAPI is a verification artifact under `artifacts/verification/`, not a handwritten committed contract. Generated `.feature.cs` files come from Reqnroll sources.

## Consequences

Changing a route requires updating endpoint metadata, source flow/policy links and affected acceptance cases. The checks identify drift in the known operation inventory and exercised response shapes; they cannot prove every possible runtime exception has an acceptance scenario. The metadata, code and `.feature` source must be reviewed together.

See `scripts/check-operation-links.py`, `scripts/validate-openapi.sh`, `scripts/check-openapi-contract.py` and `tests/ECommerceStoreInvoice.Acceptance.Tests/OpenApiExportTests.cs`.

## Alternatives considered

- Maintain a separate handwritten OpenAPI file: route and DTO changes could drift from the published contract.
- Maintain operation-to-flow and scenario links in a manual table: those links could disagree with the executed service and `.feature` sources.
