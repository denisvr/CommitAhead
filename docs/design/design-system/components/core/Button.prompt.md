The only filled surface in the interface — use `primary` once per screen, `secondary` for the alternative, `danger` for destructive actions.

```jsx
<Button>Start review</Button>
<Button variant="secondary">Open item</Button>
<Button variant="danger" icon="trash-2">Delete item</Button>
```

Hover darkens navy rather than changing opacity; press never transforms. Disabled is 45% opacity plus `not-allowed`. Sizes are `md` (40px) and `sm` (32px) — never smaller than 44px effective on mobile.
