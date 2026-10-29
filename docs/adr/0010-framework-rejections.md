# ADR-0010: Safe framework rejection problems

- Status: Accepted
- Date: 2026-10-29

## Context

Malformed JSON already has a safe 400 response. Unsupported body media types
could fall through the exception handler to 500, and unmatched routes/methods
returned empty 404/405 responses. Explicit null cart collections/items could
reach mapping and produce a generic server failure. These are HTTP boundary
failures before an application flow should write business state.

## Decision

Retain the existing six-field problem shape and malformed-body signature.
Map unsupported media to a safe 415 and declare that response on the three JSON
body endpoints. Fill empty routing 404/405/415 outcomes with safe problems using
the request path and trace ID; preserve the router's Allow header. Do not replace
existing business problems. Health and Swagger middleware stay outside this
fallback. Problems use application/problem+json and no-cache/no-store.

Reject null cart line collections/items at the API boundary with the existing
safe 400 binding signature. Valid line values still use the existing domain
policy. Preserve optional client null strings and null status behavior documented
in INV-BDD-09; their contract nullability is separate INV-BDD-13 work.

Unmatched routing scenarios have no selected business operation. Project their
identities, actual requested method/path and 404/405 intent separately in
frameworkCases. They cannot close declared business operation/status gaps.

## Consequences

Clients get complete problems for routing/method/media failures. Unsupported
media and malformed cart structure now return client errors rather than 500.
No repository, transaction, invoice claim/recovery or business rule changes.
Acceptance scenarios send exact bytes, compare complete problems/Allow, and
verify all five populated collections plus complete healthy supporting reads.
The existing 77-case baseline and all subsequent cases remain.

## Alternatives considered

- Keep empty routing responses and unsupported-media 500: no complete safe
  framework contract and incorrect server-failure classification.
- Copy the Users envelope: would replace the existing Orders/Invoices contract.
- Count routing failures as business outcomes: would claim an endpoint/flow ran
  when routing rejected the request.
