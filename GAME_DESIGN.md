# Insane Monopoly Game Design

## Source Status

No Steam Workshop HTML file was available in this repository. This document therefore converts the user-provided product prompt into a clean implementation specification and marks all concrete board spaces, cards, events, and economy values as **inferred compatible content**. The engine is intentionally data-driven so verified Workshop text can later replace the placeholder JSON without changing code.

## Design Pillars

- **Chaotic but readable:** expanded Monopoly pacing, higher cash flow, and frequent special events.
- **Config-first:** board spaces, cards, events, property values, and rule toggles live in JSON.
- **Playable anywhere:** responsive web client, local save/load, real-time online lobbies, and Electron desktop packaging.
- **8-player ready:** human, AI, spectator, reconnect, host permissions, and replay-friendly event logs.

## Player Count and Setup

- 2–8 players.
- Starting cash: $1,800.
- Salary for passing GO: $250.
- Supported participant types: local human, remote human, spectator, and AI.
- AI difficulties: easy, normal, hard, insane.

## Board Layout

The inferred board has 36 spaces to support a compact but expanded digital layout:

1. GO
2. Crash Course
3. Chaos Chest
4. Meme Avenue
5. Luxury Tax Deluxe
6. Rocket Rail
7. Insane Chance
8. Glitch Garden
9. Neon Noodle Stand
10. Jail / Just Visiting
11. Crypto Casino
12. AI Utility Grid
13. Doom Diner
14. Portal Pad
15. Surreal Subway
16. Chaos Chest
17. Tax Refund Office
18. Gravity Gym
19. Laser Lane
20. Free Parking Jackpot
21. Moon Market
22. Insane Chance
23. Mutant Mall
24. Quantum Quay
25. Hyperloop Hijinks
26. Go Directly to Jail
27. Volcano Villa
28. Meme Power Plant
29. Insane Chance
30. Dragon Drive
31. Chaos Chest
32. Time Tax
33. Final Railgun
34. Eldritch Estate
35. Boss Fight
36. Void Vault

## Property Sets

- Tutorial Terror: Crash Course, Meme Avenue.
- Glitch District: Glitch Garden, Neon Noodle Stand.
- Risk Row: Crypto Casino, Doom Diner.
- Physics Park: Gravity Gym, Laser Lane.
- Lunar Luxury: Moon Market, Mutant Mall, Quantum Quay.
- Apocalypse Alley: Volcano Villa, Dragon Drive.
- Cosmic Endgame: Eldritch Estate, Void Vault.
- Transports: Rocket Rail, Surreal Subway, Hyperloop Hijinks, Final Railgun.
- Utilities: AI Utility Grid, Meme Power Plant.

## Core Rules

- Players roll two six-sided dice and move clockwise.
- Passing GO pays salary.
- Unowned properties may be purchased; if declined, house rules can require an auction.
- Rent is paid to owners unless the property is mortgaged.
- Street rent doubles when the owner has a complete color set and no upgrades.
- Rail rent scales with number of transports owned: $25, $50, $100, $200.
- Utility rent is dice total multiplied by 4 if one utility is owned or 10 if both are owned.
- Taxes feed the Free Parking jackpot when that house rule is enabled.
- A player with negative cash is marked bankrupt and releases properties.
- Default victory condition is last player standing; optional net-worth target is $10,000.

## Jail

- Go Directly to Jail and selected cards send a player to Jail.
- Jail bail is $75.
- A player can escape by rolling doubles or by serving 3 turns.
- Get Out of Jail-ish cards are retained until used.

## Cards

Two inferred decks are provided:

### Insane Chance

- Reality Slingshot: advance to GO and collect salary.
- Rules Lawyered: go directly to Jail.
- Mandatory Upkeep: pay repair costs for upgrades.
- Catch the Nearest Rail: advance to nearest transport and pay double rent if owned.
- Streamer Donation: collect $150.

### Chaos Chest

- Group Chaos Tax: pay every other player $25.
- Unhinged Birthday: every other player pays you $50.
- Get Out of Jail-ish: keep to leave jail once.
- Quantum Swap: swap positions with the richest opponent.
- Contractor Frenzy: place one free house on a complete set.

## Special Events

- Portal Pad: roll one die, jump forward that many spaces, and resolve the destination.
- Tax Refund Office: collect half the Free Parking jackpot.
- Boss Fight: pay $100, roll 8+ on two dice to collect $300, otherwise lose the fee.

## Upgrades

- Streets support 0–4 houses, then hotel/tower-level rent in the sixth rent slot.
- Upgrades require complete sets.
- Upgrade costs are stored per property.
- Mortgage and upgrade state are tracked per property.

## Trading and Auctions

- Trades can exchange cash, properties, jail passes, and future concessions.
- Drag-and-drop UI should build a trade offer object, then both sides must confirm.
- Auctions use a minimum bid, fixed increment, and configurable timer.

## AI Design

AI evaluates property by purchase price, rent potential, set-completion pressure, liquidity, and difficulty/risk profile. Higher difficulties bid more aggressively, identify trade targets, and reserve cash for rent exposure.

## Save, Replay, and Multiplayer

- Every game state is serializable JSON.
- SQLite stores local games and replay events.
- PostgreSQL can use the same schema shape for hosted play.
- Socket.io events support lobby create/join, chat, start, roll, state sync, and reconnect-ready game loading.
