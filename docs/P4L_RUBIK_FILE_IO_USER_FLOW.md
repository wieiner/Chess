# Rubik State File UI

## State files

The top File group in Rubik Studio provides `Save State`, `Save As`, `Load
State`, an in-process recent-file list, and separate move import/export. State
dialogs default to `.rubik.json`; move dialogs use `.rubikmoves`.

Saving exports physical U/R/F/D/L/B facelets, validates them, calculates the
canonical hash, and calls the atomic file service. Replacing a file retains a
`.bak` copy. The header shows the current filename, full physical hash,
validation state, and `*` dirty marker. Recent paths remain only in process
memory and contain no cube payload.

Loading is transactional through both layers:

1. bounded file read, parse, validation, normalization, and hash verification;
2. creation of a separate native engine handle;
3. size and facelet application to that candidate;
4. live handle swap only after native acceptance;
5. old handle disposal and scene refresh after the commit.

An error leaves the current native handle, scene, hash, and selected file
unchanged. Physical imports intentionally have untrusted/empty move history and
render through the honest facelet shell until a cubie decomposition is proved.

## Move files and legacy data

`Export Moves` writes current notation as UTF-8 `.rubikmoves`. `Import Moves`
performs a bounded read and notation parse, then places validated text in the
Output panel for explicit `Apply` or `Play`; importing does not silently mutate
the cube.

The Position tab's integer cell text remains as an explicitly labelled debug
compatibility path. Integer cubie IDs cannot preserve sticker orientation and
therefore cannot be saved as a portable physical state.
