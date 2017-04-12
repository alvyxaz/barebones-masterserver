using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    public class RotatingImageUi : MonoBehaviour
    {
        public Image RotatingImage;

        public float Speed = 2f;

        void Awake()
        {
            RotatingImage = RotatingImage ?? GetComponent<Image>();
        }

        private void Update()
        {
            RotatingImage.transform.Rotate(Vector3.forward, Time.deltaTime * 360 * Speed);
        }
    }
}