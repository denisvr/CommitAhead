# Page Patterns and Delivery Order

Visual references demonstrate composition only. Domain documents, API contracts and ADRs define
the behaviour.

## Delivery order

Do not generate all documented pages at once.

1. **Current foundation:** style the existing login/authenticated shell without changing the auth
   flow.
2. **Phase 2:** ProfessionalProfile and CVPresentation editing.
3. **Phase 5:** CV export.

A later-phase visual may inform component extensibility, but its route and behaviour are not built
early.

## Login

A quiet, narrow form on the paper ground: Bookmark lockup, one-sentence purpose, email field and
one primary action. Preserve the existing backend-mediated flow and generic response. The UI never
contains Supabase SDK logic or exposes whether an address is provisioned.

States: checking session, ready, submitting, generic link-sent confirmation, validation error,
expired/invalid callback and retryable server error.

## Application shell

Desktop uses the 200px sidebar and one 820px reading column. Mobile replaces the sidebar below
768px; it does not merely squeeze it. Authentication state and logout behaviour remain owned by
the existing app flow.

The active destination uses surface tint plus navy icon/text emphasis. Theme preference changes
tokens only; it must not duplicate component styling.

## Professional profile and CV presentations

ProfessionalProfile is the canonical source. A CVPresentation selects and orders canonical entry
IDs; editing a presentation never duplicates or silently mutates canonical content.

Use comfortable density for editing. Preview is bounded by a rule, not a floating card. On narrow
screens, editor and preview become tabs/stacked regions rather than scaled-down side-by-side
panes.

## Destructive operations

Use literal confirmation copy describing the actual domain effect. Never claim a cascade or
replacement that the use case does not perform. When an invariant blocks deletion, present the
reason and the safe alternative instead of offering a destructive confirmation that must fail.

## Responsive baseline

Every implemented page must be reviewed at:

- wide desktop with sidebar;
- narrow desktop/tablet;
- mobile navigation under 768px;
- zoomed text without clipped controls.

Dense tables require a deliberate mobile alternative. Horizontal overflow is acceptable only for
data whose comparison would be destroyed by stacking, and must be keyboard accessible.
