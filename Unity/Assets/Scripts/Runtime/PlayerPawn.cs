using System.Collections;
using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    public sealed class PlayerPawn : MonoBehaviour
    {
        [SerializeField] private float hopHeight = 0.8f;
        [SerializeField] private float secondsPerSpace = 0.18f;
        [SerializeField] private Light pawnLight;

        public int PlayerIndex { get; private set; }
        public int SpaceIndex { get; private set; }
        public int Cash { get; private set; }
        public int JailTurns { get; private set; }
        public int GetOutOfJailCards { get; private set; }
        public bool IsBankrupt { get; private set; }
        public string PlayerName { get; private set; } = "Player";

        public void Initialize(int playerIndex, string playerName, int startingCash, Color color)
        {
            PlayerIndex = playerIndex;
            PlayerName = playerName;
            Cash = startingCash;
            name = playerName;

            var renderer = GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                renderer = GameObject.CreatePrimitive(PrimitiveType.Capsule).GetComponent<Renderer>();
                renderer.transform.SetParent(transform, false);
                renderer.transform.localPosition = Vector3.zero;
                renderer.transform.localScale = Vector3.one * 0.7f;
            }

            var material = new Material(Shader.Find("Standard"));
            material.name = $"Pawn {playerName}";
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.8f);
            renderer.sharedMaterial = material;

            pawnLight = pawnLight != null ? pawnLight : gameObject.AddComponent<Light>();
            pawnLight.type = LightType.Point;
            pawnLight.color = color;
            pawnLight.range = 3f;
            pawnLight.intensity = 1.1f;
        }

        public void SetCash(int cash)
        {
            Cash = cash;
        }

        public void SetJailTurns(int jailTurns)
        {
            JailTurns = Mathf.Max(0, jailTurns);
        }

        public void SetGetOutOfJailCards(int cardCount)
        {
            GetOutOfJailCards = Mathf.Max(0, cardCount);
        }

        public void SetBankrupt(bool bankrupt)
        {
            IsBankrupt = bankrupt;
            gameObject.SetActive(!bankrupt);
        }

        public void PlaceAt(BoardSpaceView space, int pawnCountOnSpace)
        {
            SpaceIndex = space.Index;
            transform.position = space.PawnAnchor.position + PawnOffset(pawnCountOnSpace);
        }

        public IEnumerator MoveTo(Board3DBuilder board, int targetIndex)
        {
            if (board.Spaces.Count == 0)
            {
                yield break;
            }

            var total = board.Spaces.Count;
            while (SpaceIndex != targetIndex)
            {
                SpaceIndex = (SpaceIndex + 1) % total;
                var destination = board.GetSpace(SpaceIndex).PawnAnchor.position;
                yield return HopTo(destination);
            }
        }

        private IEnumerator HopTo(Vector3 destination)
        {
            var start = transform.position;
            var elapsed = 0f;
            while (elapsed < secondsPerSpace)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / secondsPerSpace);
                var arc = Mathf.Sin(t * Mathf.PI) * hopHeight;
                transform.position = Vector3.Lerp(start, destination, t) + Vector3.up * arc;
                yield return null;
            }

            transform.position = destination;
        }

        private Vector3 PawnOffset(int slot)
        {
            var angle = slot * 51.428f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.38f;
        }
    }
}
