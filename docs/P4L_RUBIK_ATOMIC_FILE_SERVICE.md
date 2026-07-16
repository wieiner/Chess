# Atomic Rubik File Service

`RubikStateFileService` is the only product file boundary for `.rubik.json`.
It separates bounded file I/O from parsing and from the live native cube.

## Save

The service serializes a validated document, creates a unique temporary file
next to the destination, writes and calls `Flush(true)`, closes it, reads it
back through the strict parser, and only then commits it. Existing files use
`File.Replace`; first saves use a same-directory `File.Move`. The destination
is never deleted before replacement. An optional `<file>.bak` contains the
previous valid destination.

Temporary files are removed on all pre-commit failures. Failure-injection
contracts cover every stage before replacement and prove that the previous
destination remains byte-for-byte unchanged and parseable.

## Read and errors

Reads use `FileShare.Read`, inspect length before allocation, enforce the same
one-megabyte default as the serializer, and return a validated immutable load
plan. Error categories distinguish path/access, oversize, malformed/version,
validation/hash, replacement/disk, cancellation, and internal failures.

No exception text contains state content, and the service never logs file
payloads. Committing the returned load plan to a native handle remains the UI
owner's explicit transactional step.
