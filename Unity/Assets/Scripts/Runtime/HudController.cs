using System.Linq;
using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private TurnController turnController;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;

        public void Initialize(TurnController controller)
        {
            turnController = controller;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (turnController == null)
            {
                return;
            }

            GUI.Box(new Rect(20, 20, 390, 320), GUIContent.none, panelStyle);
            GUI.Box(new Rect(20, 20, 360, 250), GUIContent.none, panelStyle);
            GUI.Label(new Rect(36, 32, 330, 34), "INSANE MONOPOLY 3D", titleStyle);
            GUI.Label(new Rect(36, 72, 330, 28), $"Dice: {turnController.LastDiceText}", bodyStyle);
            GUI.Label(new Rect(36, 100, 330, 28), $"Free Parking Pot: ${turnController.FreeParkingPot}", bodyStyle);

            var current = turnController.Players.Count == 0 ? null : turnController.Players[turnController.CurrentPlayerIndex];
            GUI.Label(new Rect(36, 128, 330, 28), current == null ? "No players" : $"Turn: {current.PlayerName}", bodyStyle);
            GUI.enabled = !turnController.TurnInProgress;
            if (GUI.Button(new Rect(36, 168, 330, 48), turnController.TurnInProgress ? "Rolling..." : "ROLL THE CHAOS DICE", buttonStyle))
            if (GUI.Button(new Rect(36, 168, 300, 48), turnController.TurnInProgress ? "Rolling..." : "ROLL THE CHAOS DICE", buttonStyle))
            {
                turnController.RequestRoll();
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(36, 224, 158, 36), "SAMPLE TRADE"))
            {
                turnController.RequestSampleTrade();
            }
            if (GUI.Button(new Rect(208, 224, 158, 36), "PRINT SAVE JSON"))
            {
                Debug.Log(turnController.ExportSaveJson());
            }
            GUI.Label(new Rect(36, 268, 330, 24), $"Players alive: {turnController.ActivePlayerCount}", bodyStyle);

            GUI.Box(new Rect(Screen.width - 390, 20, 370, 300), GUIContent.none, panelStyle);
            GUI.Label(new Rect(Screen.width - 370, 34, 330, 26), "Players", titleStyle);
            var y = 72f;
            foreach (var player in turnController.Players)
            {
                var owned = turnController.Ledger == null ? 0 : turnController.Ledger.OwnedBy(player.PlayerIndex).Count();
                var status = player.IsBankrupt ? "BANKRUPT" : player.JailTurns > 0 ? $"Jail {player.JailTurns}" : $"Space {player.SpaceIndex}";
                GUI.Label(new Rect(Screen.width - 370, y, 340, 24), $"{player.PlayerName}: ${player.Cash} | {owned} deeds | {status}", bodyStyle);
                GUI.Label(new Rect(Screen.width - 370, y, 330, 24), $"{player.PlayerName}: ${player.Cash} | Space {player.SpaceIndex}", bodyStyle);
                y += 28f;
            }

            GUI.Box(new Rect(20, Screen.height - 270, 620, 250), GUIContent.none, panelStyle);
            GUI.Label(new Rect(36, Screen.height - 256, 580, 26), "Event Log", titleStyle);
            y = Screen.height - 220;
            foreach (var entry in turnController.EventLog)
            {
                GUI.Label(new Rect(36, y, 580, 24), entry, bodyStyle);
                y += 26f;
            }
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            var panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(0.02f, 0.03f, 0.08f, 0.84f));
            panelTexture.Apply();

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTexture },
                padding = new RectOffset(14, 14, 14, 14)
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.46f, 0.96f, 1f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
