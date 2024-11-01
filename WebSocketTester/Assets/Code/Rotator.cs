using UnityEngine;

namespace WebSocketTester
{
    public class Rotator : MonoBehaviour
    {
        #region Serialized Fields
        public float _SpeedX = 0.3f;
        public float _SpeedY = 0.4f;
        #endregion

        #region Unity Methods
        private void Update()
        {
            transform.Rotate(Vector3.right, _SpeedX * Time.deltaTime * 180.0f);
            transform.Rotate(Vector3.up, _SpeedY * Time.deltaTime * 180.0f);
        }
        #endregion
    }
}
