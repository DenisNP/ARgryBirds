using UnityEngine;

namespace Assets.Scripts
{
    public class Bird : MonoBehaviour
    {
        private Planet _planet;
        
        void Update()
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.1f);
            if (transform.position.magnitude < 1f)
            {
                Destroy(gameObject);
            }
        }
        
        public void Fire(Vector3 startPoint, Planet planet)
        {
            Instantiate(this, startPoint, Quaternion.identity);
            _planet = planet;
        }
    }
}