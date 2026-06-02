using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    public sealed class TurnController : MonoBehaviour
    {
        [SerializeField] private Board3DBuilder board;
        [SerializeField] private DiceRoller diceRoller;
        [SerializeField] private int humanPlayers = 4;

        private readonly List<PlayerPawn> players = new List<PlayerPawn>();
        private readonly List<string> eventLog = new List<string>();
        private readonly Color[] pawnColors =
        {
            Color.cyan, Color.magenta, Color.yellow, Color.green,
            new Color(1f, 0.45f, 0.1f), new Color(0.45f, 0.6f, 1f),
            new Color(0.8f, 0.25f, 1f), Color.white
        };

        private InsaneMonopolyCatalog catalog = new InsaneMonopolyCatalog();
        private PropertyLedger ledger;
        private EconomySystem economy;
        private BuildingSystem building;
        private JailSystem jail;
        private TradingSystem trading;
        private BankruptcySystem bankruptcy;
        private MonopolyAiSystem ai;
        private SaveSystem saveSystem;
        private InsaneMonopolyCatalog catalog = new InsaneMonopolyCatalog();
        private int currentPlayerIndex;
        private int freeParkingPot = 500;
        private bool turnInProgress;

        public IReadOnlyList<PlayerPawn> Players => players;
        public IReadOnlyList<string> EventLog => eventLog;
        public int CurrentPlayerIndex => currentPlayerIndex;
        public int FreeParkingPot => freeParkingPot;
        public bool TurnInProgress => turnInProgress;
        public string LastDiceText => diceRoller == null ? "--" : $"{diceRoller.LastDieA} + {diceRoller.LastDieB}";
        public int ActivePlayerCount => players.Count(player => !player.IsBankrupt);
        public PropertyLedger Ledger => ledger;

        public void Initialize(InsaneMonopolyCatalog loadedCatalog, Board3DBuilder builtBoard, DiceRoller roller)
        {
            catalog = loadedCatalog;
            board = builtBoard;
            diceRoller = roller;
            ledger = new PropertyLedger(catalog);
            economy = new EconomySystem(catalog, ledger);
            building = new BuildingSystem(catalog, ledger);
            jail = new JailSystem(catalog.rules);
            trading = new TradingSystem(ledger);
            bankruptcy = new BankruptcySystem(catalog, ledger, economy);
            ai = new MonopolyAiSystem(catalog, ledger);
            saveSystem = new SaveSystem();
            SpawnPlayers(Mathf.Clamp(humanPlayers, catalog.rules.minPlayers, catalog.rules.maxPlayers));
            SyncBoardOwnershipVisuals();
            Log($"Welcome to {catalog.rules.gameName}. This is now a real 3D Monopoly loop: roll, move, buy, rent, build, bankrupt.");
            SpawnPlayers(Mathf.Clamp(humanPlayers, catalog.rules.minPlayers, catalog.rules.maxPlayers));
            Log($"Welcome to {catalog.rules.gameName}. Right-click drag to orbit, scroll to zoom.");
        }

        public void RequestRoll()
        {
            if (!turnInProgress && ActivePlayerCount > 1)
            if (!turnInProgress && players.Count > 0)
            {
                StartCoroutine(PlayTurn());
            }
        }

        public void RequestSampleTrade()
        {
            var current = players[currentPlayerIndex];
            var partner = players.FirstOrDefault(player => player.PlayerIndex != current.PlayerIndex && !player.IsBankrupt);
            if (partner == null)
            {
                return;
            }

            var currentProperty = ledger.OwnedBy(current.PlayerIndex).FirstOrDefault();
            var partnerProperty = ledger.OwnedBy(partner.PlayerIndex).FirstOrDefault();
            if (currentProperty == null || partnerProperty == null)
            {
                Log("Trade desk is open, but both players need property before a sample trade can run.");
                return;
            }

            var offer = new TradeOffer
            {
                fromPlayer = current.PlayerIndex,
                toPlayer = partner.PlayerIndex,
                cashFromPlayer = 50,
                propertiesFromPlayer = new[] { currentProperty.spaceId },
                propertiesToPlayer = new[] { partnerProperty.spaceId }
            };

            if (trading.Execute(offer, players, out var message))
            {
                Log(message);
                SyncBoardOwnershipVisuals();
            }
            else
            {
                Log(message);
            }
        }

        public string ExportSaveJson()
        {
            return saveSystem.Serialize(currentPlayerIndex, freeParkingPot, players, ledger, eventLog);
        }

        private IEnumerator PlayTurn()
        {
            turnInProgress = true;
            AdvanceToSolventPlayer();
        private IEnumerator PlayTurn()
        {
            turnInProgress = true;
            var player = players[currentPlayerIndex];
            ClearHighlights();
            Log($"{player.PlayerName} grabs the dice...");
            yield return diceRoller.Roll();

            var rollTotal = diceRoller.LastDieA + diceRoller.LastDieB;
            var rolledDoubles = diceRoller.LastDieA == diceRoller.LastDieB;
            if (!jail.TryResolveJail(player, rolledDoubles, out var jailMessage))
            {
                Log(jailMessage);
                EndTurn();
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(jailMessage))
            {
                Log(jailMessage);
            }

            var startSpace = player.SpaceIndex;
            var targetSpace = (player.SpaceIndex + rollTotal) % board.Spaces.Count;
            if (targetSpace < startSpace)
            {
                player.SetCash(player.Cash + catalog.rules.goSalary);
                Log($"{player.PlayerName} blasts past GO and collects ${catalog.rules.goSalary}.");
            }

            yield return player.MoveTo(board, targetSpace);
            ResolveLanding(player, board.GetSpace(targetSpace), rollTotal);
            board.GetSpace(targetSpace).SetHighlight(true);
            TryAutoBuild(player);
            if (!bankruptcy.TryAvoidBankruptcy(player, out var bankruptcyMessage) && !string.IsNullOrWhiteSpace(bankruptcyMessage))
            {
                Log(bankruptcyMessage);
            }

            SyncBoardOwnershipVisuals();
            AnnounceWinnerIfFinished();
            EndTurn();
        }

        private void ResolveLanding(PlayerPawn player, BoardSpaceView space, int diceTotal)
            ResolveLanding(player, board.GetSpace(targetSpace));
            board.GetSpace(targetSpace).SetHighlight(true);
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            turnInProgress = false;
        }

        private void ResolveLanding(PlayerPawn player, BoardSpaceView space)
        {
            var data = space.Data;
            switch (SpaceKindParser.Parse(data.kind))
            {
                case SpaceKind.Go:
                    player.SetCash(player.Cash + catalog.rules.goSalary);
                    Log($"{player.PlayerName} lands on GO and collects ${catalog.rules.goSalary}.");
                    break;
                case SpaceKind.Tax:
                    var tax = data.amount > 0 ? data.amount : 100;
                    player.SetCash(player.Cash - tax);
                    freeParkingPot += tax;
                    Log($"{player.PlayerName} pays ${tax} into the Free Parking jackpot.");
                    break;
                case SpaceKind.FreeParking:
                    player.SetCash(player.Cash + freeParkingPot);
                    Log($"{player.PlayerName} scoops the ${freeParkingPot} Free Parking jackpot!");
                    freeParkingPot = 0;
                    break;
                case SpaceKind.GoToJail:
                    SendToJail(player);
                    var jailIndex = FindSpaceIndex("jail");
                    player.PlaceAt(board.GetSpace(jailIndex), CountPlayersOnSpace(jailIndex));
                    Log($"{player.PlayerName} is fired from the jail cannon straight into laser jail.");
                    break;
                case SpaceKind.Card:
                    DrawCard(player, data.cardDeck);
                    break;
                case SpaceKind.Property:
                case SpaceKind.Railroad:
                case SpaceKind.Utility:
                    ResolveOwnableSpace(player, data, diceTotal);
                    break;
                default:
                    ResolveSpecialSpace(player, data);
                    Log($"{player.PlayerName} lands on {data.name}. Prototype action: buy ${data.price} / rent ${data.rent}.");
                    break;
                default:
                    Log($"{player.PlayerName} triggers {data.name}: {data.description}");
                    break;
            }
        }

        private void ResolveOwnableSpace(PlayerPawn player, BoardSpaceData data, int diceTotal)
        {
            var entry = ledger.Get(data.id);
            if (entry == null)
            {
                Log($"{data.name} has no property ledger entry yet.");
                return;
            }

            if (entry.ownerIndex < 0)
            {
                if (ai.ShouldBuy(player, data) && economy.TryBuy(player, data, out var buyMessage))
                {
                    Log(buyMessage);
                }
                else
                {
                    Log($"{player.PlayerName} declines {data.name}; auction system would start at ${data.price / 2}.");
                }
                return;
            }

            if (entry.ownerIndex == player.PlayerIndex)
            {
                Log($"{player.PlayerName} lands on their own {data.name}.");
                return;
            }

            var owner = players[entry.ownerIndex];
            var rent = economy.PayRent(player, owner, data, diceTotal);
            Log(rent > 0
                ? $"{player.PlayerName} pays ${rent} rent to {owner.PlayerName} for {data.name}."
                : $"{data.name} is mortgaged; no rent is due.");
            bankruptcy.TryAvoidBankruptcy(player, out var message);
            if (!string.IsNullOrWhiteSpace(message))
            {
                Log(message);
            }
        }

        private void ResolveSpecialSpace(PlayerPawn player, BoardSpaceData data)
        {
            if (data.id == "black-hole")
            {
                var target = Random.Range(0, board.Spaces.Count);
                player.PlaceAt(board.GetSpace(target), CountPlayersOnSpace(target));
                Log($"{player.PlayerName} is ripped through the Black Hole to {board.GetSpace(target).Data.name}.");
                return;
            }

            if (data.id == "casino")
            {
                var swing = Random.value > 0.5f ? 200 : -200;
                player.SetCash(player.Cash + swing);
                Log($"{player.PlayerName} plays Quantum Casino and {(swing > 0 ? "wins" : "loses")} ${Mathf.Abs(swing)}.");
                return;
            }

            Log($"{player.PlayerName} triggers {data.name}: {data.description}");
        }

        private void DrawCard(PlayerPawn player, string deck)
        {
            var cards = catalog.cards.Where(card => card.deck == deck).ToArray();
            if (cards.Length == 0)
            {
                Log($"{player.PlayerName} finds an empty {deck} deck.");
                return;
            }

            var card = cards[Random.Range(0, cards.Length)];
            player.SetCash(player.Cash + card.cashDelta);
            Log($"{player.PlayerName} draws {card.title}: {card.body}");

            if (card.statusEffect == "getOutOfJail")
            {
                player.SetGetOutOfJailCards(player.GetOutOfJailCards + 1);
            }

            if (!string.IsNullOrWhiteSpace(card.moveToSpaceId))
            {
                var target = FindSpaceIndex(card.moveToSpaceId);
                player.PlaceAt(board.GetSpace(target), CountPlayersOnSpace(target));
                Log($"{player.PlayerName} warps to {board.GetSpace(target).Data.name}.");
                if (card.moveToSpaceId == "jail")
                {
                    player.SetJailTurns(3);
                }
            }
        }

        private void TryAutoBuild(PlayerPawn player)
        {
            var target = ai.ChooseBuildTarget(player);
            if (target != null && building.TryBuild(player, target, out var message))
            {
                Log(message);
            }
        }

        private void SendToJail(PlayerPawn player)
        {
            var jailIndex = FindSpaceIndex("jail");
            player.PlaceAt(board.GetSpace(jailIndex), CountPlayersOnSpace(jailIndex));
            player.SetJailTurns(3);
            Log($"{player.PlayerName} is fired from the jail cannon straight into laser jail.");
        }

        private void SpawnPlayers(int count)
        {
            players.Clear();
            }
        }

        private void SpawnPlayers(int count)
        {
            players.Clear();
            var colors = new[]
            {
                Color.cyan, Color.magenta, Color.yellow, Color.green,
                new Color(1f, 0.45f, 0.1f), new Color(0.45f, 0.6f, 1f),
                new Color(0.8f, 0.25f, 1f), Color.white
            };

            for (var i = 0; i < count; i++)
            {
                var pawnObject = new GameObject($"Player {i + 1} Pawn");
                var pawn = pawnObject.AddComponent<PlayerPawn>();
                pawn.Initialize(i, $"Player {i + 1}", catalog.rules.startingCash, pawnColors[i % pawnColors.Length]);
                pawn.Initialize(i, $"Player {i + 1}", catalog.rules.startingCash, colors[i % colors.Length]);
                pawn.PlaceAt(board.GetSpace(0), i);
                players.Add(pawn);
            }
        }

        private void SyncBoardOwnershipVisuals()
        {
            foreach (var space in board.Spaces)
            {
                var entry = ledger.Get(space.Data.id);
                if (entry == null || entry.ownerIndex < 0)
                {
                    space.SetOwner(-1, Color.clear);
                    space.SetBuildings(0, false);
                    continue;
                }

                space.SetOwner(entry.ownerIndex, pawnColors[entry.ownerIndex % pawnColors.Length]);
                space.SetBuildings(entry.buildings, entry.hasHotel);
            }
        }

        private void AdvanceToSolventPlayer()
        {
            var safety = 0;
            while (players[currentPlayerIndex].IsBankrupt && safety < players.Count)
            {
                currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
                safety += 1;
            }
        }

        private void EndTurn()
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            AdvanceToSolventPlayer();
            turnInProgress = false;
        }

        private void AnnounceWinnerIfFinished()
        {
            if (ActivePlayerCount > 1)
            {
                return;
            }

            var winner = players.FirstOrDefault(player => !player.IsBankrupt);
            if (winner != null)
            {
                Log($"{winner.PlayerName} is the last player standing and wins Insane Monopoly.");
            }
        }

        private int FindSpaceIndex(string spaceId)
        {
            for (var i = 0; i < board.Spaces.Count; i++)
            {
                if (board.Spaces[i].Data.id == spaceId)
                {
                    return i;
                }
            }

            return 0;
        }

        private int CountPlayersOnSpace(int spaceIndex)
        {
            return players.Count(player => player.SpaceIndex == spaceIndex && !player.IsBankrupt);
            return players.Count(player => player.SpaceIndex == spaceIndex);
        }

        private void ClearHighlights()
        {
            foreach (var space in board.Spaces)
            {
                space.SetHighlight(false);
            }
        }

        private void Log(string message)
        {
            eventLog.Insert(0, message);
            if (eventLog.Count > 10)
            if (eventLog.Count > 8)
            {
                eventLog.RemoveAt(eventLog.Count - 1);
            }
        }
    }
}
