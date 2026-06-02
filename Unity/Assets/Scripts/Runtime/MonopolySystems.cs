using System;
using System.Collections.Generic;
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
        public int[] jailCards = Array.Empty<int>();
        public bool[] bankrupt = Array.Empty<bool>();
        public PropertyLedgerEntry[] properties = Array.Empty<PropertyLedgerEntry>();
        public string[] log = Array.Empty<string>();
    }

    public sealed class MonopolyRuntimeContext
    {
        private readonly Dictionary<string, BoardSpaceData> spacesById = new Dictionary<string, BoardSpaceData>(StringComparer.Ordinal);
        private readonly Dictionary<string, BoardSpaceData[]> setCache = new Dictionary<string, BoardSpaceData[]>(StringComparer.Ordinal);
        private readonly BoardSpaceData[] railroadSpaces;
        private readonly BoardSpaceData[] utilitySpaces;

        public MonopolyRuntimeContext(InsaneMonopolyCatalog catalog)
        {
            Catalog = catalog;
            var railroads = new List<BoardSpaceData>();
            var utilities = new List<BoardSpaceData>();
            var groupedSets = new Dictionary<string, List<BoardSpaceData>>(StringComparer.Ordinal);

            for (var i = 0; i < catalog.spaces.Length; i++)
            {
                var space = catalog.spaces[i];
                if (!string.IsNullOrWhiteSpace(space.id))
                {
                    spacesById[space.id] = space;
                }

                var kind = SpaceKindParser.Parse(space.kind);
                if (kind == SpaceKind.Railroad)
                {
                    railroads.Add(space);
                }
                else if (kind == SpaceKind.Utility)
                {
                    utilities.Add(space);
                }

                if (MonopolyRules.IsOwnable(space) && !string.IsNullOrWhiteSpace(space.set))
                {
                    if (!groupedSets.TryGetValue(space.set, out var setSpaces))
                    {
                        setSpaces = new List<BoardSpaceData>();
                        groupedSets.Add(space.set, setSpaces);
                    }

                    setSpaces.Add(space);
                }
            }

            foreach (var pair in groupedSets)
            {
                setCache[pair.Key] = pair.Value.ToArray();
            }

            railroadSpaces = railroads.ToArray();
            utilitySpaces = utilities.ToArray();
        }

        public InsaneMonopolyCatalog Catalog { get; }
        public IReadOnlyList<BoardSpaceData> RailroadSpaces => railroadSpaces;
        public IReadOnlyList<BoardSpaceData> UtilitySpaces => utilitySpaces;

        public BoardSpaceData FindSpace(string spaceId)
        {
            return !string.IsNullOrWhiteSpace(spaceId) && spacesById.TryGetValue(spaceId, out var space) ? space : null;
        }

        public IReadOnlyList<BoardSpaceData> GetSet(string setName)
        {
            if (string.IsNullOrWhiteSpace(setName))
            {
                return Array.Empty<BoardSpaceData>();
            }

            return setCache.TryGetValue(setName, out var spaces) ? spaces : Array.Empty<BoardSpaceData>();
        }
    }

    public sealed class PropertyLedger
    {
        private readonly Dictionary<string, PropertyLedgerEntry> entries = new Dictionary<string, PropertyLedgerEntry>(StringComparer.Ordinal);
        private readonly List<PropertyLedgerEntry> entryList = new List<PropertyLedgerEntry>();

        public IReadOnlyList<PropertyLedgerEntry> Entries => entryList;

        public PropertyLedger(MonopolyRuntimeContext context)
        {
            var spaces = context.Catalog.spaces;
            for (var i = 0; i < spaces.Length; i++)
            {
                if (!MonopolyRules.IsOwnable(spaces[i]))
                {
                    continue;
                }

                var entry = new PropertyLedgerEntry { spaceId = spaces[i].id };
                entries[spaces[i].id] = entry;
                entryList.Add(entry);
            }
        }

        public PropertyLedgerEntry Get(string spaceId)
        {
            return !string.IsNullOrWhiteSpace(spaceId) && entries.TryGetValue(spaceId, out var entry) ? entry : null;
        }

        public bool IsOwned(string spaceId)
        {
            var entry = Get(spaceId);
            return entry != null && entry.ownerIndex >= 0;
        }

        public void GetOwnedBy(int playerIndex, List<PropertyLedgerEntry> destination)
        {
            destination.Clear();
            for (var i = 0; i < entryList.Count; i++)
            {
                if (entryList[i].ownerIndex == playerIndex)
                {
                    destination.Add(entryList[i]);
                }
            }
        }

        public int CountOwnedBy(int playerIndex)
        {
            var count = 0;
            for (var i = 0; i < entryList.Count; i++)
            {
                if (entryList[i].ownerIndex == playerIndex)
                {
                    count += 1;
                }
            }

            return count;
        }

        public void Transfer(string spaceId, int newOwner)
        {
            var entry = Get(spaceId);
            if (entry != null)
            {
                entry.ownerIndex = newOwner;
            }
        }

        public void Apply(PropertyLedgerEntry[] savedEntries)
        {
            if (savedEntries == null)
            {
                return;
            }

            for (var i = 0; i < savedEntries.Length; i++)
            {
                var target = Get(savedEntries[i].spaceId);
                if (target == null)
                {
                    continue;
                }

                target.ownerIndex = savedEntries[i].ownerIndex;
                target.buildings = Mathf.Clamp(savedEntries[i].buildings, 0, 4);
                target.hasHotel = savedEntries[i].hasHotel;
                target.mortgaged = savedEntries[i].mortgaged;
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

        public static bool OwnsCompleteSet(MonopolyRuntimeContext context, PropertyLedger ledger, int ownerIndex, BoardSpaceData landedSpace)
        {
            var setSpaces = context.GetSet(landedSpace.set);
            if (setSpaces.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < setSpaces.Count; i++)
            {
                if (ledger.Get(setSpaces[i].id)?.ownerIndex != ownerIndex)
                {
                    return false;
                }
            }

            return true;
        }

        public static int CountOwnedInGroup(IReadOnlyList<BoardSpaceData> group, PropertyLedger ledger, int ownerIndex)
        {
            var count = 0;
            for (var i = 0; i < group.Count; i++)
            {
                if (ledger.Get(group[i].id)?.ownerIndex == ownerIndex)
                {
                    count += 1;
                }
            }

            return count;
        }

        public static int CalculateRent(MonopolyRuntimeContext context, PropertyLedger ledger, BoardSpaceData space, int diceTotal)
        {
            var entry = ledger.Get(space.id);
            if (entry == null || entry.ownerIndex < 0 || entry.mortgaged)
            {
                return 0;
            }

            var kind = SpaceKindParser.Parse(space.kind);
            if (kind == SpaceKind.Railroad)
            {
                var ownedRails = CountOwnedInGroup(context.RailroadSpaces, ledger, entry.ownerIndex);
                return Math.Max(space.rent, 25) * (1 << Mathf.Max(0, ownedRails - 1));
            }

            if (kind == SpaceKind.Utility)
            {
                var ownedUtilities = CountOwnedInGroup(context.UtilitySpaces, ledger, entry.ownerIndex);
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
            return OwnsCompleteSet(context, ledger, entry.ownerIndex, space) ? baseRent * 2 : baseRent;
        }
    }

    public sealed class EconomySystem
    {
        private readonly MonopolyRuntimeContext context;
        private readonly PropertyLedger ledger;

        public EconomySystem(MonopolyRuntimeContext context, PropertyLedger ledger)
        {
            this.context = context;
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
                message = $"{buyer.PlayerName} cannot afford {space.name}.";
                return false;
            }

            buyer.SetCash(buyer.Cash - space.price);
            entry.ownerIndex = buyer.PlayerIndex;
            message = $"{buyer.PlayerName} buys {space.name} for ${space.price}.";
            return true;
        }

        public int PayRent(PlayerPawn visitor, PlayerPawn owner, BoardSpaceData space, int diceTotal)
        {
            var rent = MonopolyRules.CalculateRent(context, ledger, space, diceTotal);
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

    public sealed class AuctionSystem
    {
        private readonly PropertyLedger ledger;

        public AuctionSystem(PropertyLedger ledger)
        {
            this.ledger = ledger;
        }

        public bool RunBankAuction(BoardSpaceData space, IReadOnlyList<PlayerPawn> players, int cashReserve, out string message)
        {
            var entry = ledger.Get(space.id);
            if (entry == null || entry.ownerIndex >= 0)
            {
                message = $"Auction skipped for {space.name}.";
                return false;
            }

            PlayerPawn winner = null;
            var winningBid = 0;
            var minimumBid = Math.Max(10, space.price / 2);
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.IsBankrupt)
                {
                    continue;
                }

                var available = player.Cash - cashReserve;
                if (available < minimumBid)
                {
                    continue;
                }

                var bid = Mathf.Clamp(space.price * (65 + (player.PlayerIndex * 9 % 30)) / 100, minimumBid, available);
                if (bid > winningBid)
                {
                    winner = player;
                    winningBid = bid;
                }
            }

            if (winner == null)
            {
                message = $"No one can afford the opening bid for {space.name}.";
                return false;
            }

            winner.SetCash(winner.Cash - winningBid);
            entry.ownerIndex = winner.PlayerIndex;
            message = $"Auction: {winner.PlayerName} wins {space.name} for ${winningBid}.";
            return true;
        }
    }

    public sealed class BuildingSystem
    {
        private readonly MonopolyRuntimeContext context;
        private readonly PropertyLedger ledger;

        public BuildingSystem(MonopolyRuntimeContext context, PropertyLedger ledger)
        {
            this.context = context;
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

            if (entry.mortgaged)
            {
                message = $"{space.name} is mortgaged and cannot be improved.";
                return false;
            }

            if (!MonopolyRules.OwnsCompleteSet(context, ledger, owner.PlayerIndex, space))
            {
                message = $"{owner.PlayerName} needs the full {space.set} set before building.";
                return false;
            }

            var buildCost = space.HouseCost;
            if (owner.Cash < buildCost + context.Catalog.rules.cashReserve)
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

        public bool TrySellImprovement(PlayerPawn owner, BoardSpaceData space, out string message)
        {
            var entry = ledger.Get(space.id);
            if (entry == null || entry.ownerIndex != owner.PlayerIndex || (!entry.hasHotel && entry.buildings <= 0))
            {
                message = $"{space.name} has no sellable improvements.";
                return false;
            }

            var refund = Math.Max(1, space.HouseCost / 2);
            if (entry.hasHotel)
            {
                entry.hasHotel = false;
                entry.buildings = 4;
                owner.SetCash(owner.Cash + refund);
                message = $"{owner.PlayerName} sells the hotel on {space.name} for ${refund}.";
                return true;
            }

            entry.buildings -= 1;
            owner.SetCash(owner.Cash + refund);
            message = $"{owner.PlayerName} sells a house on {space.name} for ${refund}.";
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
            if (offer.fromPlayer < 0 || offer.fromPlayer >= players.Count || offer.toPlayer < 0 || offer.toPlayer >= players.Count)
            {
                message = "Trade rejected: invalid player.";
                return false;
            }

            var from = players[offer.fromPlayer];
            var to = players[offer.toPlayer];
            if (from.Cash < offer.cashFromPlayer || to.Cash < offer.cashToPlayer ||
                from.GetOutOfJailCards < offer.jailCardsFromPlayer || to.GetOutOfJailCards < offer.jailCardsToPlayer)
            {
                message = "Trade rejected: not enough cash or cards.";
                return false;
            }

            if (!ValidateTradeProperties(offer.propertiesFromPlayer, from.PlayerIndex) || !ValidateTradeProperties(offer.propertiesToPlayer, to.PlayerIndex))
            {
                message = "Trade rejected: property ownership changed.";
                return false;
            }

            from.SetCash(from.Cash - offer.cashFromPlayer + offer.cashToPlayer);
            to.SetCash(to.Cash - offer.cashToPlayer + offer.cashFromPlayer);
            for (var i = 0; i < offer.propertiesFromPlayer.Length; i++)
            {
                ledger.Transfer(offer.propertiesFromPlayer[i], to.PlayerIndex);
            }

            for (var i = 0; i < offer.propertiesToPlayer.Length; i++)
            {
                ledger.Transfer(offer.propertiesToPlayer[i], from.PlayerIndex);
            }

            from.SetGetOutOfJailCards(from.GetOutOfJailCards - offer.jailCardsFromPlayer + offer.jailCardsToPlayer);
            to.SetGetOutOfJailCards(to.GetOutOfJailCards - offer.jailCardsToPlayer + offer.jailCardsFromPlayer);
            message = $"{from.PlayerName} and {to.PlayerName} complete a cash/property/card trade.";
            return true;
        }

        private bool ValidateTradeProperties(string[] spaceIds, int ownerIndex)
        {
            if (spaceIds == null)
            {
                return true;
            }

            for (var i = 0; i < spaceIds.Length; i++)
            {
                if (ledger.Get(spaceIds[i])?.ownerIndex != ownerIndex)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class BankruptcySystem
    {
        private readonly MonopolyRuntimeContext context;
        private readonly PropertyLedger ledger;
        private readonly EconomySystem economy;
        private readonly BuildingSystem building;
        private readonly List<PropertyLedgerEntry> ownedBuffer = new List<PropertyLedgerEntry>();

        public BankruptcySystem(MonopolyRuntimeContext context, PropertyLedger ledger, EconomySystem economy, BuildingSystem building)
        {
            this.context = context;
            this.ledger = ledger;
            this.economy = economy;
            this.building = building;
        }

        public bool TryAvoidBankruptcy(PlayerPawn player, out string message)
        {
            if (player.Cash >= 0)
            {
                message = string.Empty;
                return true;
            }

            ledger.GetOwnedBy(player.PlayerIndex, ownedBuffer);
            for (var i = 0; i < ownedBuffer.Count && player.Cash < 0; i++)
            {
                var space = context.FindSpace(ownedBuffer[i].spaceId);
                if (space != null)
                {
                    while (player.Cash < 0 && building.TrySellImprovement(player, space, out message))
                    {
                        // Keep selling until the debt is handled or improvements are gone.
                    }
                }
            }

            for (var i = 0; i < ownedBuffer.Count && player.Cash < 0; i++)
            {
                var space = context.FindSpace(ownedBuffer[i].spaceId);
                if (space != null)
                {
                    economy.Mortgage(player, space, out message);
                }
            }

            if (player.Cash >= 0)
            {
                message = $"{player.PlayerName} liquidates assets and stays alive with ${player.Cash}.";
                return true;
            }

            for (var i = 0; i < ownedBuffer.Count; i++)
            {
                ownedBuffer[i].ownerIndex = -1;
                ownedBuffer[i].buildings = 0;
                ownedBuffer[i].hasHotel = false;
                ownedBuffer[i].mortgaged = false;
            }

            player.SetBankrupt(true);
            message = $"{player.PlayerName} cannot cover debts and is bankrupt.";
            return false;
        }
    }

    public sealed class MonopolyAiSystem
    {
        private readonly MonopolyRuntimeContext context;
        private readonly PropertyLedger ledger;

        public MonopolyAiSystem(MonopolyRuntimeContext context, PropertyLedger ledger)
        {
            this.context = context;
            this.ledger = ledger;
        }

        public bool ShouldBuy(PlayerPawn player, BoardSpaceData space)
        {
            if (!MonopolyRules.IsOwnable(space) || ledger.IsOwned(space.id))
            {
                return false;
            }

            var ownedInSet = MonopolyRules.CountOwnedInGroup(context.GetSet(space.set), ledger, player.PlayerIndex);
            var score = space.rent * 8 + ownedInSet * 90 + (SpaceKindParser.Parse(space.kind) == SpaceKind.Railroad ? 120 : 0);
            return player.Cash - space.price >= context.Catalog.rules.cashReserve && score >= space.price;
        }

        public BoardSpaceData ChooseBuildTarget(PlayerPawn player)
        {
            BoardSpaceData best = null;
            var bestScore = int.MinValue;
            var spaces = context.Catalog.spaces;
            for (var i = 0; i < spaces.Length; i++)
            {
                var space = spaces[i];
                var entry = ledger.Get(space.id);
                if (entry == null || entry.ownerIndex != player.PlayerIndex || entry.hasHotel || SpaceKindParser.Parse(space.kind) != SpaceKind.Property)
                {
                    continue;
                }

                if (!MonopolyRules.OwnsCompleteSet(context, ledger, player.PlayerIndex, space))
                {
                    continue;
                }

                var score = space.hotelRent + space.rent - entry.buildings * 20;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = space;
                }
            }

            return best;
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
                playerCash = new int[players.Count],
                playerSpaces = new int[players.Count],
                jailTurns = new int[players.Count],
                jailCards = new int[players.Count],
                bankrupt = new bool[players.Count],
                properties = new PropertyLedgerEntry[ledger.Entries.Count],
                log = new string[log.Count]
            };

            for (var i = 0; i < players.Count; i++)
            {
                state.playerCash[i] = players[i].Cash;
                state.playerSpaces[i] = players[i].SpaceIndex;
                state.jailTurns[i] = players[i].JailTurns;
                state.jailCards[i] = players[i].GetOutOfJailCards;
                state.bankrupt[i] = players[i].IsBankrupt;
            }

            for (var i = 0; i < ledger.Entries.Count; i++)
            {
                var entry = ledger.Entries[i];
                state.properties[i] = new PropertyLedgerEntry
                {
                    spaceId = entry.spaceId,
                    ownerIndex = entry.ownerIndex,
                    buildings = entry.buildings,
                    hasHotel = entry.hasHotel,
                    mortgaged = entry.mortgaged
                };
            }

            for (var i = 0; i < log.Count; i++)
            {
                state.log[i] = log[i];
            }

            return JsonUtility.ToJson(state, true);
        }

        public MonopolySaveState Deserialize(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<MonopolySaveState>(json);
        }
    }
}
