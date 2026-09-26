# ADR 0002: Checkout transaction and order status writes

Status: Accepted (2026-09-26)

## Context

An order is valid only if product snapshots, order and cleared cart agree. A failed write must not leave a partial checkout. Concurrent status requests must not silently overwrite each other.

## Decision

The Application service validates client/cart/products before starting a MongoDB session. It inserts product versions, inserts the order and updates the emptied cart inside one transaction. On failure it aborts the session and propagates the error. A replica set is required even for local development and tests.

The Domain rule permits `Created → Paid` and `Created → Cancelled`. A repeat `Paid`, a change after `Paid` or `Cancelled`, and any other transition fail validation with `400`. Infrastructure replaces an order only if its persisted status is still `Created`; a stale concurrent update produces `OrderWriteConflictException` and HTTP `409`. The stored order remains the winner's state.

## Consequences

ProductsCatalog is contacted before the transaction. Failure to retrieve a requested product prevents checkout from starting; transaction failure does not clear the cart or persist product snapshots/order. MongoDB transactions require a writable primary with sessions. A status change updates the current order document; there is no standalone immutable order-status event log.

See `OrderService`, `MongoOrderWriteTransaction`, `OrderRepository`, Domain status validation and `OrderCheckoutTransactionTests`.
