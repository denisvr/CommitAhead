# encrypted.pdf

A minimal one-page PDF, encrypted with the standard PDF security handler (RC4-128) using a real,
non-empty user password (`userSecret`) and owner password (`ownerSecret`).

Generated once with Python's `pypdf` (a real, independent PDF library — not this project's own
code, and not a dependency of any shipped project):

```python
from pypdf import PdfWriter

writer = PdfWriter()
writer.add_blank_page(width=612, height=792)
writer.encrypt(user_password="userSecret", owner_password="ownerSecret", algorithm="RC4-128")

with open("encrypted.pdf", "wb") as f:
    writer.write(f)
```

Used by `MinimalPdfFixtures.Encrypted()` (embedded as a resource in
`CommitAhead.Infrastructure.Tests`) to prove PdfPig's own encrypted-document detection rejects a
genuinely encrypted file when opened without the password — see `PdfPigTextExtractorTests`.
