const value = (name: string) => (import.meta.env[name] as string | undefined)?.trim()

export const config = {
  apiUrl: (value('VITE_API_URL') ?? 'http://localhost:5015').replace(/\/$/, ''),
  oidc: {
    authority: value('VITE_OIDC_AUTHORITY'),
    clientId: value('VITE_OIDC_CLIENT_ID'),
    scope: value('VITE_OIDC_SCOPE') ?? 'openid profile email',
    redirectUri: value('VITE_OIDC_REDIRECT_URI') ?? `${window.location.origin}/auth/callback`,
    postLogoutRedirectUri: value('VITE_OIDC_POST_LOGOUT_REDIRECT_URI') ?? `${window.location.origin}/signin`,
  },
}

export const isOidcConfigured = Boolean(config.oidc.authority && config.oidc.clientId)
