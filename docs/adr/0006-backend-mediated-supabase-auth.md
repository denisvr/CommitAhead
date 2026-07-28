# Backend-mediated Supabase auth; no Supabase keys in the React bundle

Supabase is typically integrated with a client-side SDK using the public anon key embedded in the frontend. We use a backend-mediated flow instead: the magic-link PKCE exchange is completed by the ASP.NET Core backend, which sets `Secure`, `HttpOnly`, `SameSite=Strict` session cookies. The Supabase anon key, service-role key, and AI provider key never reach the browser.

The reason is defence-in-depth for a private app containing sensitive career data. Client-side token storage (even `httpOnly` cookies issued by a client-side SDK) exposes tokens to XSS and browser extensions. Confining all key material to the server eliminates that surface. The added backend complexity (PKCE callback endpoint, cookie-based session management, CSRF protection) is proportionate to the sensitivity of the data.

**Consequence**: The PKCE state cookie must be `SameSite=Lax` (not `Strict`) to survive the redirect from the email magic link. Session cookies remain `SameSite=Strict`.
