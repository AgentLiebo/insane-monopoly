using System.Collections.Generic;
using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    public sealed class Board3DBuilder : MonoBehaviour
    {
        [SerializeField] private float boardRadius = 9.5f;
        [SerializeField] private float tileHeight = 0.22f;
        [SerializeField] private float cornerTileSize = 2.2f;
        [SerializeField] private float edgeTileWidth = 1.55f;
        [SerializeField] private float edgeTileDepth = 2.0f;
        [SerializeField] private float centerPlatformSize = 12f;
        [SerializeField] private float tableSize = 24f;
        [SerializeField] private Material centerMaterial;

        private readonly List<BoardSpaceView> spaces = new List<BoardSpaceView>();
        private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        public IReadOnlyList<BoardSpaceView> Spaces => spaces;

        public void Build(InsaneMonopolyCatalog catalog)
        {
            ClearExistingBoard();
            BuildTablePresentation();
            BuildCenterPlatform();

            for (var i = 0; i < catalog.spaces.Length; i++)
            {
                var data = catalog.spaces[i];
                var isCorner = i % 10 == 0;
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.transform.SetParent(transform, false);
                tile.transform.position = PositionForIndex(i, catalog.spaces.Length);
                tile.transform.rotation = RotationForIndex(i, catalog.spaces.Length);
                tile.transform.localScale = isCorner
                    ? new Vector3(cornerTileSize, tileHeight, cornerTileSize)
                    : new Vector3(edgeTileWidth, tileHeight, edgeTileDepth);

                var view = tile.AddComponent<BoardSpaceView>();
                view.Initialize(i, data, MaterialFor(data), isCorner);
                spaces.Add(view);
            }

            BuildCardDeckStacks();
            BuildCornerMonuments();
        }

        public BoardSpaceView GetSpace(int index)
        {
            if (spaces.Count == 0)
            {
                return null;
            }

            var wrapped = ((index % spaces.Count) + spaces.Count) % spaces.Count;
            return spaces[wrapped];
        }

        private void ClearExistingBoard()
        {
            spaces.Clear();
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }


        private void BuildTablePresentation()
        {
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Polished Table Base";
            table.transform.SetParent(transform, false);
            table.transform.position = new Vector3(0f, -0.42f, 0f);
            table.transform.localScale = new Vector3(tableSize, 0.55f, tableSize);
            table.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Walnut Table", new Color(0.16f, 0.08f, 0.035f));

            var felt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            felt.name = "Inset Board Felt";
            felt.transform.SetParent(transform, false);
            felt.transform.position = new Vector3(0f, -0.12f, 0f);
            felt.transform.localScale = new Vector3(tableSize - 2.2f, 0.08f, tableSize - 2.2f);
            felt.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Midnight Felt", new Color(0.01f, 0.09f, 0.12f));

            BuildRail("North Rail", new Vector3(0f, 0.05f, boardRadius + 1.45f), new Vector3(tableSize - 1.2f, 0.42f, 0.32f));
            BuildRail("South Rail", new Vector3(0f, 0.05f, -boardRadius - 1.45f), new Vector3(tableSize - 1.2f, 0.42f, 0.32f));
            BuildRail("East Rail", new Vector3(boardRadius + 1.45f, 0.05f, 0f), new Vector3(0.32f, 0.42f, tableSize - 1.2f));
            BuildRail("West Rail", new Vector3(-boardRadius - 1.45f, 0.05f, 0f), new Vector3(0.32f, 0.42f, tableSize - 1.2f));
        }

        private void BuildRail(string railName, Vector3 position, Vector3 scale)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = railName;
            rail.transform.SetParent(transform, false);
            rail.transform.position = position;
            rail.transform.localScale = scale;
            rail.GetComponent<Renderer>().sharedMaterial = CreateMaterial(railName, new Color(0.38f, 0.18f, 0.07f));
        }

        private void BuildCardDeckStacks()
        {
            BuildDeckStack("Chaos Chest Deck", new Vector3(-3.2f, 0.36f, 1.25f), new Color(0.1f, 0.65f, 1f), "CHAOS\nCHEST");
            BuildDeckStack("Mutation Chance Deck", new Vector3(3.2f, 0.36f, -1.25f), new Color(1f, 0.8f, 0.1f), "MUTATION\nCHANCE");
        }

        private void BuildDeckStack(string deckName, Vector3 position, Color color, string label)
        {
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = deckName;
            deck.transform.SetParent(transform, false);
            deck.transform.position = position;
            deck.transform.localScale = new Vector3(2.25f, 0.18f, 1.45f);
            deck.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            deck.GetComponent<Renderer>().sharedMaterial = CreateMaterial(deckName, color);

            var text = new GameObject($"{deckName} Label").AddComponent<TextMesh>();
            text.transform.SetParent(deck.transform, false);
            text.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            text.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            text.text = label;
            text.characterSize = 0.24f;
            text.fontSize = 72;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
        }

        private void BuildCornerMonuments()
        {
            BuildCornerMonument("GO Beacon", new Vector3(boardRadius, 0.62f, -boardRadius), new Color(0.1f, 1f, 0.45f));
            BuildCornerMonument("Jail Tower", new Vector3(-boardRadius, 0.62f, -boardRadius), new Color(0.35f, 0.45f, 0.65f));
            BuildCornerMonument("Parking Jackpot", new Vector3(-boardRadius, 0.62f, boardRadius), new Color(0.0f, 0.95f, 1f));
            BuildCornerMonument("Jail Cannon", new Vector3(boardRadius, 0.62f, boardRadius), new Color(1f, 0.18f, 0.12f));
        }

        private void BuildCornerMonument(string monumentName, Vector3 position, Color color)
        {
            var monument = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            monument.name = monumentName;
            monument.transform.SetParent(transform, false);
            monument.transform.position = position;
            monument.transform.localScale = new Vector3(0.42f, 0.7f, 0.42f);
            monument.GetComponent<Renderer>().sharedMaterial = CreateMaterial(monumentName, color);

            var light = monument.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4f;
            light.intensity = 1.1f;
            light.color = color;
        }

        private void BuildCenterPlatform()
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Insane Monopoly Center Platform";
            platform.transform.SetParent(transform, false);
            platform.transform.position = new Vector3(0f, -0.08f, 0f);
            platform.transform.localScale = new Vector3(centerPlatformSize, 0.12f, centerPlatformSize);
            var renderer = platform.GetComponent<Renderer>();
            renderer.sharedMaterial = centerMaterial != null ? centerMaterial : CreateMaterial("Center", new Color(0.04f, 0.06f, 0.13f));

            var title = new GameObject("Board Title").AddComponent<TextMesh>();
            title.transform.SetParent(platform.transform, false);
            title.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            title.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            title.text = "INSANE\nMONOPOLY";
            title.characterSize = 0.85f;
            title.fontSize = 96;
            title.anchor = TextAnchor.MiddleCenter;
            title.alignment = TextAlignment.Center;
            title.color = new Color(0.55f, 0.95f, 1f);
        }

        private Vector3 PositionForIndex(int index, int totalSpaces)
        {
            var sideLength = totalSpaces / 4;
            var side = index / sideLength;
            var offset = index % sideLength;
            var t = sideLength <= 1 ? 0f : offset / (float)(sideLength - 1);
            var min = -boardRadius;
            var max = boardRadius;

            switch (side)
            {
                case 0: return new Vector3(Mathf.Lerp(max, min, t), 0f, min);
                case 1: return new Vector3(min, 0f, Mathf.Lerp(min, max, t));
                case 2: return new Vector3(Mathf.Lerp(min, max, t), 0f, max);
                default: return new Vector3(max, 0f, Mathf.Lerp(max, min, t));
            }
        }

        private Quaternion RotationForIndex(int index, int totalSpaces)
        {
            var side = index / (totalSpaces / 4);
            return Quaternion.Euler(0f, side * 90f, 0f);
        }

        private Material MaterialFor(BoardSpaceData data)
        {
            var key = string.IsNullOrWhiteSpace(data.color) ? "#ffffff" : data.color;
            if (materialCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var parsed = ColorUtility.TryParseHtmlString(key, out var color) ? color : Color.magenta;
            var material = CreateMaterial(key, parsed);
            materialCache.Add(key, material);
            return material;
        }

        private Material CreateMaterial(string materialName, Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = $"Insane {materialName}";
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.35f);
            return material;
        }
    }
}
