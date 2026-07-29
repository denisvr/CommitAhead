Label + control + hint/error wrapper. Every input in the product is wrapped in one.

```jsx
<Field label="Email" htmlFor="email" hint="We send a sign-in link. Invite-only.">
  <Input id="email" type="email" />
</Field>
```

Error text replaces the hint rather than stacking under it.
