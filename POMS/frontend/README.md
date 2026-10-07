# POMS React frontend

This is the independently deployable POMS browser client. It calls the versioned `Poms.Api` endpoints and authenticates through any standards-compliant OpenID Connect provider using Authorization Code + PKCE.

## Local development

```powershell
Copy-Item .env.example .env.local
npm install
npm run dev
```

Configure the OIDC values in `.env.local`. The provider must register these browser-client URLs:

- Redirect URI: `http://localhost:5173/auth/callback`
- Post-logout URI: `http://localhost:5173/signin`
- Allowed web origin: `http://localhost:5173`

Set the API host's `Cors__AllowedOrigins__0=http://localhost:5173`. The browser client never receives the database connection string or API secrets.

## Visual demo mode

For local UI review only, set `VITE_DEMO_MODE=true`. Vite exposes this mode only during development; production builds always require OIDC and the real API.

## Production build

```powershell
npm run lint
npm run build
```

Deploy `dist/` to Cloudflare Pages or another static host. Configure all `VITE_*` values at build time. Routes must fall back to `index.html` so `/patients`, `/appointments`, and `/auth/callback` can load directly.
