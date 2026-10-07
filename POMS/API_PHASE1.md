# Phase 1 API Boundary

POMS now exposes a read-only, versioned API beside the existing ASP.NET Core MVC application.
Razor routes and forms remain the production UI while mobile, integration, and future React clients
can be developed against stable DTO contracts.

## Routes

| Method | Route | Policy | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/v1/patients` | `DataEntry` | Search and page patients |
| `GET` | `/api/v1/patients/{id}` | `DataEntry` | Get one patient |
| `GET` | `/api/v1/appointments` | `AnyAuthenticatedUser` | Filter and page visible appointments |
| `GET` | `/api/v1/appointments/{id}` | `AnyAuthenticatedUser` | Get one visible appointment |

List routes return `items`, `page`, `pageSize`, `totalCount`, and `totalPages`. Page size is limited
to 100. API contracts use strings for enum values and do not serialize EF Core entities.

## Authentication boundary

Phase 1 deliberately reuses the existing ASP.NET Core Identity cookie while API contracts and
authorization are extracted. API requests return HTTP 401/403 rather than redirecting to HTML login
or access-denied pages.

Cookie authentication is suitable for the same-origin Razor UI and early internal API testing. It
is not the final authentication mechanism for mobile applications or external organizations.
Before exposing the API externally, add a standards-based OAuth 2.0/OpenID Connect authority,
scoped access tokens, client registration, rate limits, and an external integration audit policy.

## Compatibility rule

Phase 1 is additive. Do not remove an MVC action when introducing an API endpoint. Extract shared
commands or query services only when both surfaces need the same behavior, then verify both callers.
