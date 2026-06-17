# Matchmaking Table Policy

P4B table policy is deliberately simple:

- one generated room per match;
- one generated table per match;
- seat assignment is sequential from matched ticket order;
- table ruleset equals the queued `rulesetId`;
- the existing server authority starts games and validates actions.

This keeps matchmaking separate from game rules. It does not alter Classic, Single-Side, Asgard, Rubik, or Hodge semantics.
