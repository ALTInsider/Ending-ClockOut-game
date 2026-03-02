# Streamer.bot action wiring for Clock-Out Battle Royale

## Required command actions

- `!clockin` -> use `clockin-checker.cs`
- `!clockgin` -> use `clockgin-checker.cs`
- `!clockout` -> use `clockout-checker.cs`
- `!clockgout` -> use `clockgout-checker.cs`
- `!battle` (or hotkey/stream deck) -> use `battle-trigger.cs`
- End-of-stream cleanup action (hotkey/stream end trigger) -> use `battle-clear.cs`

## What changed to make battle work

Both clock-out scripts now do **three** battle-critical steps:

1. Save participant globals:
   - `battle_{userId}_name`
   - `battle_{userId}_health`
   - deduped `battleParticipantIds`
2. Broadcast live join event:
   - `{"event":"clockout","userName":"...","health":123}`
3. Keep your chat acknowledgement logic.

Then `battle-trigger.cs`:

1. Reads all IDs from `battleParticipantIds`
2. Builds and broadcasts:
   - `battle_load` with participants array
   - `battle_begin`
3. Clears all battle globals after starting.

## Overlay compatibility notes

The overlay listens for:

- `clockout`
- `battle_load`
- `battle_begin` (also legacy `battle_start`)
- `final_showdown_begin` (optional manual trigger)

If you want Stream Deck control for showdown, add a simple action that broadcasts:

```json
{"event":"final_showdown_begin"}
```

## End-of-stream cleanup (recommended)

Run `battle-clear.cs` at stream end (or stream start) to remove leftover queue data when no battle ran.

It clears:
- `battleParticipantIds`
- every `battle_{userId}_name`
- every `battle_{userId}_health`

This prevents yesterday's clockouts from carrying into the next stream's battle.
