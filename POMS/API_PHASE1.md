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
| `GET` | `/api/v1/clinical-records/options` | `ClinicianOrAdmin` | Get active clinical lookups and enum options |
| `GET` | `/api/v1/clinical-records/prescription-options` | `ClinicianOrAdmin` | Get prescriptions for an assessment type and limb category |
| `GET` | `/api/v1/clinical-records/episodes/{id}` | `ClinicianOrAdmin` | Get the accessible clinical timeline for one patient record |
| `POST/PUT` | `/api/v1/clinical-records/assessments[/{id}]` | `ClinicianOrAdmin` + bearer-only `ApiWrite` | Create or update assessments and prescriptions |
| `POST/PUT` | `/api/v1/clinical-records/fittings[/{id}]` | `ClinicianOrAdmin` + bearer-only `ApiWrite` | Create or update fittings |
| `POST/PUT` | `/api/v1/clinical-records/deliveries[/{id}]` | `ClinicianOrAdmin` + bearer-only `ApiWrite` | Create or update deliveries |
| `POST/PUT` | `/api/v1/clinical-records/follow-ups[/{id}]` | `ClinicianOrAdmin` + bearer-only `ApiWrite` | Create or update follow-ups |
| `GET` | `/api/v1/documents/options` | `AnyAuthenticatedUser` | Get document types and upload limits |
| `GET` | `/api/v1/documents?patientId={id}` or `?episodeId={id}` | `AnyAuthenticatedUser` | List accessible patient or record documents |
| `POST` | `/api/v1/documents` | `AnyAuthenticatedUser` + bearer-only `ApiWrite` | Upload a patient or record document |
| `GET/DELETE` | `/api/v1/documents/{id}?scope=patient|episode` | `AnyAuthenticatedUser` (`DELETE` also requires `ApiWrite`) | Download or soft-delete an accessible document |
| `GET` | `/api/v1/documents/patient-photo/{patientId}` | `AnyAuthenticatedUser` | Download the latest accessible patient photo without caching |
| `GET` | `/api/v1/print/patients/{id}/registration` | `AnyAuthenticatedUser` | Generate the patient registration PDF |
| `GET` | `/api/v1/print/assessments/{id}` | `AnyAuthenticatedUser` | Generate the assessment PDF |
| `GET` | `/api/v1/print/assessments/{id}/prescription` | `AnyAuthenticatedUser` | Generate the prescription PDF |
| `GET` | `/api/v1/print/deliveries/{id}` | `AnyAuthenticatedUser` | Generate the delivery-note PDF |
| `GET` | `/api/v1/print/follow-ups/{id}` | `AnyAuthenticatedUser` | Generate the follow-up PDF |

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
`201 Created`, while state transitions return the updated appointment. Document APIs depend on
`IFileStorageService`, so the local file implementation can be replaced by object storage without
changing the API contract. Legacy OCR import remains in MVC.

## Compatibility rule

Phase 1 is additive. Do not remove an MVC action when introducing an API endpoint. Extract shared
commands or query services only when both surfaces need the same behavior, then verify both callers.
