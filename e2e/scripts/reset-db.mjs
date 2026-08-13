#!/usr/bin/env node
// CommitAhead E2E database reset — the ONLY executable reset path (E2E Foundation Plan;
// docs/testing/strategy.md §7.4). Exports resetDatabase() for the Playwright fixture and for
// verify-foundation.mjs, and backs `npm run db:reset` when run directly from the CLI — one code
// path, one set of guards, every time. Validates the exact Compose project, the running
// container's own label, the database name, and the forbidden legacy database name BEFORE opening
// psql or sending any SQL — a destructive reset must never be possible against the wrong stack.

import { spawn } from 'node:child_process';
import { readFile } from 'node:fs/promises';
import { fileURLToPath, pathToFileURL } from 'node:url';
import path from 'node:path';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..');
const composeFile = path.join(repoRoot, 'docker-compose.e2e.yml');
const resetSqlPath = path.join(__dirname, '..', 'support', 'reset.sql');

const COMPOSE_PROJECT = 'commitahead-e2e';
const DB_SERVICE = 'db';
const DB_NAME = 'commitahead_e2e';
const DB_ROLE = 'commitahead_migrator';
const FORBIDDEN_DB_NAMES = ['commitahead'];

function composeArgs(...args) {
  return ['compose', '-f', composeFile, '-p', COMPOSE_PROJECT, ...args];
}

function runCommand(command, args, { input } = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      stdio: [input !== undefined ? 'pipe' : 'ignore', 'pipe', 'pipe'],
    });

    let stdout = '';
    let stderr = '';
    child.stdout.on('data', (chunk) => (stdout += chunk));
    child.stderr.on('data', (chunk) => (stderr += chunk));
    child.on('error', reject);
    child.on('close', (code) => {
      if (code === 0) {
        resolve({ stdout, stderr });
      } else {
        reject(new Error(`'${command} ${args.join(' ')}' exited with code ${code}: ${stderr.trim()}`));
      }
    });

    if (input !== undefined) {
      child.stdin.write(input);
      child.stdin.end();
    }
  });
}

async function validateTarget() {
  if (FORBIDDEN_DB_NAMES.includes(DB_NAME)) {
    throw new Error(`Refusing to reset: target database name '${DB_NAME}' is the forbidden legacy database name.`);
  }

  const { stdout: idOutput } = await runCommand('docker', composeArgs('ps', '-q', DB_SERVICE));
  const containerId = idOutput.trim().split('\n')[0];
  if (!containerId) {
    throw new Error(
      `Refusing to reset: no running '${DB_SERVICE}' container found for Compose project '${COMPOSE_PROJECT}'. Is the E2E stack up ('npm run stack:up')?`,
    );
  }

  // A real inspection of the running container's own label — not an assumption based on this
  // script's own constants — proves the container actually belongs to the approved project.
  const { stdout: inspectOutput } = await runCommand('docker', [
    'inspect',
    '--format',
    '{{.State.Running}}|{{index .Config.Labels "com.docker.compose.project"}}',
    containerId,
  ]);
  const [runningFlag, actualProject] = inspectOutput.trim().split('|');

  if (runningFlag !== 'true') {
    throw new Error(`Refusing to reset: '${DB_SERVICE}' container is not running.`);
  }

  if (actualProject !== COMPOSE_PROJECT) {
    throw new Error(
      `Refusing to reset: the running '${DB_SERVICE}' container is labelled as Compose project '${actualProject}', not the approved '${COMPOSE_PROJECT}'.`,
    );
  }
}

export async function resetDatabase() {
  await validateTarget();

  const sql = await readFile(resetSqlPath, 'utf8');

  await runCommand(
    'docker',
    composeArgs('exec', '-T', DB_SERVICE, 'psql', '-v', 'ON_ERROR_STOP=1', '-U', DB_ROLE, '-d', DB_NAME),
    { input: sql },
  );
}

async function main() {
  await resetDatabase();
  console.log('reset-db: database reset complete.');
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) {
  main().catch((error) => {
    console.error(`reset-db: ${error.message}`);
    process.exitCode = 1;
  });
}
