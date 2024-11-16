using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DevDunk.AnimatorLOD
{
    public class AnimatorLODManager : MonoBehaviour
    {
        private static AnimatorLODManager instance;
        public static AnimatorLODManager Instance
        {
            get { return instance; }
            private set
            {
                if (instance)
                {
                    Debug.LogWarning($"Old instance of AnimatorLOD was found on {instance.name}", value.gameObject);

                    //Set animators from previous instance to this instance
                    value.Animators = instance.Animators;
                    value.LODs = instance.LODs;

                    Destroy(instance);
                }
                instance = value;
            }
        }

        public bool IsRunning { get; private set; }
        [Tooltip("Camera transform to use for LOD calculations. If not set will use Camera.main")]
        public Transform cameraTransform;
        [Tooltip("If true, the LOD system will run at start. If false, start manually with EnableAnimatorLOD")]
        public bool RunAtStart = true;

        [Tooltip("LOD settings for the animators")]
        public LODSettings[] LODs = new LODSettings[]
        {
            new LODSettings { Distance = 10f, frameCount = 0, MaxBoneWeight = SkinQuality.Bone4 },
            new LODSettings { Distance = 20f, frameCount = 1, MaxBoneWeight = SkinQuality.Bone2  },
            new LODSettings { Distance = 30f, frameCount = 2, MaxBoneWeight = SkinQuality.Bone2  },
            new LODSettings { Distance = 40f, frameCount = 4, MaxBoneWeight = SkinQuality.Bone1  },
        };

        private List<AnimatorLODObject> Animators = new List<AnimatorLODObject>();
        private NativeArray<float3> animatorPositions;
        private NativeArray<int> frameCounts;
        private NativeArray<SkinQuality> qualities;

        private void Awake()
        {
            Instance = this;
            IsRunning = RunAtStart;
        }

        private void Start()
        {
            System.Array.Sort(LODs, (a, b) => a.Distance.CompareTo(b.Distance));
            cameraTransform = Camera.main?.transform;
        }

        private void OnDestroy()
        {
            if (animatorPositions.IsCreated) animatorPositions.Dispose();
            if (frameCounts.IsCreated) frameCounts.Dispose();
            if (qualities.IsCreated) qualities.Dispose();
        }

        public void AddAnimator(AnimatorLODObject animator)
        {
            Animators.Add(animator);
        }

        public void RemoveAnimator(AnimatorLODObject animator)
        {
            Animators.Remove(animator);
        }

        public void DisableAnimatorLOD()
        {
            if (!IsRunning) return;

            foreach (var animator in Animators)
                animator.DisableLODSystem();

            IsRunning = false;
        }

        public void EnableAnimatorLOD()
        {
            if (IsRunning) return;

            foreach (var animator in Animators)
                animator.EnableLODSystem();

            IsRunning = true;
        }

        private void Update()
        {
            if (!IsRunning || !cameraTransform) return;

            int animatorCount = Animators.Count;
            if (animatorPositions.IsCreated) animatorPositions.Dispose();
            if (frameCounts.IsCreated) frameCounts.Dispose();
            if (qualities.IsCreated) qualities.Dispose();

            animatorPositions = new NativeArray<float3>(animatorCount, Allocator.TempJob);
            frameCounts = new NativeArray<int>(animatorCount, Allocator.TempJob);
            qualities = new NativeArray<SkinQuality>(animatorCount, Allocator.TempJob);

            for (int i = 0; i < animatorCount; i++)
                animatorPositions[i] = Animators[i].TrackedTransform.position;

            var lodJob = new CalculateLODJob
            {
                CameraPosition = cameraTransform.position,
                LODs = new NativeArray<LODSettings>(LODs, Allocator.TempJob),
                AnimatorPositions = animatorPositions,
                FrameCounts = frameCounts,
                Qualities = qualities
            };

            var lodJobHandle = lodJob.Schedule(animatorCount, 64);
            lodJobHandle.Complete();

            for (int i = 0; i < animatorCount; i++)
            {
                if (Animators[i].FrameCountdown <= 0)
                {
                    Animators[i].EnableAnimator(frameCounts[i], frameCounts[i] + 1, qualities[i]);
                }
                else
                {
                    Animators[i].DisableAnimator();
                }
            }

            lodJob.LODs.Dispose();
        }

        public void SetCameraTransform(Camera camera)
        {
            cameraTransform = camera.transform;
        }

        public void SetCameraTransform(Transform camera)
        {
            cameraTransform = camera;
        }
    }

    [BurstCompile]
    public struct CalculateLODJob : IJobParallelFor
    {
        [ReadOnly] public float3 CameraPosition;
        [ReadOnly] public NativeArray<float3> AnimatorPositions;
        [ReadOnly] public NativeArray<LODSettings> LODs;

        [WriteOnly] public NativeArray<int> FrameCounts;
        [WriteOnly] public NativeArray<SkinQuality> Qualities;

        public void Execute(int index)
        {
            float distance = math.distance(AnimatorPositions[index], CameraPosition);
            int closestIndex = 0;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < LODs.Length; i++)
            {
                float diff = math.abs(LODs[i].Distance - distance);
                if (diff < closestDistance)
                {
                    closestIndex = i;
                    closestDistance = diff;
                }
            }

            FrameCounts[index] = LODs[closestIndex].frameCount;
            Qualities[index] = LODs[closestIndex].MaxBoneWeight;
        }
    }
}
