#!/usr/bin/env node
// CommitAhead E2E — one-command full lifecycle: bring the stack up, wait for health, run
// Playwright, and ALWAYS attempt `docker compose down -v` afterward — on success, on failure, on
// Ctrl-C (SIGINT), and on SIGTERM. Contains NO reset logic of its own: reset-db.mjs is the only
// executable reset path, called by the Playwright fixture per test, never re-implemented here.
//
// This script cannot guarantee cleanup after SIGKILL, a Docker daemon crash, or a host machine
// failure — those bypass any Node-level signal handler or `finally` block entirely. If a stack is
// left behind after one of those, the operator's fallback is `npm run stack:down`.

import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const e2eDir = path.join(__dirname, '..');
const repoRoot = path.resolve(__dirname, '..', '..');
const composeFile = path.join(repoRoot, 'docker-compose.e2e.yml');

const COMPOSE_PROJECT = 'commitahead-e2e';
const HEALTH_URL = 'http://localhost:8081/api/health';
const HEALTH_TIMEOUT_MS = 120_000;
const HEALTH_POLL_INTERVAL_MS = 2_000;

function composeArgs(...args) {
  return ['compose', '-f', composeFile, '-p', COMPOSE_PROJECT, ...args];
}

function run(command, args, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { stdio: 'inherit', ...options });
    child.on('error', reject);
    child.on('close', (code) => (code === 0 ? resolve() : reject(new Error(`'${command} ${args.join(' ')}' exited with code ${code}`))));
  });
}

async function waitForHealth() {
  const deadline = Date.now() + HEALTH_TIMEOUT_MS;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(HEALTH_URL);
      if (response.ok) {
        return;
      }
    } catch {
      // Not up yet — keep polling until the deadline.
    }

    await new Promise((resolve) => setTimeout(resolve, HEALTH_POLL_INTERVAL_MS));
  }

  throw new Error(`Timed out waiting for ${HEALTH_URL} to become healthy after ${HEALTH_TIMEOUT_MS}ms.`);
}

let tornDown = false;
async function tearDown() {
  if (tornDown) {
    return;
  }
  tornDown = true;

  try {
    await run('docker', composeArgs('down', '-v'));
  } catch (error) {
    console.error(`run-full: teardown failed — ${error.message}. Run 'npm run stack:down' manually.`);
  }
}

function installSignalHandlers() {
  let handling = false;
  for (const signal of ['SIGINT', 'SIGTERM']) {
    process.on(signal, async () => {
      if (handling) {
        return;
      }
      handling = true;
      console.error(`run-full: received ${signal} — tearing down before exit.`);
      await tearDown();
      process.exit(1);
    });
  }
}

async function main() {
  installSignalHandlers();

  let playwrightFailed = false;
  try {
    console.log('run-full: bringing the E2E stack up (build + start)...');
    await run('docker', composeArgs('up', '-d', '--build'));

    console.log('run-full: waiting for /api/health...');
    await waitForHealth();

    console.log('run-full: running Playwright...');
    try {
      // `npx` resolves to npx.cmd on Windows — even naming the .cmd explicitly, Node's spawn()
      // cannot exec a Windows batch file without a shell (fails with EINVAL). shell: true is safe
      // here: every argument is a static literal, never user input.
      await run('npx', ['playwright', 'test'], { cwd: e2eDir, shell: process.platform === 'win32' });
    } catch (error) {
      console.error(`run-full: Playwright run failed — ${error.message}`);
      playwrightFailed = true;
    }
  } finally {
    console.log('run-full: tearing down (down -v)...');
    await tearDown();
  }

  process.exitCode = playwrightFailed ? 1 : 0;
}

main().catch((error) => {
  console.error(`run-full: ${error.message}`);
  process.exitCode = 1;
});
