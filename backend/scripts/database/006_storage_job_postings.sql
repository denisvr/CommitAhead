-- CommitAhead — Supabase Storage bucket and RLS policies for uploaded job-posting PDFs (ADR-0010,
-- ADR-0018).
--
-- Unlike 001_roles.sql-005_rls_phase3.sql, this script targets the REAL Supabase project's own
-- managed `storage` schema (storage.buckets/storage.objects) — never the local Docker Postgres
-- used for development and CI, which has no such schema at all. It is NOT run by
-- backend/scripts/setup-local-db.ps1 and is NOT covered by any automated test. Applying it is a
-- one-time operator action against the real project (using the project's own SQL editor or the
-- Supabase CLI, with the operator's own real project credentials). Required whenever the Phase 6a
-- manual PDF-upload acceptance check is performed against a real Supabase project, and again for
-- Phase 6c internet deployment — never for CI or the isolated Phase 6b E2E stack, which use no real
-- Supabase project at all. Same precedent already set for applying 001-005 to the real Supabase
-- Postgres (see docs/roadmap.md Phase 0).
--
-- ADR-0018: the application itself NEVER uses the project's service-role key for these Storage
-- calls. Every upload/delete is made with the current user's own Supabase-issued JWT (the same
-- token already validated to authenticate that request against our own API), so these RLS
-- policies — keyed on auth.uid() — are what actually enforces per-owner isolation at the Storage
-- layer. The service-role key remains reserved for genuine backend Auth-session administration
-- and for running this script itself; it is never a runtime credential for upload/delete.
--
-- Object key format: `{ownerUserId}/{Guid}` (CreateJobAnalysisFromUploadUseCase) — never the
-- original filename (ADR-0010) — so `storage.foldername(name))[1]` (the first path segment) is
-- always the owning user's own auth.uid() as text.
--
-- Idempotent: safe to re-run. INSERT ... ON CONFLICT DO NOTHING for the bucket; DROP POLICY IF
-- EXISTS + CREATE POLICY for each policy (Postgres has no CREATE POLICY IF NOT EXISTS).

-- Bucket creation can equally be done once via the Supabase dashboard (Storage -> New bucket,
-- "Public bucket" left OFF) — this INSERT is here so the whole setup is scripted and reviewable,
-- not because dashboard creation is wrong.
insert into storage.buckets (id, name, public)
values ('job-postings', 'job-postings', false)
on conflict (id) do nothing;

alter table storage.objects enable row level security;

-- INSERT: required for every upload.
drop policy if exists "job_postings_insert_own_folder" on storage.objects;
create policy "job_postings_insert_own_folder"
on storage.objects
for insert
to authenticated
with check (
    bucket_id = 'job-postings'
    and (storage.foldername(name))[1] = auth.uid()::text
);

-- SELECT: an INSERT can be implemented as `INSERT ... RETURNING` under the hood, which needs read
-- access to the affected row even though the application never separately re-reads an existing
-- object afterward (ADR-0010: the AI provider only ever receives the already-extracted text, not
-- the file).
drop policy if exists "job_postings_select_own_folder" on storage.objects;
create policy "job_postings_select_own_folder"
on storage.objects
for select
to authenticated
using (
    bucket_id = 'job-postings'
    and (storage.foldername(name))[1] = auth.uid()::text
);

-- DELETE: required for both the failure-cleanup path (CreateJobAnalysisFromUploadUseCase) and the
-- post-commit cleanup on JobAnalysis deletion (DeleteJobAnalysisUseCase).
drop policy if exists "job_postings_delete_own_folder" on storage.objects;
create policy "job_postings_delete_own_folder"
on storage.objects
for delete
to authenticated
using (
    bucket_id = 'job-postings'
    and (storage.foldername(name))[1] = auth.uid()::text
);

-- No UPDATE policy: the application never modifies an existing object in place. No policy for the
-- `anon` role or any bucket other than 'job-postings': every policy above restricts both the
-- bucket and the owner path, so there is no public access and no cross-bucket grant to close.
