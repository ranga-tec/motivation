# POMS source

Source for the Exceed Lanka Prosthetic & Orthotic Data Management System. Start with the
repository [`README.md`](../README.md); hosting is in [`DEPLOY_CONTABO.md`](../DEPLOY_CONTABO.md).

| Folder | Contents |
| --- | --- |
| `frontend/` | React client — see [`frontend/README.md`](frontend/README.md) |
| `src/Poms.Api/` | Production API host — see [`API_HOST.md`](API_HOST.md) |
| `src/Poms.Web/Api/V1/` | API controllers and contracts compiled into `Poms.Api` |
| `src/Poms.Web/` (rest) | Legacy Razor MVC application, not deployed |
| `src/Poms.Domain/` | Entities and enums |
| `src/Poms.Infrastructure/` | EF Core context, schema upgraders, seeders, services |
| `src/Poms.Reporting/` | Report queries and QuestPDF forms |
| `tests/Poms.Tests/` | xUnit tests |
| `deploy/contabo/` | Production Docker Compose stack, Caddy, setup and backup scripts |
| `compose.development.yaml` | Full stack locally with PostgreSQL |
| `API_PHASE1.md` | Historical notes on the first API extraction |

```bash
dotnet build POMS.sln -c Release
dotnet test POMS.sln -c Release
```
