---
status: accepted
date: 2026-07-29
---

# Custom React components with CSS Modules and shared design tokens

## Context

Phase 1 needs a production UI approach. The approved Reading Room identity is intentionally
specific: flat paper-like surfaces, a small radius scale, local fonts and icons, and strict
semantic use of colour. The security policy also requires `style-src 'self'` without
`unsafe-inline`. The generated Claude Design package proves the visual direction, but its JSX and
HTML use inline styles, browser globals and runtime-injected markup, so they are not production
code.

The open choice was between a general UI framework (MUI), a Tailwind/shadcn stack, headless
primitives, or custom components.

## Decision

The frontend uses custom React 19 + TypeScript components implemented incrementally for each
vertical slice:

- Shared visual values are CSS custom properties in global token stylesheets.
- Components and feature layouts use CSS Modules.
- Production assets are local and bundled by Vite.
- Components use semantic HTML first and implement complete keyboard, focus and accessible-name
  behaviour.
- No general UI framework, Tailwind, shadcn, CSS-in-JS or runtime style library is introduced.
- Production JSX contains no inline `style` attributes. Dynamic visual values use semantic CSS
  classes or SVG presentation attributes rather than weakening the CSP.
- Generated files under `docs/design/` are visual references only. They are never imported,
  copied verbatim or treated as behavioural specifications.
- Components are added only when the current roadmap slice needs them. There is no up-front port
  of the complete reference inventory.

The authoritative visual and content rules live in
`docs/design/design-system/readme.md`. Domain documents and ADRs override any visual example that
conflicts with real behaviour.

## Consequences

- Reading Room can be implemented faithfully without fighting framework defaults or shipping a
  large component dependency.
- The production stylesheet satisfies the existing `style-src 'self'` CSP without
  `unsafe-inline`.
- Accessibility behaviours for dialogs, tabs, rating controls and interactive rows must be
  implemented and tested explicitly rather than assumed from a prototype.
- A small amount of component CSS is maintained locally. Shared tokens and primitives prevent
  page-level drift.
- A future headless primitive may be proposed through a new ADR if a genuinely complex accessible
  interaction proves costly; no such dependency is approved by this decision.

## Considered Alternatives

MUI was rejected because its visual defaults and runtime styling model conflict with the approved
identity and CSP. Tailwind plus shadcn was rejected because the product already has a compact
token system and does not benefit from an additional utility/configuration layer. Adopting all
generated JSX was rejected because it is JavaScript prototype code with inline styles and
incomplete production accessibility.
