using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    public sealed class BoardSpaceView : MonoBehaviour
    {
        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private TextMesh titleLabel;
        [SerializeField] private TextMesh priceLabel;
        [SerializeField] private Light accentLight;
        private GameObject ownerMarker;
        private readonly GameObject[] buildingMarkers = new GameObject[5];

        public int Index { get; private set; }
        public BoardSpaceData Data { get; private set; } = new BoardSpaceData();
        public Transform PawnAnchor { get; private set; }

        public void Initialize(int index, BoardSpaceData data, Material material, bool isCorner)
        {
            Index = index;
            Data = data;
            name = $"Space {index:00} - {data.name}";
            PawnAnchor = new GameObject("Pawn Anchor").transform;
            PawnAnchor.SetParent(transform, false);
            PawnAnchor.localPosition = new Vector3(0f, isCorner ? 0.45f : 0.3f, 0f);

            tileRenderer = tileRenderer != null ? tileRenderer : GetComponentInChildren<Renderer>();
            if (tileRenderer != null)
            {
                tileRenderer.sharedMaterial = material;
            }

            titleLabel = titleLabel != null ? titleLabel : CreateLabel("Title", data.name, 0.18f, new Vector3(0f, 0.62f, -0.25f));
            titleLabel.anchor = TextAnchor.MiddleCenter;
            titleLabel.alignment = TextAlignment.Center;
            titleLabel.color = Color.white;

            var priceText = data.price > 0 ? $"${data.price}" : SpaceKindParser.Parse(data.kind).ToString();
            priceLabel = priceLabel != null ? priceLabel : CreateLabel("Price", priceText, 0.14f, new Vector3(0f, 0.62f, 0.34f));
            priceLabel.anchor = TextAnchor.MiddleCenter;
            priceLabel.alignment = TextAlignment.Center;
            priceLabel.color = Color.yellow;

            accentLight = accentLight != null ? accentLight : gameObject.AddComponent<Light>();
            accentLight.type = LightType.Point;
            accentLight.range = isCorner ? 4f : 2.5f;
            accentLight.intensity = isCorner ? 0.9f : 0.35f;
            accentLight.color = material.color;
        }


        public void SetOwner(int ownerIndex, Color ownerColor)
        {
            if (ownerMarker == null)
            {
                ownerMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ownerMarker.name = "Owner Marker";
                ownerMarker.transform.SetParent(transform, false);
                ownerMarker.transform.localPosition = new Vector3(0f, 0.72f, 0f);
                ownerMarker.transform.localScale = new Vector3(0.85f, 0.04f, 0.85f);
                Destroy(ownerMarker.GetComponent<Collider>());
            }

            ownerMarker.SetActive(ownerIndex >= 0);
            if (ownerIndex >= 0)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = ownerColor;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", ownerColor * 1.2f);
                ownerMarker.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        public void SetBuildings(int houses, bool hotel)
        {
            for (var i = 0; i < buildingMarkers.Length; i++)
            {
                if (buildingMarkers[i] == null)
                {
                    buildingMarkers[i] = GameObject.CreatePrimitive(i == 4 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                    buildingMarkers[i].name = i == 4 ? "Hotel Marker" : $"House Marker {i + 1}";
                    buildingMarkers[i].transform.SetParent(transform, false);
                    Destroy(buildingMarkers[i].GetComponent<Collider>());
                }

                var active = hotel ? i == 4 : i < houses;
                buildingMarkers[i].SetActive(active);
                if (active)
                {
                    buildingMarkers[i].transform.localPosition = new Vector3(-0.54f + i * 0.27f, 0.82f, 0.58f);
                    buildingMarkers[i].transform.localScale = i == 4 ? new Vector3(0.34f, 0.22f, 0.34f) : new Vector3(0.2f, 0.2f, 0.2f);
                    var material = new Material(Shader.Find("Standard"));
                    material.color = i == 4 ? new Color(1f, 0.85f, 0.1f) : new Color(0.1f, 1f, 0.35f);
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", material.color * 0.9f);
                    buildingMarkers[i].GetComponent<Renderer>().sharedMaterial = material;
                }
            }
        }

        public void SetHighlight(bool highlighted)
        {
            transform.localScale = highlighted ? Vector3.one * 1.08f : Vector3.one;
            if (accentLight != null)
            {
                accentLight.intensity = highlighted ? 2.4f : 0.35f;
            }
        }

        private TextMesh CreateLabel(string labelName, string text, float size, Vector3 localPosition)
        {
            var labelObject = new GameObject(labelName);
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var mesh = labelObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = size;
            mesh.fontSize = 64;
            mesh.richText = true;
            return mesh;
        }
    }
}
