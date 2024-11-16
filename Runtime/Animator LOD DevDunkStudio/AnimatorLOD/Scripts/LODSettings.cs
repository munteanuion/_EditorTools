using UnityEngine;

namespace DevDunk.AnimatorLOD
{
    [System.Serializable]
    public struct LODSettings
    {
        public float Distance;
        public int frameCount;
        public SkinQuality MaxBoneWeight;
    }
}