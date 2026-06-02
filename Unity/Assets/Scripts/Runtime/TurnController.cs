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

        public void Initialize(InsaneMonopolyCatalog loadedCatalog, Board3DBuilder builtBoard, DiceRoller roller)
        {
            catalog = loadedCatalog;
            board = builtBoard;
            diceRoller = roller;
            SpawnPlayers(Mathf.Clamp(humanPlayers, catalog.rules.minPlayers, catalog.rules.maxPlayers));
            Log($"Welcome to {catalog.rules.gameName}. Right-click drag to orbit, scroll to zoom.");
        }

        public void RequestRoll()
        {
            if (!turnInProgress && players.Count > 0)
            {
                StartCoroutine(PlayTurn());
            }
        }

        private IEnumerator PlayTurn()
        {
            turnInProgress = true;
            var player = players[currentPlayerIndex];
            ClearHighlights();
            Log($"{player.PlayerName} grabs the dice...");
            yield return diceRoller.Roll();

            var rollTotal = diceRoller.LastDieA + diceRoller.LastDieB;
            var startSpace = player.SpaceIndex;
            var targetSpace = (player.SpaceIndex + rollTotal) % board.Spaces.Count;
            if (targetSpace < startSpace)
            {
                player.SetCash(player.Cash + catalog.rules.goSalary);
                Log($"{player.PlayerName} blasts past GO and collects ${catalog.rules.goSalary}.");
            }

            yield return player.MoveTo(board, targetSpace);
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
                    Log($"{player.PlayerName} lands on {data.name}. Prototype action: buy ${data.price} / rent ${data.rent}.");
                    break;
                default:
                    Log($"{player.PlayerName} triggers {data.name}: {data.description}");
                    break;
            }
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

            if (!string.IsNullOrWhiteSpace(card.moveToSpaceId))
            {
                var target = FindSpaceIndex(card.moveToSpaceId);
                player.PlaceAt(board.GetSpace(target), CountPlayersOnSpace(target));
                Log($"{player.PlayerName} warps to {board.GetSpace(target).Data.name}.");
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
                pawn.Initialize(i, $"Player {i + 1}", catalog.rules.startingCash, colors[i % colors.Length]);
                pawn.PlaceAt(board.GetSpace(0), i);
                players.Add(pawn);
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
            if (eventLog.Count > 8)
            {
                eventLog.RemoveAt(eventLog.Count - 1);
            }
        }
    }
}
