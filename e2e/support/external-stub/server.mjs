// CommitAhead E2E external-stub — deterministic, dependency-free replacement for real Supabase
// Auth and Anthropic endpoints (E2E Foundation Plan). Supports exactly:
//   POST /v1/messages/count_tokens          (Anthropic)
//   POST /v1/messages                       (Anthropic)
//   POST /auth/v1/token?grant_type=refresh_token   (Supabase refresh)
//   POST /auth/v1/logout                    (Supabase logout)
//   GET  /__stub/unexpected                 (verification helper)
// Anything else responds 501 and is recorded, so verify-foundation.mjs can assert the count is
// exactly zero for a real run — an unsupported request is a defect to surface, not to guess at.
// Never logs request bodies, cookies, tokens, or any header value — only the method and path.
//
// POST /v1/messages is the one exception to "never inspected": it parses its own body to pick
// between exactly two fixed, deterministic responses — the AnalyzeJobAnalysis
// AddJobRequirement/AddJobGap pair-response Journey 3 needs, or the original empty-output
// fallback for everything else (including malformed JSON) — by structurally classifying the
// request's own Structured Outputs schema (which StructuredSuggestionCommandType variants it
// declares), never by matching request text. The body is still never logged.

import http from 'node:http';
import crypto from 'node:crypto';

const PORT = Number(process.env.PORT ?? 8080);
const SIGNING_KEY = process.env.E2E_SIGNING_KEY;
const ISSUER = process.env.E2E_ISSUER;
const SUPABASE_USER_ID = process.env.E2E_SUPABASE_USER_ID;
const ANTHROPIC_API_KEY = process.env.ANTHROPIC_API_KEY_SENTINEL;
const SUPABASE_ANON_KEY = process.env.SUPABASE_ANON_KEY_SENTINEL;

for (const [name, value] of Object.entries({
  E2E_SIGNING_KEY: SIGNING_KEY,
  E2E_ISSUER: ISSUER,
  E2E_SUPABASE_USER_ID: SUPABASE_USER_ID,
  ANTHROPIC_API_KEY_SENTINEL: ANTHROPIC_API_KEY,
  SUPABASE_ANON_KEY_SENTINEL: SUPABASE_ANON_KEY,
})) {
  if (!value) {
    console.error(`external-stub: missing required environment variable ${name}`);
    process.exit(1);
  }
}

const unexpectedRequests = [];

// The two fixed /v1/messages outputs (E2E Foundation Plan / Journey 3) — nothing else. Not a
// scenario engine: exactly one structural classifier below picks between exactly these two.
const EMPTY_OUTPUT_RESULT = { suggestionProposals: [], linkProposals: [], studyItemProposals: [] };

// Mirrors AnthropicStructuredOutputSchema's AddJobRequirement/AddJobGap shapes exactly — one pair
// the journey accepts, one pair it rejects, so JobAnalysisDetailPage's Requirements/Gaps sections
// after Apply prove both halves of "accept some, reject others." Two casing conventions, on
// purpose, matching the schema itself (see AnthropicStructuredOutputSchema's own doc comment): the
// envelope fields (commandType/payload/advisoryMarkdown) are camelCase; everything *inside* each
// payload is the canonical PascalCase every real consumer (AiStructuredSuggestionValidator, the
// frontend's payloadFields.ts) already expects — this is not a typo.
const ANALYZE_JOB_ANALYSIS_RESULT = {
  suggestionProposals: [
    {
      commandType: 'AddJobRequirement',
      payload: {
        ProposalKey: 'req-cache-invalidation',
        Text: 'Design and implement cache invalidation strategies for distributed systems',
        Kind: 'Technical',
        Priority: 'Required',
        SourceExcerpt: 'Must have hands-on experience designing and implementing cache invalidation strategies at scale.',
      },
      advisoryMarkdown: null,
    },
    {
      commandType: 'AddJobGap',
      payload: {
        ExistingRequirementId: null,
        ProposedRequirementKey: 'req-cache-invalidation',
        MatchLevel: 'Missing',
        Severity: 'High',
        Rationale: "No cache invalidation work is documented in the candidate's profile or study catalogue.",
      },
      advisoryMarkdown: null,
    },
    {
      commandType: 'AddJobRequirement',
      payload: {
        ProposalKey: 'req-graphql-api',
        Text: 'Familiarity with GraphQL API design',
        Kind: 'Technical',
        Priority: 'Preferred',
        SourceExcerpt: 'Experience with GraphQL is a plus but not required.',
      },
      advisoryMarkdown: null,
    },
    {
      commandType: 'AddJobGap',
      payload: {
        ExistingRequirementId: null,
        ProposedRequirementKey: 'req-graphql-api',
        MatchLevel: 'Partial',
        Severity: 'Low',
        Rationale: 'Some API design experience exists but no direct GraphQL exposure.',
      },
      advisoryMarkdown: null,
    },
  ],
  linkProposals: [],
  studyItemProposals: [],
};

// Structural classifier — reads the request's own Structured Outputs schema (which
// StructuredSuggestionCommandType variants AnthropicStructuredOutputSchema declared for this
// call), never the free-text prompt. Array.isArray() guards every collection before iterating —
// optional chaining alone stops at `undefined`/`null` but not at a malformed non-array value.
function suggestionProposalCommandTypes(parsedBody) {
  const variants = parsedBody?.output_config?.format?.schema?.properties?.suggestionProposals?.items?.anyOf;
  const commandTypes = new Set();
  if (!Array.isArray(variants)) {
    return commandTypes;
  }

  for (const variant of variants) {
    const enumValues = variant?.properties?.commandType?.enum;
    if (!Array.isArray(enumValues)) {
      continue;
    }

    for (const value of enumValues) {
      commandTypes.add(value);
    }
  }

  return commandTypes;
}

function isAnalyzeJobAnalysisRequest(parsedBody) {
  const commandTypes = suggestionProposalCommandTypes(parsedBody);
  return commandTypes.size === 2 && commandTypes.has('AddJobRequirement') && commandTypes.has('AddJobGap');
}

function buildMessagesResponse(result) {
  return {
    content: [{ type: 'text', text: JSON.stringify(result) }],
    stop_reason: 'end_turn',
    usage: { input_tokens: 42, output_tokens: 8 },
  };
}

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

  // Every request body is drained here; only the /v1/messages branch below ever parses these
  // bytes (structurally, to classify the request — see the module comment), and never logs them.
  const bodyBuffer = await readBody(req);

  console.log(`external-stub: ${method} ${path}`);

  if (method === 'GET' && path === '/__stub/unexpected') {
    return sendJson(res, 200, { count: unexpectedRequests.length, requests: unexpectedRequests });
  }

  if (method === 'POST' && path === '/v1/messages/count_tokens') {
    if (req.headers['x-api-key'] !== ANTHROPIC_API_KEY) {
      return sendJson(res, 401, { type: 'error', error: { type: 'authentication_error', message: 'invalid x-api-key' } });
    }

    return sendJson(res, 200, { input_tokens: 42 });
  }

  if (method === 'POST' && path === '/v1/messages') {
    if (req.headers['x-api-key'] !== ANTHROPIC_API_KEY) {
      return sendJson(res, 401, { type: 'error', error: { type: 'authentication_error', message: 'invalid x-api-key' } });
    }

    let parsedBody;
    try {
      parsedBody = JSON.parse(bodyBuffer.toString('utf8'));
    } catch {
      // Malformed JSON never crashes the stub — falls back to the same safe empty-output shape
      // as an unrecognised schema below.
      return sendJson(res, 200, buildMessagesResponse(EMPTY_OUTPUT_RESULT));
    }

    if (isAnalyzeJobAnalysisRequest(parsedBody)) {
      return sendJson(res, 200, buildMessagesResponse(ANALYZE_JOB_ANALYSIS_RESULT));
    }

    // Deterministic, structurally valid, empty-output response (mirrors the FakeAIProvider
    // "EmptyOutput" scenario) for every other real or hypothetical AnalyzeX command — enough for
    // foundation-level verification that the real adapter's request/response wiring works end to
    // end without needing a fixture per command.
    return sendJson(res, 200, buildMessagesResponse(EMPTY_OUTPUT_RESULT));
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
