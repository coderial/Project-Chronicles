using UnityEngine;

namespace Project_Chronicles.Common
{
    [RequireComponent(typeof(Animator))]
    public class BaseAnimator : MonoBehaviour
    {
        protected Animator _animator;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
        }
    }
}