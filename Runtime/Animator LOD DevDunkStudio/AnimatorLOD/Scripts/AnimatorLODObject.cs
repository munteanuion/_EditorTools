using UnityEngine;

namespace DevDunk.AnimatorLOD
{
    [DefaultExecutionOrder(1)]
    public class AnimatorLODObject : MonoBehaviour
    {
        public bool ChangeSkinWeights = true;
        public Animator TrackedAnimatorComponent;
        public Transform TrackedTransform;
        public SkinnedMeshRenderer[] SkinnedMeshRenderers;
        [System.NonSerialized] public int FrameCountdown;

        private bool currentState;
        private float currentSpeed;
        private SkinQuality currentQuality;

        void Awake()
        {
            if (!TrackedAnimatorComponent) TrackedAnimatorComponent = GetComponent<Animator>();
            if (!TrackedTransform) TrackedTransform = transform;
            if (SkinnedMeshRenderers == null || SkinnedMeshRenderers.Length == 0)
                SkinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

            FrameCountdown = Random.Range(0, 5);
        }

        private void OnEnable()
        {
            AnimatorLODManager.Instance.AddAnimator(this);
        }

        private void OnDisable()
        {
            AnimatorLODManager.Instance.RemoveAnimator(this);
        }

        public void DisableLODSystem()
        {
            TrackedAnimatorComponent.enabled = true;
            currentState = true;
            TrackedAnimatorComponent.speed = 1.0f;
            currentSpeed = 1.0f;
        }

        public void EnableLODSystem(int maxValue = 5)
        {
            FrameCountdown = Random.Range(0, maxValue);
        }

        public void EnableAnimator(int newFrameCount, int speed, SkinQuality quality)
        {
            if (!currentState)
            {
                TrackedAnimatorComponent.enabled = true;
                currentState = true;
            }
            if (currentSpeed != speed)
            {
                TrackedAnimatorComponent.speed = speed;
                currentSpeed = speed;
            }

            FrameCountdown = newFrameCount;

            SetMeshQuality(quality);
        }

        public void DisableAnimator()
        {
            if (currentState)
            {
                TrackedAnimatorComponent.enabled = false;
                currentState = false;
            }

            FrameCountdown--;
        }

        public void SetMeshQuality(SkinQuality quality)
        {
            if (!ChangeSkinWeights || currentQuality == quality) return;

            for (int i = 0; i < SkinnedMeshRenderers.Length; i++)
            {
                SkinnedMeshRenderers[i].quality = quality;
                currentQuality = quality;
            }
        }
    }
}
