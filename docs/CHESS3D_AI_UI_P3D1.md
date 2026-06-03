# Chess3D AI UI P3D.1

P3D.1 keeps the existing compact Chess3D AI/Search panel and hardens behavior:

- `Search Best` runs asynchronously so the WPF UI remains responsive during bounded searches;
- `Make AI Move` runs asynchronously and applies only the selected legal profile action;
- buttons are disabled while a search is running;
- the panel displays summary JSON v2, including completed depth, nodes, qnodes, elapsed time, stopped reason, and compact best action;
- `Copy Summary` still copies the latest native summary JSON.

The UI does not add an AI-vs-AI scheduler, opening-book browser, timeline editor, or online authority controls.

