using System.IO;
using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    public sealed class InsaneMonopolyBootstrap : MonoBehaviour
    {
        [SerializeField] private string configFileName = "insane-monopoly-config.json";

        private void Awake()
        {
            var catalog = LoadCatalog();
            ConfigureSceneLighting();

            var boardObject = new GameObject("Generated 3D Board");
            var board = boardObject.AddComponent<Board3DBuilder>();
            board.Build(catalog);

            var diceObject = new GameObject("Chaos Dice Roller");
            var diceRoller = diceObject.AddComponent<DiceRoller>();
            diceRoller.EnsureDice();

            var turnObject = new GameObject("Turn Controller");
            var turnController = turnObject.AddComponent<TurnController>();
            turnController.Initialize(catalog, board, diceRoller);

            var hudObject = new GameObject("Arcade HUD");
            var hud = hudObject.AddComponent<HudController>();
            hud.Initialize(turnController);

            EnsureCamera();
        }

        private InsaneMonopolyCatalog LoadCatalog()
        {
            var configPath = Path.Combine(Application.streamingAssetsPath, configFileName);
            if (!File.Exists(configPath))
            {
                Debug.LogWarning($"Missing config at {configPath}; using an empty fallback catalog.");
                return new InsaneMonopolyCatalog();
            }

            var json = File.ReadAllText(configPath);
            var catalog = JsonUtility.FromJson<InsaneMonopolyCatalog>(json);
            if (catalog == null || catalog.spaces == null || catalog.spaces.Length == 0)
            {
                Debug.LogWarning("Config did not contain board spaces; using an empty fallback catalog.");
                return new InsaneMonopolyCatalog();
            }

            return catalog;
        }

        private void ConfigureSceneLighting()
        {
            RenderSettings.ambientLight = new Color(0.18f, 0.2f, 0.32f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.015f, 0.02f, 0.05f);
            RenderSettings.fogDensity = 0.012f;

            var sun = new GameObject("Neon Key Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            sun.color = new Color(0.68f, 0.88f, 1f);
            sun.intensity = 1.6f;
        }

        private void EnsureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.005f, 0.01f, 0.025f);
            camera.fieldOfView = 48f;
            if (camera.GetComponent<CameraOrbitRig>() == null)
            {
                camera.gameObject.AddComponent<CameraOrbitRig>();
            }
        }
    }
}
