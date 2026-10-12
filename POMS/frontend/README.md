# POMS React frontend

This is the independently deployable POMS browser client. It calls the versioned `Poms.Api` endpoints and signs in through ASP.NET Core Identity bearer-token endpoints hosted by that API.

## Local development

```powershell
Copy-Item .env.example .env.local
npm install
npm run dev
```

Set `VITE_API_URL` to the API origin and configure the API host with `Cors__AllowedOrigins__0=http://localhost:5173`. Database credentials and API secrets must never be placed in `VITE_*` variables.

## Production build

```powershell
npm run lint
npm run build
```

Deploy `dist/` to a static host. Configure `VITE_API_URL` at build time. Routes must fall back to `index.html` so pages such as `/patients`, `/appointments`, and `/signin` load directly.
