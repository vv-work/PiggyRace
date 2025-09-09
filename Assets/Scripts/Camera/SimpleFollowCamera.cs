using UnityEngine;

namespace PiggyRace.Camera
{
    // Lightweight camera follower for local testing; attach to Main Camera.
    [DisallowMultipleComponent]
    public class SimpleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);
        [SerializeField] private float positionLerp = 10f;
        [SerializeField] private float rotationLerp = 10f;

        public void SetTarget(Transform t) => target = t;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desiredPos = target.position + target.TransformDirection(offset);
            transform.position = Vector3.Lerp(transform.position, desiredPos, Mathf.Clamp01(positionLerp * Time.deltaTime));
            Quaternion desiredRot = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Mathf.Clamp01(rotationLerp * Time.deltaTime));
        }
    }
}

