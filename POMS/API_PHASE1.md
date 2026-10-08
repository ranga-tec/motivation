# Phase 1 API Boundary

POMS exposes a versioned API beside the existing ASP.NET Core MVC application.
Razor routes and forms remain the production UI while mobile, integration, and future React clients
can be developed against stable DTO contracts.

## Routes

| Method | Route | Policy | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/v1/patients` | `DataEntry` | Search and page patients |
| `GET` | `/api/v1/patients/{id}` | `DataEntry` | Get one patient |
| `GET` | `/api/v1/patients/registration-options` | `DataEntry` | Get active locations, centres, referrals, and assignees |
| `POST` | `/api/v1/patients` | `DataEntry` + bearer-only `ApiWrite` | Register a patient |
| `GET` | `/api/v1/appointments` | `AnyAuthenticatedUser` | Filter and page visible appointments |
| `GET` | `/api/v1/appointments/{id}` | `AnyAuthenticatedUser` | Get one visible appointment |
| `GET` | `/api/v1/appointments/options` | `AnyAuthenticatedUser` | Get appointment types and active assignees |
| `POST` | `/api/v1/appointments` | `DataEntry` + bearer-only `ApiWrite` | Schedule an appointment |
| `POST` | `/api/v1/appointments/{id}/complete` | `DataEntry` + bearer-only `ApiWrite` | Complete a scheduled appointment |
| `POST` | `/api/v1/appointments/{id}/cancel` | `DataEntry` + bearer-only `ApiWrite` | Cancel a scheduled appointment with a reason |
| `POST` | `/api/v1/appointments/{id}/reschedule` | `DataEntry` + bearer-only `ApiWrite` | Move a scheduled appointment and preserve its previous schedule |
| `GET` | `/api/v1/episodes/options` | `ClinicianOrAdmin` | Get active centres and record statuses |
| `GET` | `/api/v1/episodes?patientId={id}` | `ClinicianOrAdmin` | List visible records for one patient |
| `GET` | `/api/v1/episodes/{id}` | `ClinicianOrAdmin` | Get one accessible patient record |
| `POST` | `/api/v1/episodes` | `ClinicianOrAdmin` + bearer-only `ApiWrite` | Create a patient record |
| `PUT` | `/api/v1/episodes/{id}` | `ClinicianOrAdmin` + bearer-only `ApiWrite` | Update a patient record without changing its patient |

List routes return `items`, `page`, `pageSize`, `totalCount`, and `totalPages`. Page size is limited
to 100. API contracts use strings for enum values and do not serialize EF Core entities.

## Authentication boundary

Phase 1 deliberately reuses the existing ASP.NET Core Identity cookie while API contracts and
authorization are extracted. API requests return HTTP 401/403 rather than redirecting to HTML login
or access-denied pages.

Cookie authentication remains available for the same-origin Razor UI and internal API testing.
The API also supports JWT bearer tokens issued by a standards-based OAuth 2.0/OpenID Connect
authority when the following production settings are supplied:

```text
ApiAuthentication__Enabled=true
ApiAuthentication__Authority=https://identity.example.com
ApiAuthentication__Audience=poms-api
ApiAuthentication__RequireHttpsMetadata=true
```

Requests with an `Authorization: Bearer ...` header use JWT validation. Requests without that
header use the existing Identity cookie. Enabling bearer authentication without both authority and
audience values fails startup rather than accepting unverifiable tokens.

The application does not issue access tokens. Before external use, configure mobile/SPA and
machine-to-machine clients at the selected OIDC authority, map its role or scope claims to POMS
authorization policies, add rate limits, and define an external integration audit policy.

Write routes require an explicit bearer token even when hosted inside `Poms.Web`; Identity cookies
cannot invoke them. Patient and appointment writes return ProblemDetails-compatible validation and
state-conflict responses. Patient registration returns `201 Created`; appointment scheduling returns
`201 Created`, while state transitions return the updated appointment. Photo and legacy OCR import
remain in MVC until object storage is added.

## Compatibility rule

Phase 1 is additive. Do not remove an MVC action when introducing an API endpoint. Extract shared
commands or query services only when both surfaces need the same behavior, then verify both callers.
