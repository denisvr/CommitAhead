Renders one glyph from the locally bundled Lucide sprite; use it anywhere an icon is needed instead of inlining SVG.

```jsx
<Icon name="check" size={16} />
```

The page must load `assets/icons/icons.js` once. Icons inherit `currentColor` and belong only in navigation and confirm/destructive affordances — never beside a heading, as a bullet, or in body copy.
