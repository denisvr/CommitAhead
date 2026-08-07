---
status: accepted
date: 2026-07-28
---

# JobSource PDF text is extracted once at upload; AI receives text only

## Context

A `JobAnalysis` can be created from a pasted job description or an uploaded PDF. AI analysis needs the posting content as text. The question was when and where to extract text from PDFs, and whether the AI provider should fetch files directly.

## Decision

`JobSource` is a discriminated union: `PastedText(content)` or `UploadedFile(storageObjectKey, originalFileName, mimeType, extractedText)`.

For uploaded PDFs, text extraction happens once during the upload request using a maintained text-only library under strict page-count and 50 000-character output caps, enforced explicitly by the extractor itself, plus a best-effort extraction timeout — the library's own API is synchronous and uncancellable, so the timeout is a wall-clock race, not a hard parser-level guarantee; container memory/CPU limits are the real backstop against a runaway parse. The extracted text is stored in `UploadedFile.extractedText`. The `storageObjectKey` is a backend-generated quarantine key; the original filename is never used as a storage path.

The AI provider always receives the extracted text string. It never fetches files from Supabase Storage, receives URLs, or accesses embedded content. Explicit rejection with a user-visible error replaces silent truncation when limits are exceeded.

Rejected uploads (malformed, encrypted, image-only, wrong MIME, or oversized) have their Storage objects deleted best-effort; on a delete failure, the orphaned object's key is logged for manual cleanup rather than the response being blocked on it.

## Consequences

- Text extraction is independently testable in isolation from AI commands.
- The AI provider's trust boundary is clean: it receives a bounded text string with no external references.
- If the extraction library has a vulnerability, the blast radius is limited to the upload endpoint, not the AI provider call path.
- `UploadedFile.extractedText` grows the `JobAnalysis` row; very large postings are bounded by the 50 000-character cap.

## Considered Alternatives

Having the AI provider fetch the PDF directly via a pre-signed Supabase Storage URL was considered. This was rejected because it gives the provider an external network reference, risks the provider following embedded links in malicious PDFs, and makes text extraction non-testable in isolation. It also couples the AI layer to the storage infrastructure.
