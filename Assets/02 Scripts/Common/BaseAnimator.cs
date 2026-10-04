using UnityEngine;

namespace Project_Chronicles.Common
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BaseAnimator : MonoBehaviour
    {
        protected Animator _animator;
        protected SpriteRenderer _spriteRenderer;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}