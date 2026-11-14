# Hired Hands Co-op Bridge

This is an independent compatibility add-on for Hired Hands 1.2.1. It does not
contain or replace any Hired Hands files.

In multiplayer, the bridge disables Hired Hands' custom worker NPC simulation
and uses its existing background-work path. Hired Hands already restricts farm,
money, and save-data changes to the main player, so the host becomes the single
authority and Stardew can synchronize the resulting farm changes normally.

## Install

1. Download and install Hired Hands 1.2.1 from its official Nexus Mods page.
2. Put the `HiredHandsCoopBridge` folder in the same `Mods` folder.
3. The host presses `L` to hire and manage workers.

Only the host needs both mods for a fixed host group. Install both on every
computer if different players may host different farms.

## Limitations

- Workers operate in the background and aren't visibly walking around in co-op.
- Only the host can open the hiring board and change worker assignments.
- The bridge is version-specific to Hired Hands 1.2.1.
- This is a compatibility add-on, not an official update from the Hired Hands
  author. Test on a backed-up co-op save before relying on it long-term.

Single-player behavior is unchanged, including visible workers.
