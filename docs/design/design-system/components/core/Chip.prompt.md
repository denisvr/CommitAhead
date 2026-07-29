Monochrome outline chip for StudyItem categories and queue filters.

```jsx
<Chip>System Design</Chip>
<Chip selected onClick={...}>All</Chip>
```

Categories are **never** colour-coded — LeetCode, System Design, Theory and Behavioral all render identically. Colour on a chip means it is the selected filter, nothing else.
