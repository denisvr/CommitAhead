The dense scale, borrowed from the Instrument exploration, for analytical screens only: Study Items, JobAnalysis requirements, AI usage.

```jsx
<DataTable
  columns={[{key:'rank',label:'#',width:'30px',mono:true,muted:true},{key:'title',label:'Item'},
            {key:'score',label:'Score',width:'56px',align:'right',mono:true}]}
  rows={items} />
```

30px rows with a zebra tint. Never use it for the Study Queue itself, and never where a row is a mobile tap target.
