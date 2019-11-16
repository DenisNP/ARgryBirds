using System;
using UnityEngine;

namespace Assets.Scripts
{
    public class Bird : MonoBehaviour
    {
        private Planet _planet;
        private Vector3 _velocity;
        private float _speed;
        private Vector3 _axis;
        private float _angSpeed;
        private string _id;
        
        void Update()
        {
            transform.position += _velocity * _speed;
            var a = Vector3.SignedAngle(_velocity, -transform.position, _axis);
            if (a < 0f)
            {
                _velocity = Quaternion.AngleAxis(-_angSpeed, _axis) * _velocity;
            }

            if (transform.position.magnitude < 1.1f)
            {
                _planet.HitSomething(transform.position, _id);
                Destroy(gameObject);
            }
            
            if (transform.position.z > 0.5f)
            {
                Destroy(gameObject);
            }
        }
        
        public void Fire(Vector3 startPoint, float speed, float angSpeed, string id, Planet planet)
        {
            _planet = planet;
            
            var dir2d = new Vector2(startPoint.x, startPoint.y);
            var p = Vector2.Perpendicular(dir2d);
            _axis = new Vector3(p.x, p.y, 0f);
            
            var vel = startPoint - Camera.main.gameObject.transform.position;
            _velocity = vel.normalized;
            _speed = speed;
            _angSpeed = angSpeed;
            _id = id;
        }
    }
}