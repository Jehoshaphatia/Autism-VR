using System.Collections.Generic;
using UnityEngine;

namespace AutismVR.Comfort
{
    // What the detectors see in one frame. Hands lists tracked hands only; LoudnessDbfs is -infinity with no mic.
    public struct SensorFrame
    {
        public Vector3 HeadPosition;
        public Vector3 HeadForward;
        public List<Vector3> Hands;
        public Vector3 ChildHead;
        public Vector3 ChildChest;
        public Vector3 ChildPosition;
        public float LoudnessDbfs;
    }
}
