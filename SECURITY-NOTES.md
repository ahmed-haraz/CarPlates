# Security review branch notes

Review draft only. No merge, deployment, database migration, live settings change, credential rotation or history rewrite was performed. Read the compatibility limits before considering deployment.

## Implemented

- Authenticated tenant resolution uses exactly one validated companyCode claim. A supplied header must match it exactly. Missing or malformed company codes, duplicate headers and missing claims are rejected. Anonymous tenant routing is limited to login and refresh and requires an explicit company header. There is no default-company routing or local-database fallback on lookup failure.
- JWT session validation uses the already cryptographically validated principal while constructing the tenant-scoped database context. The previous request principal is restored afterward. Database/tenant errors now fail token validation instead of skipping revocation checks.
- Swagger is available only in Development.

## Not fixed: shared legacy password storage

DES password comparison and storage remain unchanged. This is a known high-risk finding, not a completed fix. Blindly writing password hashes into the existing ERP password columns could break desktop/mobile/other shared clients, and column widths/formats have not been verified. BankServices also accepts pre-encrypted values in its update flow, which needs to be removed during the approved cutover.

Required design review: inventory all consumers and columns, select a modern salted one-way password format, define a versioned/dedicated credential store and ownership, approve any schema changes, define account recovery, and test migration/rollback with every client. After that, hash new credentials and upgrade verified legacy credentials at sign-in through a controlled cutover. Do not add a permanent plaintext/DES fallback or merely change the DES key. No migration or credential changes are included here.

## Compatibility and remaining controls

- Existing clients must send an explicit valid company header on login/refresh. Existing tokens need exactly one valid companyCode claim. Allowed company codes are 1-64 ASCII letters, digits, hyphens or underscores. Check the real company-code format before deployment.
- Tenant lookup outages now deny access rather than using a local database. Headers that disagree with tokens no longer select another database. This deliberately changes prior behavior.
- Tenant denial exceptions currently flow through the existing generic error middleware for some paths. Verify client handling of denial/error responses and operational monitoring.
- External company-resolution service authorization and the upstream database mapping remain unverified. This branch does not prove tenant isolation for every query or permission boundary.
- Financial/CRUD operation roles, device binding, session/refresh behavior, cross-tenant test databases, production proxy/TLS and shared database compatibility need staging tests. No live API attack tests or database calls were made.
- Any secrets in settings/history are not rotated or removed by these changes. Review all secrets separately and coordinate rotation outside this branch.

## CarPlates-specific changes and limits

- Self-service registration is disabled. Anonymous callers must authenticate; authenticated callers receive 403. No new tenant-admin or invitation flow is guessed. The old internal registration service remains, but the endpoint does not call it. Enrollment needs a separately reviewed design before re-enabling.
- Production requests must be HTTPS according to the server request scheme. Plaintext requests are rejected, not redirected. The hard-coded all-interface HTTP listener was removed. Configure real Kestrel HTTPS certificates/endpoints before deployment. A TLS-terminating proxy will need an explicitly reviewed trusted-proxy/forwarded-header configuration; none is trusted implicitly here. Do not disable the production HTTPS guard just to make a proxy work. Block direct backend ports at the network edge.
- Login and refresh have an in-process per-remote-IP limit of 10 requests per minute with no queue and a 429 response. This is not distributed or account-aware. Behind a proxy, callers may share a bucket; coordinate safe proxy handling and edge/account-aware limits. Development bypasses the HTTPS requirement but not this limiter.
- Full CarPlates.API project build succeeded using .NET SDK 10.0.404. NuGet reported pre-existing vulnerability warnings for AutoMapper 16.2.0 (GHSA-6vhm-j9h7-f9gh, GHSA-gpwp-5797-mjff) and transitive System.Security.Cryptography.Pkcs 9.0.4 (GHSA-2fmv-rv2c-59x7, GHSA-846f-f764-76mx). No dependency upgrade is included; review compatible fixed versions separately.

## Offline validation

A local harness ran 29 checks across the two repositories, with no live network services or databases contacted. Fourteen checks per repository covered explicit anonymous login/refresh tenant routing; denial of other anonymous routes, missing headers/claims, duplicate headers/claims, malformed claims and cross-tenant headers; valid claim/matching-header selection; lookup failure with no fallback; JWT session database-resolution failure rejection; and restoration of the temporary request principal. One extra check confirmed the CarPlates registration action returns 403 without calling the service. BankServices used the isolated substitutes described above. These are focused regression checks, not end-to-end application or deployment tests. HTTPS middleware, endpoint throttling, successful database-backed sessions and multi-client password compatibility still need staging tests.

Before deployment, run negative authentication/cross-tenant tests with real non-production databases, verify that normal login/refresh/logout still work, confirm revocation failures deny access, verify tenant-service outages never route elsewhere, check 429 behavior and proxy IP handling, and confirm no production Swagger route is reachable. Review every intended compatibility break above.
