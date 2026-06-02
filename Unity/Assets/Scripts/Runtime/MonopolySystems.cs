using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    [Serializable]
    public sealed class PropertyLedgerEntry
    {
        public string spaceId = string.Empty;
        public int ownerIndex = -1;
        public int buildings;
        public bool hasHotel;
        public bool mortgaged;
    }

    [Serializable]
    public sealed class TradeOffer
    {
        public int fromPlayer;
        public int toPlayer;
        public int cashFromPlayer;
        public int cashToPlayer;
        public string[] propertiesFromPlayer = Array.Empty<string>();
        public string[] propertiesToPlayer = Array.Empty<string>();
        public int jailCardsFromPlayer;
        public int jailCardsToPlayer;
    }

    [Serializable]
    public sealed class MonopolySaveState
    {
        public int currentPlayerIndex;
        public int freeParkingPot;
        public int[] playerCash = Array.Empty<int>();
        public int[] playerSpaces = Array.Empty<int>();
        public int[] jailTurns = Array.Empty<int>();
        public bool[] bankrupt = Array.Empty<bool>();
        public PropertyLedgerEntry[] properties = Array.Empty<PropertyLedgerEntry>();
        public string[] log = Array.Empty<string>();
    }

    public sealed class PropertyLedger
    {
        private readonly Dictionary<string, PropertyLedgerEntry> entries = new Dictionary<string, PropertyLedgerEntry>();

        public IReadOnlyCollection<PropertyLedgerEntry> Entries => entries.Values;

        public PropertyLedger(InsaneMonopolyCatalog catalog)
        {
            foreach (var space in catalog.spaces.Where(MonopolyRules.IsOwnable))
            {
                entries[space.id] = new PropertyLedgerEntry { spaceId = space.id };
            }
        }

        public PropertyLedgerEntry Get(string spaceId)
        {
            return entries.TryGetValue(spaceId, out var entry) ? entry : null;
        }

        public bool IsOwned(string spaceId)
        {
            var entry = Get(spaceId);
            return entry != null && entry.ownerIndex >= 0;
        }

        public IEnumerable<PropertyLedgerEntry> OwnedBy(int playerIndex)
        {
            return entries.Values.Where(entry => entry.ownerIndex == playerIndex);
        }

        public void Transfer(string spaceId, int newOwner)
        {
            var entry = Get(spaceId);
            if (entry != null)
            {
                entry.ownerIndex = newOwner;
            }
        }
    }

    public static class MonopolyRules
    {
        public static bool IsOwnable(BoardSpaceData space)
        {
            var kind = SpaceKindParser.Parse(space.kind);
            return kind == SpaceKind.Property || kind == SpaceKind.Railroad || kind == SpaceKind.Utility;
        }

        public static bool OwnsCompleteSet(InsaneMonopolyCatalog catalog, PropertyLedger ledger, int ownerIndex, BoardSpaceData landedSpace)
        {
            if (string.IsNullOrWhiteSpace(landedSpace.set))
            {
                return false;
            }

            var setSpaces = catalog.spaces.Where(space => IsOwnable(space) && space.set == landedSpace.set).ToArray();
            return setSpaces.Length > 0 && setSpaces.All(space => ledger.Get(space.id)?.ownerIndex == ownerIndex);
        }

        public static int CalculateRent(InsaneMonopolyCatalog catalog, PropertyLedger ledger, BoardSpaceData space, int diceTotal)
        {
            var entry = ledger.Get(space.id);
            if (entry == null || entry.ownerIndex < 0 || entry.mortgaged)
            {
                return 0;
            }

            var kind = SpaceKindParser.Parse(space.kind);
            if (kind == SpaceKind.Railroad)
            {
                var ownedRails = catalog.spaces.Count(candidate => SpaceKindParser.Parse(candidate.kind) == SpaceKind.Railroad && ledger.Get(candidate.id)?.ownerIndex == entry.ownerIndex);
                return Math.Max(space.rent, 25) * (int)Mathf.Pow(2, Mathf.Max(0, ownedRails - 1));
            }

            if (kind == SpaceKind.Utility)
            {
                var ownedUtilities = catalog.spaces.Count(candidate => SpaceKindParser.Parse(candidate.kind) == SpaceKind.Utility && ledger.Get(candidate.id)?.ownerIndex == entry.ownerIndex);
                return diceTotal * (ownedUtilities >= 2 ? 10 : 4);
            }

            if (entry.hasHotel)
            {
                return space.hotelRent > 0 ? space.hotelRent : space.rent * 12;
            }

            if (entry.buildings > 0 && space.houseRent != null && space.houseRent.Length >= entry.buildings)
            {
                return space.houseRent[entry.buildings - 1];
            }

            var baseRent = Math.Max(0, space.rent);
            return OwnsCompleteSet(catalog, ledger, entry.ownerIndex, space) ? baseRent * 2 : baseRent;
        }
    }

    public sealed class EconomySystem
    {
        private readonly InsaneMonopolyCatalog catalog;
        private readonly PropertyLedger ledger;

        public EconomySystem(InsaneMonopolyCatalog catalog, PropertyLedger ledger)
        {
            this.catalog = catalog;
            this.ledger = ledger;
        }

        public bool TryBuy(PlayerPawn buyer, BoardSpaceData space, out string message)
        {
            if (!MonopolyRules.IsOwnable(space))
            {
                message = $"{space.name} is not buyable.";
                return false;
            }

            var entry = ledger.Get(space.id);
            if (entry == null || entry.ownerIndex >= 0)
            {
                message = $"{space.name} is already owned.";
                return false;
            }

            if (buyer.Cash < space.price)
            {
                message = $"{buyer.PlayerName} cannot afford {space.name}. Auction should start here.";
                return false;
            }

            buyer.SetCash(buyer.Cash - space.price);
            entry.ownerIndex = buyer.PlayerIndex;
            message = $"{buyer.PlayerName} buys {space.name} for ${space.price}.";
            return true;
        }

        public int PayRent(PlayerPawn visitor, PlayerPawn owner, BoardSpaceData space, int diceTotal)
        {
            var rent = MonopolyRules.CalculateRent(catalog, ledger, space, diceTotal);
            if (rent <= 0)
            {
                return 0;
            }

            visitor.SetCash(visitor.Cash - rent);
            owner.SetCash(owner.Cash + rent);
            return rent;
        }

        public bool Mortgage(PlayerPawn owner, BoardSpaceData space, out string message)
        {
            var entry = ledger.Get(space.id);
            if (entry == null || entry.ownerIndex != owner.PlayerIndex || entry.mortgaged || entry.buildings > 0 || entry.hasHotel)
            {
                message = $"{space.name} cannot be mortgaged right now.";
                return false;
            }

            entry.mortgaged = true;
            owner.SetCash(owner.Cash + space.MortgageValue);
            message = $"{owner.PlayerName} mortgages {space.name} for ${space.MortgageValue}.";
            return true;
        }
    }

    public sealed class BuildingSystem
    {
        private readonly InsaneMonopolyCatalog catalog;
        private readonly PropertyLedger ledger;

        public BuildingSystem(InsaneMonopolyCatalog catalog, PropertyLedger ledger)
        {
            this.catalog = catalog;
            this.ledger = ledger;
        }

        public bool TryBuild(PlayerPawn owner, BoardSpaceData space, out string message)
        {
            var entry = ledger.Get(space.id);
            if (entry == null || entry.ownerIndex != owner.PlayerIndex || SpaceKindParser.Parse(space.kind) != SpaceKind.Property)
            {
                message = $"{owner.PlayerName} does not own {space.name}.";
                return false;
            }

            if (!MonopolyRules.OwnsCompleteSet(catalog, ledger, owner.PlayerIndex, space))
            {
                message = $"{owner.PlayerName} needs the full {space.set} set before building.";
                return false;
            }

            var buildCost = space.HouseCost;
            if (owner.Cash < buildCost + catalog.rules.cashReserve)
            {
                message = $"{owner.PlayerName} keeps cash instead of building on {space.name}.";
                return false;
            }

            if (entry.hasHotel)
            {
                message = $"{space.name} already has a hotel.";
                return false;
            }

            owner.SetCash(owner.Cash - buildCost);
            if (entry.buildings >= 4)
            {
                entry.buildings = 0;
                entry.hasHotel = true;
                message = $"{owner.PlayerName} upgrades {space.name} to a HOTEL. Rent is now terrifying.";
            }
            else
            {
                entry.buildings += 1;
                message = $"{owner.PlayerName} builds house {entry.buildings} on {space.name}.";
            }

            return true;
        }
    }

    public sealed class JailSystem
    {
        private readonly RuleData rules;

        public JailSystem(RuleData rules)
        {
            this.rules = rules;
        }

        public bool TryResolveJail(PlayerPawn player, bool rolledDoubles, out string message)
        {
            if (player.JailTurns <= 0)
            {
                message = string.Empty;
                return true;
            }

            if (rolledDoubles)
            {
                player.SetJailTurns(0);
                message = $"{player.PlayerName} rolls doubles and escapes Jail.";
                return true;
            }

            if (player.GetOutOfJailCards > 0)
            {
                player.SetGetOutOfJailCards(player.GetOutOfJailCards - 1);
                player.SetJailTurns(0);
                message = $"{player.PlayerName} uses a Get Out of Jail Free card.";
                return true;
            }

            if (player.Cash >= rules.jailFine)
            {
                player.SetCash(player.Cash - rules.jailFine);
                player.SetJailTurns(0);
                message = $"{player.PlayerName} pays ${rules.jailFine} to leave Jail.";
                return true;
            }

            player.SetJailTurns(player.JailTurns - 1);
            message = $"{player.PlayerName} is stuck in Jail.";
            return false;
        }
    }

    public sealed class TradingSystem
    {
        private readonly PropertyLedger ledger;

        public TradingSystem(PropertyLedger ledger)
        {
            this.ledger = ledger;
        }

        public bool Execute(TradeOffer offer, IReadOnlyList<PlayerPawn> players, out string message)
        {
            var from = players[offer.fromPlayer];
            var to = players[offer.toPlayer];
            if (from.Cash < offer.cashFromPlayer || to.Cash < offer.cashToPlayer)
            {
                message = "Trade rejected: not enough cash.";
                return false;
            }

            if (offer.propertiesFromPlayer.Any(spaceId => ledger.Get(spaceId)?.ownerIndex != from.PlayerIndex) ||
                offer.propertiesToPlayer.Any(spaceId => ledger.Get(spaceId)?.ownerIndex != to.PlayerIndex))
            {
                message = "Trade rejected: property ownership changed.";
                return false;
            }

            from.SetCash(from.Cash - offer.cashFromPlayer + offer.cashToPlayer);
            to.SetCash(to.Cash - offer.cashToPlayer + offer.cashFromPlayer);
            foreach (var spaceId in offer.propertiesFromPlayer)
            {
                ledger.Transfer(spaceId, to.PlayerIndex);
            }

            foreach (var spaceId in offer.propertiesToPlayer)
            {
                ledger.Transfer(spaceId, from.PlayerIndex);
            }

            from.SetGetOutOfJailCards(from.GetOutOfJailCards - offer.jailCardsFromPlayer + offer.jailCardsToPlayer);
            to.SetGetOutOfJailCards(to.GetOutOfJailCards - offer.jailCardsToPlayer + offer.jailCardsFromPlayer);
            message = $"{from.PlayerName} and {to.PlayerName} complete a cash/property/card trade.";
            return true;
        }
    }

    public sealed class BankruptcySystem
    {
        private readonly InsaneMonopolyCatalog catalog;
        private readonly PropertyLedger ledger;
        private readonly EconomySystem economy;

        public BankruptcySystem(InsaneMonopolyCatalog catalog, PropertyLedger ledger, EconomySystem economy)
        {
            this.catalog = catalog;
            this.ledger = ledger;
            this.economy = economy;
        }

        public bool TryAvoidBankruptcy(PlayerPawn player, out string message)
        {
            if (player.Cash >= 0)
            {
                message = string.Empty;
                return true;
            }

            foreach (var owned in ledger.OwnedBy(player.PlayerIndex).ToArray())
            {
                var space = catalog.FindSpace(owned.spaceId);
                if (space != null && economy.Mortgage(player, space, out message) && player.Cash >= 0)
                {
                    return true;
                }
            }

            foreach (var owned in ledger.OwnedBy(player.PlayerIndex))
            {
                owned.ownerIndex = -1;
                owned.buildings = 0;
                owned.hasHotel = false;
                owned.mortgaged = false;
            }

            player.SetBankrupt(true);
            message = $"{player.PlayerName} cannot cover debts and is bankrupt.";
            return false;
        }
    }

    public sealed class MonopolyAiSystem
    {
        private readonly InsaneMonopolyCatalog catalog;
        private readonly PropertyLedger ledger;

        public MonopolyAiSystem(InsaneMonopolyCatalog catalog, PropertyLedger ledger)
        {
            this.catalog = catalog;
            this.ledger = ledger;
        }

        public bool ShouldBuy(PlayerPawn player, BoardSpaceData space)
        {
            if (!MonopolyRules.IsOwnable(space) || ledger.IsOwned(space.id))
            {
                return false;
            }

            var setPressure = catalog.spaces.Count(candidate => candidate.set == space.set && ledger.Get(candidate.id)?.ownerIndex == player.PlayerIndex);
            var score = space.rent * 8 + setPressure * 90 + (SpaceKindParser.Parse(space.kind) == SpaceKind.Railroad ? 120 : 0);
            return player.Cash - space.price >= catalog.rules.cashReserve && score >= space.price;
        }

        public BoardSpaceData ChooseBuildTarget(PlayerPawn player)
        {
            return catalog.spaces
                .Where(space => SpaceKindParser.Parse(space.kind) == SpaceKind.Property)
                .Where(space => MonopolyRules.OwnsCompleteSet(catalog, ledger, player.PlayerIndex, space))
                .Where(space => ledger.Get(space.id)?.hasHotel == false)
                .OrderByDescending(space => space.hotelRent + space.rent)
                .FirstOrDefault();
        }
    }

    public sealed class SaveSystem
    {
        public string Serialize(int currentPlayerIndex, int freeParkingPot, IReadOnlyList<PlayerPawn> players, PropertyLedger ledger, IReadOnlyList<string> log)
        {
            var state = new MonopolySaveState
            {
                currentPlayerIndex = currentPlayerIndex,
                freeParkingPot = freeParkingPot,
                playerCash = players.Select(player => player.Cash).ToArray(),
                playerSpaces = players.Select(player => player.SpaceIndex).ToArray(),
                jailTurns = players.Select(player => player.JailTurns).ToArray(),
                bankrupt = players.Select(player => player.IsBankrupt).ToArray(),
                properties = ledger.Entries.ToArray(),
                log = log.ToArray()
            };
            return JsonUtility.ToJson(state, true);
        }
    }
}
