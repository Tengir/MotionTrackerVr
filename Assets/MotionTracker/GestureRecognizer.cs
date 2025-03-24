using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MotionTracker
{
    /// <summary>
    /// Класс реализует алгоритм распознавания жестов на основе ресемплирования и расчёта средней евклидовой дистанции между точками.
    /// </summary>
    public class GestureRecognizer : MonoBehaviour
    {
        public bool UseResampling { get; set; } = true;
        public int TargetPointCount { get; set; } = 32;
        public float SimilarityThreshold { get; set; } = 0.7f;


        /// <summary>
        /// Ресемплирует массив точек жеста до заданного количества точек.
        /// Входной массив имеет вид: [x0,y0,z0, x1,y1,z1, ...]
        /// </summary>
        private float[] ResampleGesture(float[] points)
        {
            int n = points.Length / 3;
            if (n < 2) return points;

            // Вычисляем общую длину пути
            float totalLength = 0f;
            Vector3 prev = new Vector3(points[0], points[1], points[2]);
            for (int i = 1; i < n; i++)
            {
                Vector3 current = new Vector3(points[i * 3], points[i * 3 + 1], points[i * 3 + 2]);
                totalLength += Vector3.Distance(prev, current);
                prev = current;
            }

            float interval = totalLength / (TargetPointCount - 1);
            List<Vector3> resampled = new List<Vector3>();
            resampled.Add(new Vector3(points[0], points[1], points[2]));
            float distanceSoFar = 0f;
            prev = new Vector3(points[0], points[1], points[2]);
            int index = 1;
            while (index < n)
            {
                Vector3 current = new Vector3(points[index * 3], points[index * 3 + 1], points[index * 3 + 2]);
                float d = Vector3.Distance(prev, current);
                if (distanceSoFar + d >= interval)
                {
                    float t = (interval - distanceSoFar) / d;
                    Vector3 newPoint = Vector3.Lerp(prev, current, t);
                    resampled.Add(newPoint);
                    prev = newPoint;
                    distanceSoFar = 0f;
                }
                else
                {
                    distanceSoFar += d;
                    prev = current;
                    index++;
                }
            }

            while (resampled.Count < TargetPointCount)
            {
                resampled.Add(new Vector3(points[(n - 1) * 3], points[(n - 1) * 3 + 1], points[(n - 1) * 3 + 2]));
            }

            float[] result = new float[resampled.Count * 3];
            for (int i = 0; i < resampled.Count; i++)
            {
                result[i * 3] = resampled[i].x;
                result[i * 3 + 1] = resampled[i].y;
                result[i * 3 + 2] = resampled[i].z;
            }

            return result;
        }


        public float GetSimilarityConfidence(GestureData gestureA, GestureData gestureB)
        {
            float[] aPoints = gestureA.Points;
            float[] bPoints = gestureB.Points;
            if (UseResampling)
            {
                aPoints = ResampleGesture(aPoints);
                bPoints = ResampleGesture(bPoints);
            }

            int nA = aPoints.Length / 3;
            int nB = bPoints.Length / 3;
            int count = Math.Min(nA, nB);
            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 a = new Vector3(aPoints[i * 3], aPoints[i * 3 + 1], aPoints[i * 3 + 2]);
                Vector3 b = new Vector3(bPoints[i * 3], bPoints[i * 3 + 1], bPoints[i * 3 + 2]);
                total += Vector3.Distance(a, b);
            }

            float avgDistance = total / count;
            float confidence = 1f - (avgDistance / SimilarityThreshold);
            if (confidence < 0f) confidence = 0f;
            if (confidence > 1f) confidence = 1f;
            return confidence;
        }

        /// <summary>
        /// Метод для нахождения наиболее схожего жеста из словаря с жестами "gestures".
        /// </summary>
        /// <param name="recordedGesture">Жест которому надо найти наиболее сходный.</param>
        /// <param name="gestures"> Массив жестов, среди которых надо найти похожий.</param>
        /// <returns>Наиболее сходный GestureData из "gestures".</returns>
        public GestureData GetGesture(GestureData recordedGesture, GestureData[] gestures)
        {
            if (recordedGesture == null)
            {
                return null;
            }

            GestureData similarlyGestureData = null;
            float maxConfidence = float.MinValue;

            foreach (GestureData baseGesture in gestures)
            {
                if (baseGesture == null)
                {
                    continue;
                }

                float conf = GetSimilarityConfidence(recordedGesture, baseGesture);
                if (conf > maxConfidence)
                {
                    maxConfidence = conf;
                    similarlyGestureData = baseGesture;
                }
            }

            return similarlyGestureData;
        }
    }
}