using UnityEngine;

namespace SimpleSync.Components;

internal class SimpleInterpolator : MonoBehaviour
{
    private Vector3 _velocity;
    
    private tk2dSprite _sprite;
    private float _opacity = 1.0f;

    private void Start()
    {
        _sprite = GetComponent<tk2dSprite>();
    }
    
    private void Update()
    {
        transform.position += _velocity * Time.deltaTime;

        if (_opacity > 0) _opacity -= Time.deltaTime;
        _sprite.color = new Color(1, 1, 1, _opacity);
    }

    public void SetVelocity(Vector3 v)
    {
        _velocity = v;
        _opacity = 1.0f;
    }
}