---
status: accepted
date: 2026-08-21
---

# ADR-0027: The application security profile is S2

## Context

CommitAhead has a threat model (`docs/security/threat-model.md`) and a substantial set of implemented
controls: backend-mediated Supabase authentication, HttpOnly cookie sessions with a server-side
15-minute effective access-token lifetime, antiforgery on unsafe methods, default-deny authorization
with an enabled-user requirement, owner-scoped data with PostgreSQL RLS as defense in depth, a CSP
without `unsafe-inline` for `style-src`, secret scanning, dependency auditing, and locked restores.

What it did not have is a recorded assurance level. The canonical contract requires every deployable
application to select and record one risk-based profile — S1 Baseline, S2 Standard, or S3 High
assurance — because the profile determines verification depth and which requirements may be marked
not applicable. Without it, "secure enough" was an implicit judgement per change rather than a stated
target with evidence.

Against the contract's S2 triggers, this application matches several: users authenticate, it changes
durable user data, it processes personal data (a complete professional profile: contact details,
employment history, education), and it is intended to be internet-accessible once Phase 6c starts. Any
one of those is sufficient. It matches no S3 trigger: compromise would not cause severe financial,
safety, legal, or infrastructure harm, no binding requirement demands S3, and a single-user private CV
tool is not an unusually valuable target.

## Decision

CommitAhead is an **S2 Standard** application, targeting all applicable OWASP ASVS 5.0 Level 1 and
Level 2 requirements.

- The profile is recorded in `docs/engineering-context.md` and referenced from
  `docs/security/threat-model.md`.
- `docs/security/threat-model.md` gains an evidence register mapping each applicable control to its
  owner (code, configuration, infrastructure, or operation), its implementation, its verification
  evidence, and its status. Requirements whose primary owner is outside this repository remain listed
  with the external owner named, not silently dropped.
- Requirements are marked not applicable only when the relevant feature does not exist, with the
  versioned requirement identifier and rationale recorded. Password-storage requirements are the
  obvious case: identity is delegated entirely to Supabase and this application never receives a
  password.
- Changes to authentication, authorization, session handling, owner scoping, secrets, file handling,
  or outbound requests receive focused review, per the contract's S2 expectation.
- The profile is reviewed when exposure, data, identity, or business impact changes — in particular
  before Phase 6c internet deployment, and if the application ever serves users other than its owner
  in a shared-tenant sense.

## Consequences

- Verification depth is now stated rather than assumed. The contract's required API authorization
  matrix (no credential, malformed or wrong-audience credential, authenticated without permission,
  another owner's resource, authorized success) becomes mandatory per protected operation, which the
  current API tests only partially cover.
- An explicit endpoint-authorization inventory becomes mandatory, including the approved
  anonymous-endpoint list and a mechanical test enumerating MVC actions.
- Selecting S2 rather than S1 means the existing RLS and owner-scoping tests are necessary but not
  sufficient: cross-owner behaviour must also be proven through the public HTTP contract.
- Profile achievement cannot be claimed from a scanner result or from this ADR. It requires evidence
  for every applicable requirement at Level 1 and Level 2, which the evidence register tracks and
  which is not complete today.
- Selecting S2 rather than S3 means no independent security review or penetration test is required
  before a release. That is a recorded risk decision, revisited under the review triggers above.

## Considered Alternatives

**S1 Baseline.** Rejected: the contract permits S1 only when the application meets no S2 trigger, and
this one meets four. The single-user reality reduces impact today but does not change the triggers,
and the contract is explicit that S1 is a risk decision rather than permission to verify less.

**S3 High assurance.** Rejected: no severe-harm, regulatory, or high-value-target trigger applies. S3
would require independent review before material releases, which is disproportionate for a private,
invite-only CV tool with one user.

**Defer the decision until Phase 6c.** Rejected: the profile determines what is tested now, and
retrofitting Level 2 verification after the architecture migration would be more expensive than
building it in during the migration.
