using UnityEngine;

namespace SilklessCoop
{
    internal class SimpleInterpolator : MonoBehaviour
    {
        public Vector3 velocity;
        public float drag = 0.8f;

        private void Start()
        {

        }

        private void Update()
        {
            transform.position += velocity * Time.deltaTime;

            velocity *= Mathf.Pow(1 - drag, Time.deltaTime);
        }
    }
}
