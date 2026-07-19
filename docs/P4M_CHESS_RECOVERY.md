# Chess2D Autosave and Recovery

Chess Advisor keeps recovery files in `%LOCALAPPDATA%\Chess\Autosave`. The directory is user-local and is not part of the repository or a production package.

An accepted move marks the active session dirty and restarts a 750 ms debounce timer. When the timer expires, the application writes a versioned session through the same temporary-file, flush, re-read, hash, and atomic-replace path as an explicit save. Rejected moves never call the dirty/autosave path. Up to eight valid recovery files are retained per session.

At startup the application ignores `.tmp`, malformed, unsupported, and non-autosave documents. For a valid candidate it offers:

- **Yes**: open it as an unsaved recovered copy;
- **No**: discard that recovery file;
- **Cancel**: retain it for a later launch.

Opening a recovered copy clears the explicit destination path, so **Save Session** routes to **Save Session As** and cannot silently overwrite a manual file. A successful explicit save discards recovery files for that session. If an explicit session is newer, the recovery service does not offer its stale autosave for that file.
