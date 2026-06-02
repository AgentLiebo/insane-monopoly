using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    public sealed class CameraOrbitRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 22f;
        [SerializeField] private float height = 16f;
        [SerializeField] private float orbitSpeed = 65f;
        [SerializeField] private float zoomSpeed = 6f;
        [SerializeField] private float pitch = 55f;

        private float yaw = 45f;

        private void LateUpdate()
        {
            if (target == null)
            {
                var board = GameObject.Find("Generated 3D Board");
                target = board != null ? board.transform : null;
            }

            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime, 28f, 78f);
            }

            distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * zoomSpeed, 11f, 32f);
            var pivot = target != null ? target.position : Vector3.zero;
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = pivot + rotation * new Vector3(0f, 0f, -distance) + Vector3.up * height * 0.08f;
            transform.LookAt(pivot);
        }
    }
}
