# CommitAhead — E2E Runbook

Operational guide for the Playwright end-to-end suite: how to install it, bring the stack up, run
the journeys, reset data, and read the artifacts when something fails.

> **Status: foundation implemented; journeys not written yet.** The Docker stack, the E2E-only
> auth endpoint, the reset path, the orchestration scripts, and the Playwright/fixture skeleton all
> exist and are verified (`npm run verify:foundation`). No journey spec files exist yet — that is
> the next slice, tracked separately in `docs/roadmap.md`.

**Read [`docs/testing/strategy.md`](../docs/testing/strategy.md) Layer 7 first.** It is the
normative contract — journeys, isolation, auth, locators, and the rules about what E2E may and may
not touch. This file only tells you how to operate it. Where the two disagree, the strategy wins.

**When you need this at all:** the E2E stack is started **only for explicit E2E work** — writing or
debugging a journey, or a post-merge/manual verification run. Ordinary PRs do not execute
Playwright, and normal feature development never needs this stack running.

---

## Layout

```
CommitAhead/
├── docker-compose.e2e.yml          ← isolated app + PostgreSQL + external-stub, one proxy in front
└── e2e/
    ├── package.json                ← Playwright/TypeScript deps, separate from frontend/
    ├── package-lock.json
    ├── tsconfig.json
    ├── playwright.config.ts        ← Playwright execution config only
    ├── README.md                   ← this file
    ├── scripts/
    │   ├── run-full.mjs            ← up → wait for health → test → always attempt down -v
    │   ├── reset-db.mjs            ← the only executable reset path (npm run db:reset)
    │   └── verify-foundation.mjs   ← foundation checks — no journeys needed to run these
    ├── support/
    │   ├── reset.sql                ← the SQL only; never drops migrations or RLS
    │   ├── db-init/                 ← one-shot: roles → EF migration bundle → RLS
    │   ├── external-stub/           ← deterministic local Anthropic + Supabase Auth stub
    │   └── proxy/                   ← the only host-facing service's nginx config
    └── tests/
        ├── fixtures/
        │   └── e2e-test.ts         ← resetDb (auto) → e2eSession (lazy) → authenticatedPage (lazy)
        └── journeys/
            ├── 001-authenticated-access.spec.ts   (not written yet)
            ├── 002-study-queue-ranking.spec.ts     (not written yet)
            ├── 003-job-analysis-draft.spec.ts      (not written yet)
            └── 004-cv-presentation-export.spec.ts  (not written yet)
```

Full per-file responsibilities are in `docs/testing/strategy.md` §7.11. In short: the Compose file
owns the topology, `playwright.config.ts` owns execution settings only, `e2e-test.ts` owns
reset-then-authenticate, `reset.sql` owns the SQL, `reset-db.mjs` owns validating the target and
executing that SQL, `run-full.mjs` owns the stack lifecycle, and `verify-foundation.mjs` owns the
foundation-level checks below.

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
| DB host port | `5433` | `127.0.0.1:5434` | **none — `db` is internal-only** |
| App host port | — | `127.0.0.1:8080` | **none — only `proxy` publishes `127.0.0.1:8081`** |
| Data | persistent volume | persistent volume | **`tmpfs` — gone on `down`, or a crash** |

**`proxy` is the only host-facing service.** `app`, `db`, `db-init`, and `external-stub` sit on an
`internal: true` Compose network with no route off it — verified empirically (an internal-only
service's `ports:` entry is silently ignored; `docker port` shows nothing for it at all), not
merely configured. `proxy` is a plain nginx container dual-homed onto that network and an ordinary
bridge network, forwarding only to `app`.

**Rules:**

1. **Never point E2E at port 8080.** That is the local production-like stack from ADR-0021, and it
   holds data you actually care about. The Playwright config is required to fail fast rather than
   run against it — if you find yourself editing `baseURL` to make something work, stop.
2. **Never run the reset command without going through `npm run db:reset` / the fixture.**
   `reset-db.mjs` validates the running container's own Compose-project label and the database name
   before sending any SQL — a hand-rolled `docker compose exec … psql` reset has none of those
   guards.
3. **The E2E database is named `commitahead_e2e`, not `commitahead`.** If a command you are about to
   run mentions `commitahead` without the suffix, it is aimed at the wrong database.
4. **Back up before troubleshooting near the production-like stack.** If you need real data intact,
   `backend/scripts/backup-production-db.ps1` first — see the main [README](../README.md).
5. **There is no auth state file to protect.** Sessions are minted per test by a test-scoped
   fixture and kept in memory (`docs/testing/strategy.md` §7.3) — no `e2e/.auth/`, no
   `storageState` on disk. If you find yourself adding one, you are leaving the contract and
   creating a committable session cookie.

---

## Prerequisites

- Docker Desktop running (`docker info` should respond).
- Node.js per [`frontend/.nvmrc`](../frontend/.nvmrc) (currently 24).
- **No host .NET SDK needed for E2E** — the EF migration bundle is built entirely inside the
  `db-init` image. The SDK requirement in the main README is for local dev/production-like work,
  not this stack.

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

## The usual case: one command

`run-full.mjs` brings the stack up (build included), waits for health, runs Playwright, and
**always attempts `docker compose down -v`** — in a `finally`, and again on `Ctrl-C`/`SIGTERM`. It
cannot guarantee cleanup after `SIGKILL`, a Docker daemon crash, or a host failure; if a stack is
left behind after one of those, run `npm run stack:down` by hand.

```bash
npm run e2e:full
```

Use this for verification runs and for anything unattended. Use the manual steps below when you
are iterating and want the stack to stay up between runs.

---

## Bringing the stack up manually

Builds the production image plus `db-init`/`external-stub`, starts everything, and runs `db-init`
(roles → EF migration bundle → RLS) before `app` is allowed to start
(`depends_on: db-init: condition: service_completed_successfully`):

```bash
npm run stack:up
```

Wait for health:

```bash
docker compose -f ../docker-compose.e2e.yml -p commitahead-e2e ps
```

`db` and `app` should report `healthy`; `proxy` has no healthcheck of its own and should report
`Up` (it depends on `app`'s health, not the other way around); `db-init` should show `Exited (0)`.
The app is then reachable at <http://localhost:8081> — through `proxy`, which is the only service
with a published port.

Tear down — `db`'s data directory is `tmpfs`, so this (and any crash) discards all E2E data by
design:

```bash
npm run stack:down
```

Playwright does **not** start or stop this stack for you (no `webServer` config). That is
deliberate: the reset path has to address the same Compose project, and `webServer` shutdown would
kill the process group without running `down`.

---

## Verifying the foundation (no journeys required)

Checks health through the proxy, the session/refresh/logout round trip against `external-stub`,
reset idempotence with migrations/RLS surviving it, that the stub recorded zero unexpected
requests, that `app` cannot reach the real internet, and that only `proxy` publishes a host port:

```bash
npm run stack:up
npm run verify:foundation
```

This is the right first thing to run after touching anything under `docker-compose.e2e.yml`,
`e2e/support/`, or the E2E-only backend code (`E2ESessionController`, `E2EConfigurationGuard`,
`AnthropicBaseAddress`) — it needs no journey spec to exist.

---

## Running the journeys

All commands run from `e2e/`. (No journey spec files exist yet — these commands are documented for
when they do.)

```bash
npm test
```

Runs all four journeys serially (`workers: 1`).

**Headed** — watch a real browser window:

```bash
npm run test:headed
```

**UI mode** — the interactive runner with a timeline, per-action snapshots, watch mode, and a
locator picker. Best first stop when a journey fails:

```bash
npm run test:ui
```

UI Mode needs **no manual authentication step**. Playwright normally skips `setup` projects in UI
Mode, which is why suites built on a saved `storageState` require you to re-run auth by hand — this
suite has no setup project and no state file, so each test mints its own session exactly as a
terminal run does.

**Debug mode** — Playwright Inspector, headed, with test timeouts disabled so you can step:

```bash
npx playwright test --debug
```

Add `await page.pause()` in a test to break at a specific line.

### Running one journey

```bash
npm run test:journey -- 003-job-analysis-draft.spec.ts
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
only — they keep the files in a readable order and carry no execution-order dependency. If a
journey only passes as part of the full run, that is a defect in the journey, not a way to run the
suite.

### Exploratory tooling

Playwright's Agent CLI and similar generative tools are **optional local aids** — useful for poking
at a stuck selector or exploring a page. Nothing they produce is committed as generated: a journey
enters the suite only as reviewed `@playwright/test` code meeting the Layer 7 contract.
`@playwright/test` is the permanent automated suite, and no agent tool is ever a CI dependency.

---

## Resetting the database

Every test resets automatically before it runs (the fixture's `resetDb`, automatic and test-scoped
— docs/testing/strategy.md §7.3/§7.4). Run it by hand when you have been poking at the app and want
a clean slate:

```bash
npm run db:reset
```

That is the **only** reset command. It runs `scripts/reset-db.mjs`, which inspects the running
`db` container's own Compose-project label (not merely its own constants) to confirm it is
`commitahead-e2e`, confirms the target database is `commitahead_e2e`, and only then pipes
`support/reset.sql` to `psql` over stdin, connected as `commitahead_migrator` — the table owner,
which holds `TRUNCATE`. The Playwright fixture calls the same module's `resetDatabase()`, so
operator and suite share one code path with one set of guards.

Do **not** hand-roll a `docker compose exec … psql` reset. A second path is a second chance to
target the wrong database, and it is the one nobody remembers to add the guards to.

The reset **truncates business tables and re-seeds the E2E user**. It does not drop the schema or
the database — RLS policies and the EF migrations-history table must survive, or later tests would
run unprotected or unmigrated and still appear to pass. Nothing else seeds that user: `db-init`
applies roles/migrations/RLS only, deliberately no data, so a stack that has never been reset has
no enabled `User` row to authenticate against.

If you need a genuinely clean rebuild (schema included):

```bash
npm run stack:down
npm run stack:up
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

`test-results/`, `playwright-report/`, and `blob-report/` are build output and are gitignored.
There is no auth-state directory to exclude — sessions never touch disk.

---

## Troubleshooting

**Every request 401s partway through a run, or only in the last test.** Symptom of the 15-minute
access-token cap (`docs/testing/strategy.md` §7.3): the API rejects tokens older than 15 minutes
against their `iat` claim, regardless of cookie lifetime. Under this contract each test mints its
own fresh session, so seeing this means a session is being shared across tests — check that the
`e2eSession`/`authenticatedPage` fixtures are still test-scoped and have not been widened to worker
scope or replaced by a saved state file.

**`POST /auth/e2e/session` returns 404.** Either `ASPNETCORE_ENVIRONMENT` on the `app` container is
not `E2E`, or you are pointed at the wrong stack (8080 is the local production-like stack, which
has no such endpoint at all). Check `docker compose -f ../docker-compose.e2e.yml -p commitahead-e2e
exec app printenv ASPNETCORE_ENVIRONMENT`.

**The `app` container refuses to start with a configuration error.** That is the intended
fail-closed guard (`E2EConfigurationGuard`, called from `Program.cs` before the pipeline is built):
it throws if any `E2E:*` value is missing inside `E2E`, or if `Supabase:Url`/`Supabase:AnonKey`/
`Auth:CallbackUrl`/the Anthropic base address or key differ from their exact approved sentinel
values. Check the container's logs for exactly which check failed — the message names the
offending key.

**A test 401s or can't find its data when run alone.** The reset→authenticate ordering is broken.
`resetDb` must run first and `e2eSession` must depend on it — a real fixture dependency
(destructured parameter), not two hooks that happen to run in a convenient order.

**Tests pass alone but fail together.** Almost always leaked state. Confirm the reset actually ran,
and that no test depends on another's data. Do not "fix" it by raising `workers` or adding a sleep.

**A test only passes when you add a wait.** The wait is hiding a race. Fixed sleeps are banned;
replace it with a web-first assertion on the thing you were really waiting for (an element becoming
visible, text changing) so Playwright retries properly.

**Journey 3 hangs or fails at the Analyze step.** `external-stub` is not reachable, or its
credentials don't match. E2E runs the real `AnthropicAIProvider` pointed at `external-stub` — not
`FakeAIProvider` (`docs/testing/strategy.md` §7.6) — so check the stub container is up and that
`AI__Providers__Anthropic__BaseUrl`/`ApiKey` on `app` match `external-stub`'s
`ANTHROPIC_API_KEY_SENTINEL`. It must never point at a real provider; `app` has no route to the
public internet by design — confirm with `npm run verify:foundation`.

**`GET /__stub/unexpected` on `external-stub` shows a nonzero count.** Something called an endpoint
the stub does not support. It responds `501` and records the method/path — read that response to
see which call needs to be added to the stub or fixed in the caller; the stub is deliberately
narrow (exactly four endpoints), not a general-purpose mock.

**Chromium crashes or dies mid-run in Docker.** Missing `--ipc=host` on the container running
Playwright itself (not the E2E stack, which runs no browser). Playwright documents this as a cause
of Chromium running out of memory in containers.

**Port already in use on 8081.** Another E2E stack is still up. `npm run stack:down` first — do not
switch to 8080 to get around it. If this happens after `npm run e2e:full`, its teardown is not
working; fix that rather than tearing down by hand each time.

**`docker compose exec` intermittently fails on Windows with `NativeCommandError`.** Known
PowerShell 5.1 behaviour with Compose's stderr warnings — pass `--env-file` consistently if you add
one; this stack currently needs no env file since every value is a fixed non-secret sentinel.

**The app is healthy but the page is blank.** The SPA build was not copied into the image. Rebuild
with `npm run stack:up` (which passes `--build`); a published image without `frontend/dist` should
fail the build, not serve an empty shell.

---

## Related documentation

| Document | What it covers |
|---|---|
| [`docs/testing/strategy.md`](../docs/testing/strategy.md) | Layer 7 — the normative E2E contract; all other test layers |
| [`README.md`](../README.md) | Local dev and the production-like Docker stack (ADR-0021) |
| [`docs/adr/0021-production-hardening-starts-with-local-docker.md`](../docs/adr/0021-production-hardening-starts-with-local-docker.md) | Why the local Docker stack exists and how it is isolated |
| [`docs/security/threat-model.md`](../docs/security/threat-model.md) | Security controls journey 1 exercises |
