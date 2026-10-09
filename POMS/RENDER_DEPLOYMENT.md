# POMS demo deployment: Render + Neon

This deploys the existing React/API/OIDC architecture. It does not replace Keycloak with a different
authentication system.

## Important limitation

Render Free has an ephemeral filesystem. The API is configured with `/tmp/poms-storage` so it can
start, but uploaded patient documents will disappear after a restart, spin-down, or redeploy. Do not
upload real patient documents until an object-storage implementation is deployed.

## 1. Create the Neon databases

Create two Neon projects in the same region as the Render services (Singapore when available):

- `poms-production` for POMS application data.
- `poms-identity` for Keycloak data.

For POMS, copy the pooled PostgreSQL URL. It is used as `DATABASE_URL`.

For Keycloak, use the direct (non-pooler) connection details and construct:

```text
KC_DB_URL=jdbc:postgresql://NEON_DIRECT_HOST/neondb?sslmode=require
KC_DB_USERNAME=NEON_USERNAME
KC_DB_PASSWORD=NEON_PASSWORD
```

Never commit these values.

## 2. Create the Render Blueprint

1. Sign in to Render and connect the GitHub account that can read `ranga-tec/motivation`.
2. Select **New > Blueprint**.
3. Select the `motivation` repository and the `main` branch.
4. Render detects the root `render.yaml` and asks for the private values below.

Use these values:

| Variable | Service | Value |
|---|---|---|
| `KC_DB_URL` | poms-identity | Keycloak JDBC URL from step 1 |
| `KC_DB_USERNAME` | poms-identity | Keycloak Neon username |
| `KC_DB_PASSWORD` | poms-identity | Keycloak Neon password |
| `KC_HOSTNAME` | poms-identity | `https://poms-identity.onrender.com` |
| `KC_BOOTSTRAP_ADMIN_USERNAME` | poms-identity | A private Keycloak console username |
| `KC_BOOTSTRAP_ADMIN_PASSWORD` | poms-identity | A generated password of at least 20 characters |
| `DATABASE_URL` | poms-api | POMS pooled Neon PostgreSQL URL |
| `ApiAuthentication__Authority` | poms-api | `https://poms-identity.onrender.com/realms/poms` |
| `Cors__AllowedOrigins__0` | poms-api | The final Cloudflare Pages origin; temporarily use the expected Pages URL |
| `BootstrapAdmin__Email` | poms-api | The real POMS administrator email |
| `BootstrapAdmin__Password` | poms-api | A separate generated password of at least 20 characters |

If Render assigns a different hostname, update `KC_HOSTNAME` and
`ApiAuthentication__Authority` to the actual HTTPS hostname and redeploy both services.

## 3. Verify Keycloak

### Free-plan first deployment

Render's free 512 MB instance cannot reliably run Keycloak's initial Liquibase migration and
realm import. Initialize the empty `poms-identity` Neon database once from a machine with Docker,
using `POMS/docker/keycloak/Dockerfile.render`, the same `KC_DB_*` values, and
`--import-realm`. Stop and remove that temporary container after the log reports both
`Realm 'poms' imported` and `Keycloak ... started`. Normal Render starts intentionally omit
`--import-realm`; the imported realm remains in Neon.

The production Render service uses a local cache, a five-connection database pool, and a capped
JVM heap. These values are declared in the root `render.yaml` and are required for the free plan.

Wait for `poms-identity` to show **Live**, then open:

```text
https://poms-identity.onrender.com/realms/poms/.well-known/openid-configuration
```

It must return JSON. Then open:

```text
https://poms-identity.onrender.com/admin
```

Sign in with the `KC_BOOTSTRAP_ADMIN_*` credentials. In the `poms` realm:

1. Create a user whose email exactly matches `BootstrapAdmin__Email`.
2. Mark the email as verified.
3. Set a permanent password.
4. Assign the `ADMIN` realm role.

The email match is required: Keycloak owns login credentials while POMS owns application roles and
staff profiles.

## 4. Verify the API

Wait for `poms-api` to show **Live**, then open:

```text
https://poms-api.onrender.com/health
```

Expected response: HTTP 200 with `Healthy`. An unauthenticated request to
`/api/v1/patients` must return HTTP 401.

## 5. Connect the frontend later

After Cloudflare Pages provides the final origin, update all three locations:

1. Render `poms-api`: `Cors__AllowedOrigins__0=https://YOUR_PROJECT.pages.dev`
2. Keycloak `poms` realm, client `poms-web`: Valid redirect URI
   `https://YOUR_PROJECT.pages.dev/*`
3. Keycloak `poms` realm, client `poms-web`: Web origin
   `https://YOUR_PROJECT.pages.dev`

The frontend build variables must use the same API and Keycloak URLs.

## Cold-start expectations

Render Free services sleep after inactivity. The first Keycloak login and the first API request can
each take about one minute. This deployment is suitable for a demonstration, not live clinical use.
