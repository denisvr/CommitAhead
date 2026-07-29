One proposal inside an AnalysisDraft, with its per-proposal Accept / Reject. Nothing in the domain changes until the whole draft is applied.

```jsx
<ProposalRow kind="Link proposal" rationale="The posting names rate limiting twice as a required skill."
  onAccept={...} onReject={...}>
  Link this analysis to <strong>Design a Rate Limiter</strong> — weight 4 of 5
</ProposalRow>
```

Always show the rationale. Accept and Reject are equal-weight siblings — never pre-select Accept.
