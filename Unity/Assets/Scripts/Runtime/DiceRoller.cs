using System.Collections;
using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    public sealed class DiceRoller : MonoBehaviour
    {
        [SerializeField] private Transform dieA;
        [SerializeField] private Transform dieB;
        [SerializeField] private float rollSeconds = 0.9f;

        public int LastDieA { get; private set; } = 1;
        public int LastDieB { get; private set; } = 1;
        public bool IsRolling { get; private set; }

        public void EnsureDice()
        {
            dieA = dieA != null ? dieA : CreateDie("Die A", new Vector3(-1.2f, 1.4f, 0f));
            dieB = dieB != null ? dieB : CreateDie("Die B", new Vector3(1.2f, 1.4f, 0f));
        }

        public IEnumerator Roll()
        {
            EnsureDice();
            IsRolling = true;
            var elapsed = 0f;
            while (elapsed < rollSeconds)
            {
                elapsed += Time.deltaTime;
                dieA.Rotate(Random.insideUnitSphere * 880f * Time.deltaTime, Space.World);
                dieB.Rotate(Random.insideUnitSphere * 880f * Time.deltaTime, Space.World);
                dieA.position = new Vector3(-1.2f, 1.4f + Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.8f, 0f);
                dieB.position = new Vector3(1.2f, 1.4f + Mathf.Abs(Mathf.Cos(elapsed * 12f)) * 0.8f, 0f);
                yield return null;
            }

            LastDieA = Random.Range(1, 7);
            LastDieB = Random.Range(1, 7);
            dieA.rotation = Quaternion.Euler(90f * LastDieA, 30f, 0f);
            dieB.rotation = Quaternion.Euler(0f, 90f * LastDieB, 45f);
            dieA.position = new Vector3(-1.2f, 1.4f, 0f);
            dieB.position = new Vector3(1.2f, 1.4f, 0f);
            IsRolling = false;
        }

        private Transform CreateDie(string dieName, Vector3 position)
        {
            var die = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            die.name = dieName;
            die.SetParent(transform, false);
            die.position = position;
            die.localScale = Vector3.one * 0.85f;

            var renderer = die.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.white;
            material.SetFloat("_Metallic", 0.2f);
            material.SetFloat("_Glossiness", 0.8f);
            renderer.sharedMaterial = material;

            var light = die.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 2.5f;
            light.intensity = 0.6f;
            return die;
        }
    }
}
