# Rubik Solution Verification Authority

Solver output is untrusted until `RubikSolutionVerifier` replays it through an
independent move-executor context created from a clone of the portable input.
The verifier validates every structured move, applies it, validates every
intermediate facelet state, computes final facelets/hash, and requires the
canonical solved color layout.

Verification returns a typed status and the number of moves actually applied.
A legal but truncated or incorrect sequence returns `Failed`, including its
diagnostic final hash; successful application alone is never enough. Malformed
axis/turn data and out-of-range layers fail before mutation. Cancellation is a
normal failed verification result.

Contract tests use a fresh native Rubik handle as the executor authority and
cover a valid reverse-history solution, malformed move, illegal layer,
truncated solution, syntactically valid incorrect sequence, cancellation, and
caller-input immutability. Future solver backends use the same verifier.
