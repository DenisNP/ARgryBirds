using UnityEngine;

namespace Assets.Scripts
{
    public class Bird : MonoBehaviour
    {
        void Update()
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.1f);
            if (transform.position.z > 0f)
            {
                Destroy(this);
            }
        }
        
        public void Fire(Vector3 startPoint)
        {
            Instantiate(this, startPoint, Quaternion.identity);
        }
    }
}