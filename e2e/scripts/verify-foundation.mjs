#!/usr/bin/env node
// CommitAhead E2E foundation verifier (E2E Foundation Plan). Checks the stack invariants that
// matter before any journey spec exists: health, the E2E session/refresh/logout round trip
// against the external stub, reset idempotence with migrations/RLS surviving it, network
// isolation, and that only `proxy` publishes a host port. Assumes the stack is already up
// (`npm run stack:up`) — this script owns no lifecycle of its own, and may call resetDatabase()
// only to prove it is idempotent, never as a substitute for the fixture's own per-test reset.

import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import { resetDatabase } from './reset-db.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..');
const composeFile = path.join(repoRoot, 'docker-compose.e2e.yml');
const COMPOSE_PROJECT = 'commitahead-e2e';
const BASE_URL = 'http://localhost:8081';

function composeArgs(...args) {
  return ['compose', '-f', composeFile, '-p', COMPOSE_PROJECT, ...args];
}

function run(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args);
    let stdout = '';
    let stderr = '';
    child.stdout.on('data', (chunk) => (stdout += chunk));
    child.stderr.on('data', (chunk) => (stderr += chunk));
    child.on('error', reject);
    child.on('close', (code) => resolve({ code, stdout, stderr }));
  });
}

async function execInService(service, ...args) {
  return run('docker', composeArgs('exec', '-T', service, ...args));
}

async function containerIdFor(service) {
  const { stdout } = await run('docker', composeArgs('ps', '-q', service));
  return stdout.trim().split('\n')[0] ?? '';
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

function parseSetCookies(response) {
  const raw = typeof response.headers.getSetCookie === 'function' ? response.headers.getSetCookie() : [];
  return raw.map((c) => c.split(';')[0]).join('; ');
}

const results = [];
async function check(name, fn) {
  try {
    await fn();
    results.push({ name, ok: true });
    console.log(`PASS  ${name}`);
  } catch (error) {
    results.push({ name, ok: false, error: error.message });
    console.log(`FAIL  ${name}: ${error.message}`);
  }
}

async function main() {
  await check('app /api/health responds healthy through the proxy', async () => {
    const response = await fetch(`${BASE_URL}/api/health`);
    assert(response.ok, `expected 2xx, got ${response.status}`);
    const body = await response.text();
    assert(body.toLowerCase().includes('healthy'), `health body did not report healthy: ${body}`);
  });

  await check('the SPA shell is served through the proxy', async () => {
    const response = await fetch(`${BASE_URL}/`);
    assert(response.ok, `expected 2xx, got ${response.status}`);
    const body = await response.text();
    assert(body.toLowerCase().includes('<!doctype html>'), 'response did not look like the SPA shell');
  });

  // db-init deliberately never seeds any row — only reset.sql does (docs/testing/strategy.md
  // §7.4) — so the E2E user must be seeded before any auth check below can succeed.
  await check('resetDatabase() seeds the E2E user', async () => {
    await resetDatabase();
  });

  let sessionCookieHeader = '';

  await check('POST /auth/e2e/session mints a session that authorizes /api/me', async () => {
    const sessionResponse = await fetch(`${BASE_URL}/auth/e2e/session`, { method: 'POST' });
    assert(sessionResponse.status === 204, `expected 204, got ${sessionResponse.status}`);

    sessionCookieHeader = parseSetCookies(sessionResponse);
    assert(sessionCookieHeader.length > 0, 'no Set-Cookie header on the session response');

    const meResponse = await fetch(`${BASE_URL}/api/me`, { headers: { Cookie: sessionCookieHeader } });
    assert(meResponse.status === 200, `expected 200 from /api/me, got ${meResponse.status}`);
  });

  await check('refresh against the external stub succeeds', async () => {
    const csrfResponse = await fetch(`${BASE_URL}/auth/csrf`, { headers: { Cookie: sessionCookieHeader } });
    assert(csrfResponse.status === 200, `expected 200 from /auth/csrf, got ${csrfResponse.status}`);
    const { token } = await csrfResponse.json();
    const cookieHeader = [sessionCookieHeader, parseSetCookies(csrfResponse)].filter(Boolean).join('; ');

    const refreshResponse = await fetch(`${BASE_URL}/auth/refresh`, {
      method: 'POST',
      headers: { Cookie: cookieHeader, 'X-CSRF-TOKEN': token },
    });
    assert(refreshResponse.status === 204, `expected 204 from /auth/refresh, got ${refreshResponse.status}`);

    sessionCookieHeader = [sessionCookieHeader, parseSetCookies(refreshResponse)].filter(Boolean).join('; ');
  });

  await check('logout against the external stub succeeds', async () => {
    const csrfResponse = await fetch(`${BASE_URL}/auth/csrf`, { headers: { Cookie: sessionCookieHeader } });
    assert(csrfResponse.status === 200, `expected 200 from /auth/csrf, got ${csrfResponse.status}`);
    const { token } = await csrfResponse.json();
    const cookieHeader = [sessionCookieHeader, parseSetCookies(csrfResponse)].filter(Boolean).join('; ');

    const logoutResponse = await fetch(`${BASE_URL}/auth/logout`, {
      method: 'POST',
      headers: { Cookie: cookieHeader, 'X-CSRF-TOKEN': token },
    });
    assert(logoutResponse.status === 204, `expected 204 from /auth/logout, got ${logoutResponse.status}`);
  });

  await check('resetDatabase() is idempotent across two consecutive runs', async () => {
    await resetDatabase();
    await resetDatabase();
  });

  await check('__EFMigrationsHistory and RLS on professional_profiles survive a reset', async () => {
    const historyCheck = await execInService(
      'db', 'psql', '-U', 'postgres', '-d', 'commitahead_e2e', '-tAc',
      `SELECT to_regclass('public."__EFMigrationsHistory"') IS NOT NULL;`,
    );
    assert(historyCheck.code === 0, `psql failed: ${historyCheck.stderr}`);
    assert(historyCheck.stdout.trim() === 't', '__EFMigrationsHistory table is missing after reset');

    const policyCheck = await execInService(
      'db', 'psql', '-U', 'postgres', '-d', 'commitahead_e2e', '-tAc',
      `SELECT count(*) FROM pg_policies WHERE tablename = 'professional_profiles';`,
    );
    assert(policyCheck.code === 0, `psql failed: ${policyCheck.stderr}`);
    assert(Number(policyCheck.stdout.trim()) > 0, 'RLS policy on professional_profiles is missing after reset');
  });

  await check('the external stub recorded zero unexpected requests', async () => {
    const { code, stdout, stderr } = await execInService('app', 'curl', '-s', 'http://external-stub:8080/__stub/unexpected');
    assert(code === 0, `curl failed: ${stderr}`);
    const body = JSON.parse(stdout);
    assert(body.count === 0, `external stub recorded ${body.count} unexpected request(s): ${JSON.stringify(body.requests)}`);
  });

  await check('app cannot reach the public internet by IP (1.1.1.1)', async () => {
    const { code } = await execInService('app', 'curl', '--max-time', '5', '-s', '-o', '/dev/null', 'http://1.1.1.1/');
    assert(code !== 0, 'app reached 1.1.1.1 — the E2E network is not isolated as configured');
  });

  await check('only proxy publishes a host port', async () => {
    for (const service of ['app', 'db', 'external-stub']) {
      const id = await containerIdFor(service);
      if (!id) {
        continue;
      }
      const { stdout } = await run('docker', ['port', id]);
      assert(stdout.trim() === '', `service '${service}' publishes a host port: ${stdout.trim()}`);
    }

    const proxyId = await containerIdFor('proxy');
    assert(proxyId, 'proxy container not found');
    const { stdout } = await run('docker', ['port', proxyId]);
    assert(stdout.includes('8081'), `proxy does not publish port 8081: ${stdout.trim()}`);
  });

  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} checks passed.`);
  if (failed.length > 0) {
    process.exitCode = 1;
  }
}

main().catch((error) => {
  console.error(`verify-foundation: ${error.message}`);
  process.exitCode = 1;
});
