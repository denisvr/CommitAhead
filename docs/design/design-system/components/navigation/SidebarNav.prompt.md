The product's primary navigation. The six destinations are fixed — Study Queue, Study Items, Profile & CVs, Job Analyses, Interview Notes, Settings.

```jsx
<SidebarNav active="queue" onNavigate={setScreen} />
```

Categories (Theory, LeetCode, System Design, Behavioral) are filters inside Study Items, never nav entries. AI is contextual, never a destination. Collapses to a bottom bar under 768px.
