using System;
using System.IO;
using UnityEngine;

namespace MotionTracker
{
    public class GestureData
    {
        public readonly string GestureName;
        public readonly float[] Points; // Формат: x,y,z, x,y,z, ...

        public GestureData(string gestureName, float[] points)
        {
            GestureName = gestureName;
            Points = points;
        }
    }
}