# Unity 3D Monopoly Architecture

The Unity client is organized around the normal Monopoly engine loop first, then the Insane Monopoly layer sits on top of it.

```text
InsaneMonopolyBootstrap
 ├─ Board3DBuilder / BoardSpaceView
 ├─ PlayerPawn
 ├─ DiceRoller
 ├─ TurnController
 │   ├─ PropertyLedger
 │   ├─ EconomySystem
 │   ├─ BuildingSystem
 │   ├─ JailSystem
 │   ├─ TradingSystem
 │   ├─ BankruptcySystem
 │   ├─ MonopolyAiSystem
 │   └─ SaveSystem
 └─ HudController
```

## Core loop

`TurnController` implements the board-game loop:

1. Roll two dice.
2. Resolve Jail if needed.
3. Move the current pawn around the 3D board.
4. Resolve the landing space.
5. Attempt AI purchase/build decisions.
6. Check bankruptcy and win condition.
7. Advance to the next non-bankrupt player.

## Space types

The Unity data supports the standard Monopoly space categories:

- GO
- Properties
- Railroads
- Utilities
- Chance-style card spaces
- Community Chest-style card spaces
- Taxes
- Jail / Just Visiting
- Free Parking
- Go To Jail
- Special Insane Monopoly spaces

## Property ledger

`PropertyLedger` tracks runtime ownership separately from static JSON data:

- Owner player index.
- House count.
- Hotel flag.
- Mortgage flag.

`BoardSpaceView` mirrors that state into the scene with glowing owner markers, house cubes, and hotel cylinders.

## Economy system

`EconomySystem` handles:

- Buying unowned properties.
- Paying rent to owners.
- Mortgaging unimproved properties.

Rent calculation lives in `MonopolyRules.CalculateRent` and handles:

- Base property rent.
- Doubled rent for complete sets with no houses.
- House rent tables.
- Hotel rent.
- Railroad scaling by number owned.
- Utility rent scaling by dice total.
- Mortgaged properties charging zero rent.

## Building system

`BuildingSystem` allows houses/hotels only when the owner has a full color set. Building costs, house rents, hotel rents, and mortgage values are read from StreamingAssets JSON.

## Jail system

`JailSystem` supports the standard choices in priority order:

1. Leave by rolling doubles.
2. Use a Get Out of Jail Free card.
3. Pay the configured jail fine.
4. Stay in Jail if the player cannot pay.

## Trading system

`TradingSystem` supports cash, property, and Get Out of Jail Free card exchange through a `TradeOffer`. The current HUD exposes a sample trade button; a production UI can bind to the same system.

## Bankruptcy and win condition

`BankruptcySystem` tries to mortgage assets when a player is negative. If the debt still cannot be covered, the player is marked bankrupt, their assets return to the bank, and `TurnController` skips them. The last non-bankrupt player wins.

## Save system

`SaveSystem` exports a serializable JSON snapshot with player cash, positions, Jail state, bankrupt flags, property ledger entries, Free Parking pot, current turn, and event log.
