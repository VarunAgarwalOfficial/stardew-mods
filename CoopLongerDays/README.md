# Co-op Longer Days

Co-op Longer Days makes the Stardew Valley day clock run more slowly without
rewinding completed ten-minute updates or changing NPC and monster speeds.

## Install

1. Install SMAPI 4.0 or later.
2. Remove any other time-speed mod, including the original Longer Days.
3. Put the `CoopLongerDays` folder in Stardew Valley's `Mods` folder.
4. Install the same version on the host and every farmhand.

The host's setting is authoritative and is sent to every connected player. The
default multiplier is `2.0`, so each day lasts twice as long (14 real seconds
per 10 in-game minutes). The allowed range is 1x through 6x.

The host can show or change the setting in the SMAPI console:

```text
coop_days
coop_days 2
```

Farmhands cannot override the host while connected. This mod does not change
save files and can be removed safely. Do not combine it with another mod that
changes the speed of the game clock.

Version 1.2 also checks each connecting player's Stardew, SMAPI, and installed
co-op mod versions against this tested setup. A mismatch produces a HUD warning;
the SMAPI console lists the exact missing or incorrect entries. This check is
diagnostic only and doesn't disconnect anyone.

Built for Stardew Valley 1.6.15 and SMAPI 4.5.2. The multiplayer synchronization
logic has been locally verified, but a real multi-computer session is still
recommended before relying on it for a long-running save.
