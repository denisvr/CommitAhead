# CommitAhead — E2E Runbook

Operational guide for the Playwright end-to-end suite: how to install it, bring the stack up, run
the journeys, reset data, and read the artifacts when something fails.

> **Status: not implemented yet.** No Playwright project, no `docker-compose.e2e.yml`, and no E2E
> auth hook exist in this repo today. This file documents the commands the implementation is
> expected to provide, so that the runbook and the contract are agreed before any code is written.
> Commands below will not work until that implementation lands.

**Read [`docs/testing/strategy.md`](../docs/testing/strategy.md) Layer 7 first.** It is the
normative contract — journeys, isolation, auth, locators, and the rules about what E2E may and may
not touch. This file only tells you how to operate it. Where the two disagree, the strategy wins.

---

## Safeguards — read before your first run

E2E destroys data. Every command here truncates or discards a database. The suite is designed so
that it *cannot* reach your real data, and those protections are not optional.

**The E2E stack is separate from every other stack on your machine:**

| | Dev | Local production-like | **E2E** |
|---|---|---|---|
| Compose project | (directory default) | `commitahead-prod` | **`commitahead-e2e`** |
| Compose file | `backend/docker-compose.yml` | `docker-compose.prod.yml` | **`docker-compose.e2e.yml`** |
| Database | `commitahead` | `commitahead` | **`commitahead_e2e`** |
| DB port | `5433` | `127.0.0.1:5434` | **`127.0.0.1:5435`** |
| App port | — | `127.0.0.1:8080` | **`127.0.0.1:8081`** |
| Data | persistent volume | persistent volume | **no volume — gone on `down`** |

**Rules:**

1. **Never point E2E at port 8080 or 5434.** That is the local production-like stack from ADR-0021,
   and it holds data you actually care about. The Playwright config is required to fail fast rather
   than run against it — if you find yourself editing `baseURL` to make something work, stop.
2. **Never run the reset command without `-f docker-compose.e2e.yml -p commitahead-e2e`.** Both
   flags, every time. A reset that relies on your ambient Docker context can hit the wrong stack.
3. **The E2E database is named `commitahead_e2e`, not `commitahead`.** If a command you are about to
   run mentions `commitahead` without the suffix, it is aimed at the wrong database.
4. **Back up before troubleshooting near the production-like stack.** If you need real data intact,
   `backend/scripts/backup-production-db.ps1` first — see the main [README](../README.md).
5. **There is no auth state file to protect.** Sessions are minted per journey by a test-scoped
   fixture and kept in memory (`docs/testing/strategy.md` §7.3) — no `e2e/.auth/`, no
   `storageState` on disk. If you find yourself adding one, you are leaving the contract and
   creating a committable session cookie.

---

## Prerequisites

- Docker Desktop running (`docker info` should respond).
- Node.js per [`frontend/.nvmrc`](../frontend/.nvmrc) (currently 24).
- .NET SDK per [`backend/global.json`](../backend/global.json) — needed to apply migrations.

## Installation

The E2E suite has its own `package.json`, separate from `frontend/`, so Playwright never enters the
application's dependency tree.

```bash
cd e2e && npm ci
```

Install the browser. Chromium only — the suite does not run Firefox or WebKit
(`docs/testing/strategy.md` §7.7):

```bash
cd e2e && npx playwright install --with-deps chromium
```

On Windows, `--with-deps` is a no-op for OS packages; plain `npx playwright install chromium` is
equivalent there. Do not cache browser binaries in CI — Playwright's own guidance is that restoring
the cache costs about as much as downloading them.

---

## Bringing the stack up

One command builds the production image, starts PostgreSQL and the app, applies migrations and all
RLS scripts, and seeds the E2E user:

```bash
docker compose -f docker-compose.e2e.yml -p commitahead-e2e up -d --build
```

Wait for health before running tests:

```bash
docker compose -f docker-compose.e2e.yml -p commitahead-e2e ps
```

Both services should report `healthy`. The app is then at <http://localhost:8081>.

Tear down — and because the E2E database has no named volume, this discards all E2E data by design:

```bash
docker compose -f docker-compose.e2e.yml -p commitahead-e2e down -v
```

Playwright does **not** start or stop this stack for you (no `webServer` config). That is
deliberate: the reset helper has to address the same Compose project, and `webServer` shutdown
would kill the process group without running `down`.

---

## Running the journeys

All commands run from `e2e/`.

```bash
npx playwright test
```

Runs all four journeys serially (`workers: 1`), in filename order.

**Headed** — watch a real browser window:

```bash
npx playwright test --headed
```

**UI mode** — the interactive runner with a timeline, per-action snapshots, watch mode, and a
locator picker. Best first stop when a journey fails:

```bash
npx playwright test --ui
```

UI Mode needs **no manual authentication step**. Playwright normally skips `setup` projects in UI
Mode, which is why suites built on a saved `storageState` require you to re-run auth by hand — this
suite has no setup project and no state file, so each run mints its own session exactly as a
terminal run does.

**Debug mode** — Playwright Inspector, headed, with test timeouts disabled so you can step:

```bash
npx playwright test --debug
```

Add `await page.pause()` in a test to break at a specific line.

### Running one journey

By file:

```bash
npx playwright test 003-job-analysis-draft.spec.ts
```

By file and line, straight into the debugger:

```bash
npx playwright test 003-job-analysis-draft.spec.ts:42 --debug
```

By title (regex):

```bash
npx playwright test -g "applies accepted proposals"
```

Re-run only what failed last time:

```bash
npx playwright test --last-failed
```

Each journey must pass **on its own and in any order**. The `001`–`004` prefixes are organizational
only — they keep the files in a readable order and carry no dependency. If a journey only passes as
part of the full run, that is a defect in the journey, not a way to run the suite.

---

## Resetting the database

Every journey starts from a known state, and the suite resets automatically before each journey
file. Run it by hand when you have been poking at the app and want a clean slate:

```bash
docker compose -f docker-compose.e2e.yml -p commitahead-e2e exec -T db \
  psql -U postgres -d commitahead_e2e -f /e2e/reset.sql
```

The reset **truncates business tables and re-seeds the E2E user**. It does not drop the schema or
the database — RLS policies and the EF migrations-history table must survive, or later journeys
would run unprotected or unmigrated and still appear to pass.

Reset always runs **before** authentication: the authenticated fixture depends on it, so the E2E
user row is freshly seeded before any session is minted against it.

If you need a genuinely clean rebuild (schema included), tear the stack down and bring it back up:

```bash
docker compose -f docker-compose.e2e.yml -p commitahead-e2e down -v
docker compose -f docker-compose.e2e.yml -p commitahead-e2e up -d --build
```

---

## Reports and failure artifacts

Artifacts are produced according to `docs/testing/strategy.md` §7.7: trace on the first retry,
screenshot only on failure, video retained on failure. A passing run leaves almost nothing behind.

**HTML report** (opens the last run's results):

```bash
npx playwright show-report
```

**Trace** — the highest-value artifact. Action-by-action timeline, DOM snapshots before and after
each step, network, and console:

```bash
npx playwright show-trace test-results/<path-to>/trace.zip
```

Traces exist only for retried tests. To force one while investigating locally:

```bash
npx playwright test 002-study-queue-ranking.spec.ts --trace on
```

**Screenshots and videos** land alongside the failing test's folder under `test-results/`. The
video covers the whole test, which is often faster than a trace for "what did the page look like
when it broke".

**Downloads** (journey 4's exported PDF) live in a temporary directory that Playwright deletes when
the browser context closes. To keep one for inspection, save it explicitly inside the test with
`download.saveAs(...)` — do not rely on `download.path()` surviving the run.

`test-results/` and `playwright-report/` are build output and do not belong in git. There is no
auth-state directory to exclude — sessions never touch disk.

---

## Troubleshooting

**Every request 401s partway through a run, or only in the last journey.** Symptom of the
15-minute access-token cap (`docs/testing/strategy.md` §7.3): the API rejects tokens older than 15
minutes against their `iat` claim, regardless of cookie lifetime. Under this contract each journey
mints its own fresh session, so seeing this means a session is being shared across journeys —
check that the authenticated fixture is test-scoped and has not been widened to worker scope or
replaced by a saved state file.

**401s immediately, from the first test.** The E2E auth hook is not enabled. Check that the app
container has `ASPNETCORE_ENVIRONMENT=E2E` and the E2E signing key configured. If the container
refuses to start altogether, that is the intended fail-closed guard: the signing key is set while
the environment is *not* `E2E`.

**A journey 401s or can't find its data when run alone.** The reset→authenticate ordering is
broken. Reset must run first and the session must be minted after it, expressed as a fixture
dependency rather than two independent hooks.

**Tests pass alone but fail together.** Almost always leaked state. Confirm the reset actually ran
between journeys, and that no journey depends on another's data. Do not "fix" it by raising
`workers` or adding a sleep.

**A test only passes when you add a wait.** The wait is hiding a race. Fixed sleeps are banned;
replace it with a web-first assertion on the thing you were really waiting for (an element becoming
visible, text changing) so Playwright retries properly.

**Journey 3 hangs or fails at the Analyze step.** The local AI stub is not reachable. E2E runs the
real `AnthropicAIProvider` pointed at a deterministic stub inside the stack — not `FakeAIProvider`
(`docs/testing/strategy.md` §7.6) — so check the stub service is up and the adapter's configured
base address points at it. It must never point at a real provider; the app container has no route
to the public internet by design.

**Chromium crashes or dies mid-run in Docker.** Missing `--ipc=host`. Playwright documents this as
a cause of Chromium running out of memory in containers.

**Port already in use on 8081 or 5435.** Another E2E stack is still up. `down -v` it first — do not
switch to 8080 or 5434 to get around it.

**`docker compose exec` intermittently fails on Windows with `NativeCommandError`.** Known
PowerShell 5.1 behaviour with Compose's stderr warnings — pass `--env-file` consistently, the same
fix applied to the scripts in `backend/scripts/`.

**The app is healthy but the page is blank.** The SPA build was not copied into the image. Rebuild
with `--build`; a published image without `frontend/dist` should fail the build, not serve an empty
shell.

---

## Related documentation

| Document | What it covers |
|---|---|
| [`docs/testing/strategy.md`](../docs/testing/strategy.md) | Layer 7 — the normative E2E contract; all other test layers |
| [`README.md`](../README.md) | Local dev and the production-like Docker stack (ADR-0021) |
| [`docs/adr/0021-production-hardening-starts-with-local-docker.md`](../docs/adr/0021-production-hardening-starts-with-local-docker.md) | Why the local Docker stack exists and how it is isolated |
| [`docs/security/threat-model.md`](../docs/security/threat-model.md) | Security controls journey 1 exercises |
