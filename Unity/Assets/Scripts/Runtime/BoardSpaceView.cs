using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    public sealed class BoardSpaceView : MonoBehaviour
    {
        private static Material houseMaterial;
        private static Material hotelMaterial;

        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private TextMesh titleLabel;
        [SerializeField] private TextMesh priceLabel;
        [SerializeField] private Light accentLight;

        private GameObject ownerMarker;
        private Renderer ownerMarkerRenderer;
        private Material ownerMarkerMaterial;
        private readonly GameObject[] buildingMarkers = new GameObject[5];
        private int currentOwnerIndex = int.MinValue;
        private int currentHouses = -1;
        private bool currentHotel;

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
            if (currentOwnerIndex == ownerIndex)
            {
                return;
            }

            if (ownerIndex < 0 && ownerMarker == null)
            {
                currentOwnerIndex = ownerIndex;
                return;
            }

            EnsureOwnerMarker();
            currentOwnerIndex = ownerIndex;
            ownerMarker.SetActive(ownerIndex >= 0);
            if (ownerIndex < 0)
            {
                return;
            }

            if (ownerMarkerMaterial == null)
            {
                ownerMarkerMaterial = new Material(Shader.Find("Standard"));
                ownerMarkerMaterial.name = $"Owner Marker {Index:00}";
                ownerMarkerMaterial.EnableKeyword("_EMISSION");
                ownerMarkerRenderer.sharedMaterial = ownerMarkerMaterial;
            }

            ownerMarkerMaterial.color = ownerColor;
            ownerMarkerMaterial.SetColor("_EmissionColor", ownerColor * 1.2f);
        }

        public void SetBuildings(int houses, bool hotel)
        {
            houses = Mathf.Clamp(houses, 0, 4);
            if (currentHouses == houses && currentHotel == hotel)
            {
                return;
            }

            if (houses == 0 && !hotel && currentHouses <= 0 && !currentHotel)
            {
                currentHouses = 0;
                currentHotel = false;
                return;
            }

            currentHouses = houses;
            currentHotel = hotel;
            EnsureBuildingMaterials();
            for (var i = 0; i < buildingMarkers.Length; i++)
            {
                EnsureBuildingMarker(i);
                var active = hotel ? i == 4 : i < houses;
                buildingMarkers[i].SetActive(active);
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

        private void EnsureOwnerMarker()
        {
            if (ownerMarker != null)
            {
                return;
            }

            ownerMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ownerMarker.name = "Owner Marker";
            ownerMarker.transform.SetParent(transform, false);
            ownerMarker.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            ownerMarker.transform.localScale = new Vector3(0.85f, 0.04f, 0.85f);
            ownerMarkerRenderer = ownerMarker.GetComponent<Renderer>();
            Destroy(ownerMarker.GetComponent<Collider>());
        }

        private void EnsureBuildingMarker(int index)
        {
            if (buildingMarkers[index] != null)
            {
                return;
            }

            buildingMarkers[index] = GameObject.CreatePrimitive(index == 4 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            buildingMarkers[index].name = index == 4 ? "Hotel Marker" : $"House Marker {index + 1}";
            buildingMarkers[index].transform.SetParent(transform, false);
            buildingMarkers[index].transform.localPosition = new Vector3(-0.54f + index * 0.27f, 0.82f, 0.58f);
            buildingMarkers[index].transform.localScale = index == 4 ? new Vector3(0.34f, 0.22f, 0.34f) : new Vector3(0.2f, 0.2f, 0.2f);
            buildingMarkers[index].GetComponent<Renderer>().sharedMaterial = index == 4 ? hotelMaterial : houseMaterial;
            Destroy(buildingMarkers[index].GetComponent<Collider>());
        }

        private static void EnsureBuildingMaterials()
        {
            if (houseMaterial == null)
            {
                houseMaterial = CreateSharedMarkerMaterial("House Marker", new Color(0.1f, 1f, 0.35f));
            }

            if (hotelMaterial == null)
            {
                hotelMaterial = CreateSharedMarkerMaterial("Hotel Marker", new Color(1f, 0.85f, 0.1f));
            }
        }

        private static Material CreateSharedMarkerMaterial(string materialName, Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = materialName;
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.9f);
            return material;
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
