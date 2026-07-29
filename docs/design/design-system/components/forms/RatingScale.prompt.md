The domain's one bespoke control: a 1–5 radio group for Importance, InitialMastery and StudyReview confidence.

```jsx
<RatingScale name="Confidence rating" value={confidence} onChange={setConfidence} />
```

Always operable by keyboard (it is a real radiogroup). Never render it as stars — this is a rating the user assigns to themselves, not a review score.
