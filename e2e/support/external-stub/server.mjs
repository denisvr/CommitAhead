// CommitAhead E2E external-stub — deterministic, dependency-free replacement for the real
// Supabase Auth endpoints (E2E Foundation Plan). Supports exactly:
//   POST /auth/v1/token?grant_type=refresh_token   (Supabase refresh)
//   POST /auth/v1/logout                    (Supabase logout)
//   GET  /__stub/unexpected                 (verification helper)
// Anything else responds 501 and is recorded, so verify-foundation.mjs can assert the count is
// exactly zero for a real run — an unsupported request is a defect to surface, not to guess at.
// Never logs request bodies, cookies, tokens, or any header value — only the method and path.

import http from 'node:http';
import crypto from 'node:crypto';

const PORT = Number(process.env.PORT ?? 8080);
const SIGNING_KEY = process.env.E2E_SIGNING_KEY;
const ISSUER = process.env.E2E_ISSUER;
const SUPABASE_USER_ID = process.env.E2E_SUPABASE_USER_ID;
const SUPABASE_ANON_KEY = process.env.SUPABASE_ANON_KEY_SENTINEL;

for (const [name, value] of Object.entries({
  E2E_SIGNING_KEY: SIGNING_KEY,
  E2E_ISSUER: ISSUER,
  E2E_SUPABASE_USER_ID: SUPABASE_USER_ID,
  SUPABASE_ANON_KEY_SENTINEL: SUPABASE_ANON_KEY,
})) {
  if (!value) {
    console.error(`external-stub: missing required environment variable ${name}`);
    process.exit(1);
  }
}

const unexpectedRequests = [];

function base64url(input) {
  return Buffer.from(input).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

// Minimal HS256 JWT — no dependency, matches exactly what E2ESessionController and the real
// AuthenticationServiceCollectionExtensions E2E branch validate: iss, aud, sub, iat, nbf, exp.
function signJwt(claims) {
  const encodedHeader = base64url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const encodedPayload = base64url(JSON.stringify(claims));
  const signature = crypto.createHmac('sha256', SIGNING_KEY).update(`${encodedHeader}.${encodedPayload}`).digest();
  return `${encodedHeader}.${encodedPayload}.${base64url(signature)}`;
}

function mintAccessToken() {
  const nowSeconds = Math.floor(Date.now() / 1000);
  const expiresInSeconds = 600; // well within the app's 15-minute effective iat cap
  const token = signJwt({
    iss: ISSUER,
    aud: 'authenticated',
    sub: SUPABASE_USER_ID,
    iat: nowSeconds,
    nbf: nowSeconds,
    exp: nowSeconds + expiresInSeconds,
  });
  return { token, expiresInSeconds };
}

function sendJson(res, statusCode, body) {
  const payload = JSON.stringify(body);
  res.writeHead(statusCode, { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(payload) });
  res.end(payload);
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    req.on('data', (chunk) => chunks.push(chunk));
    req.on('end', () => resolve(Buffer.concat(chunks)));
    req.on('error', reject);
  });
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, `http://${req.headers.host ?? 'external-stub'}`);
  const path = url.pathname;
  const method = req.method;

  // Every request body is drained here, even though no route below inspects it — keeps the
  // socket-handling shape uniform regardless of which routes exist.
  await readBody(req);

  console.log(`external-stub: ${method} ${path}`);

  if (method === 'GET' && path === '/__stub/unexpected') {
    return sendJson(res, 200, { count: unexpectedRequests.length, requests: unexpectedRequests });
  }

  if (method === 'POST' && path === '/auth/v1/token' && url.searchParams.get('grant_type') === 'refresh_token') {
    if (req.headers['apikey'] !== SUPABASE_ANON_KEY) {
      return sendJson(res, 401, { error: 'invalid apikey' });
    }

    const { token, expiresInSeconds } = mintAccessToken();
    return sendJson(res, 200, {
      access_token: token,
      refresh_token: 'e2e-rotated-refresh-token',
      expires_in: expiresInSeconds,
      user: { id: SUPABASE_USER_ID },
    });
  }

  if (method === 'POST' && path === '/auth/v1/logout') {
    if (req.headers['apikey'] !== SUPABASE_ANON_KEY) {
      return sendJson(res, 401, { error: 'invalid apikey' });
    }

    res.writeHead(204);
    return res.end();
  }

  unexpectedRequests.push({ method, path });
  return sendJson(res, 501, { error: 'unsupported by the deterministic E2E external stub', method, path });
});

server.listen(PORT, () => {
  console.log(`external-stub: listening on :${PORT}`);
});
