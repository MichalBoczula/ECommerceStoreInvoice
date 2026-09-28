# ADR-0008: Publish the scanned Invoices image to Docker Hub

- Status: Accepted
- Date: 2026-09-28

## Context

The existing CI quality gate checks the source, tests, coverage, contracts and
security before building and scanning a local image. Consumers need a published
image from a successful `master` build. The earlier CI decision kept the image
unpublished until the registry and publication policy were agreed.

## Decision

Keep the quality gate and high/critical Trivy scan. Pull requests only build
and scan the local image. On a push to `master`, log in to Docker Hub using
GitHub Actions secrets `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN`, then tag
and push the same scanned image as `mb0101/ecommerce-store-invoice-api:latest` and
`mb0101/ecommerce-store-invoice-api:<full commit SHA>`. Check that both tags refer to the
scanned local image and that both pushes report the same registry digest.
A failure in any required check, image scan, login or push fails the job.

## Consequences

Only a successful `master` workflow publishes. The full SHA tag identifies
the build; `latest` moves to the latest successful publication. The workflow
publishes an image but does not deploy it. Docker Hub repository access,
credentials and token rotation are operational responsibilities.

## Alternatives considered

- Publish from pull requests: exposes unmerged images.
- Rebuild after scanning: could publish an image different from the scanned one.
- Publish before the scan: allows an image with a blocking finding into the registry.
