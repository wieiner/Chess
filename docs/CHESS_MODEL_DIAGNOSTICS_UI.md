# Chess Model Diagnostics UI

The visual diagnostics panel exposes the model/material state a tester needs during manual QA:

- active piece set;
- OBJ model count;
- fallback primitive count;
- overlay count;
- animation lock state;
- selected visual mode and selection state;
- current visual options;
- last OBJ/MTL/material diagnostic text;
- last invalid click/action reason.

Missing texture or material files must not crash the app. The renderer falls back to readable procedural materials.
