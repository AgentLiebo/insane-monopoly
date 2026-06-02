using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    public sealed class BoardSpaceView : MonoBehaviour
    {
        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private TextMesh titleLabel;
        [SerializeField] private TextMesh priceLabel;
        [SerializeField] private Light accentLight;

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
