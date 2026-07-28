---
status: accepted
date: 2026-07-28
---

# Backend-mediated Supabase auth; no Supabase keys in the React bundle

## Context

Supabase is typically integrated with a client-side SDK using the public anon key embedded in the frontend. This is appropriate for multi-tenant apps where users own their own data. CommitAhead holds sensitive career data for a single private user and has no registration flow.

## Decision

Authentication is fully backend-mediated using Supabase magic link with PKCE:

1. Magic-link initiation accepts only the owner's email address, returns a generic response regardless of outcome, and is rate-limited.
2. The PKCE exchange is completed by the ASP.NET Core backend via a callback endpoint. The backend sets `Secure`, `HttpOnly`, `SameSite=Strict` session cookies.
3. The **PKCE state cookie** uses `SameSite=Lax` (not `Strict`) to survive the cross-site redirect from the email magic link. All session cookies revert to `Strict` after the exchange.
4. Every API request is validated by middleware: issuer, audience, signature, and expiry are checked against Supabase's JWKS; `sub` must equal `OWNER_USER_ID` (stored in protected server configuration). Any other authenticated Supabase identity receives 403.
5. Access tokens expire after **15 minutes**. The frontend refreshes proactively before expiry or retries once after a 401, using a single-flight request. The refresh cookie is scoped to `/auth/refresh` and requires POST + CSRF validation. Refresh tokens rotate atomically on each use.
6. An **absolute session timeout of 7 days** applies regardless of activity; re-authentication is required after it.
7. `POST /auth/logout` revokes the refresh token server-side and clears all cookies. An issued access token remains valid for up to 15 minutes after logout — this window is accepted. No server-side denylist is maintained in the MVP.
8. CSRF validation is required on all state-changing requests.
9. The Supabase anon key, service-role key, and AI provider key are never sent to the browser.

## Consequences

- The browser still holds session cookies, so `HttpOnly` reduces (not eliminates) the token-theft surface. The primary protection is the 15-minute access token lifetime combined with `SameSite=Strict` on session cookies.
- Backend complexity increases: PKCE callback endpoint, cookie management, CSRF middleware, and single-flight refresh logic are all required.
- Public signup must be disabled in Supabase; the owner account must be pre-created.

## Considered Alternatives

A client-side Supabase SDK with `localStorage` storage exposes tokens to XSS and requires the anon key in the browser. A browser SDK cannot create `HttpOnly` cookies; doing so requires a server or server-side auth helper. Backend mediation keeps Supabase keys and token exchange logic on the server while supporting the magic-link flow.
