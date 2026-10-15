# POMS React frontend

The browser client for POMS. It calls the `Poms.Api` endpoints and signs in with ASP.NET Core
Identity bearer tokens from the same API.

## Run locally

```powershell
Copy-Item .env.example .env.local   # VITE_API_URL=http://localhost:5015
npm install
npm run dev                         # http://localhost:5173
```

The API must allow the origin: `Cors__AllowedOrigins__0=http://localhost:5173` (the default in
`Poms.Api/appsettings.json`). Never put secrets in `VITE_*` variables; they are public.

## Build

```powershell
npm run lint
npm run build      # output in dist/
```

`VITE_API_URL` is fixed at build time. In the Contabo stack it is the site's own address, because
Caddy routes `/api` and `/auth` to the API. The Docker image serves `dist/` with nginx and falls
back to `index.html` so deep links such as `/patients/123` load directly.

## Structure

| Path | Contents |
| --- | --- |
| `src/styles/theme.css` | Design tokens and the Light, Dark, Classic and compact-density themes |
| `src/styles/app.css` | Layout, components, pages and responsive rules |
| `src/lib/` | `format.ts` (dates, labels, file download), `theme.tsx`, `toast.tsx`, `access.ts` (role rules) |
| `src/components/Ui.tsx` | PageHeader, Card, Badge, Field, Modal, FormModal, ConfirmModal, Pagination, Segmented |
| `src/components/` | App shell, combobox, patient search and picker, clinical/appointment/record dialogs, documents, history timeline |
| `src/pages/` | Dashboard, Patients, Registration (also edit), Patient folder, Records, Clinical record, Appointments, Reports, Users & access, Reference data, Settings, Sign-in |
| `src/api/` | Typed API client and contracts |
| `index.html` | Applies the saved theme before first paint |

## Conventions

- Every colour comes from a token in `theme.css`; new screens must work in all themes.
- Dates sent to the API use the local calendar date (`localDate()`), never `toISOString()`.
- Show actions only when `access(roles)` allows them, matching the API policies.
- Confirm destructive actions with `ConfirmModal`; report results with `useToast()`.
- Tables use the `responsive` class and `data-label` cells so they stack on phones.
