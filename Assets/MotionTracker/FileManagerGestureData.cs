using System;
using System.IO;
using UnityEngine;

namespace MotionTracker
{
    public static class FileManagerGestureData
    {
        /// <summary>
        /// Сохраняет жест в бинарном формате.
        /// </summary>
        public static void SaveGestureBinary(GestureData gesture, string folderPath)
        {
            string filePath = Path.Combine(folderPath, gesture.GestureName);
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write(gesture.GestureName);
                writer.Write(gesture.Points.Length);
                for (int i = 0; i < gesture.Points.Length; i++)
                {
                    writer.Write(gesture.Points[i]);
                }
            }
        }

        /// <summary>
        /// Загружает один жест из файла.
        /// Формат файла: сначала строка с именем жеста, затем целое число (количество точек), затем последовательность float.
        /// </summary>
        public static GestureData LoadGesture(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    string gestureName = reader.ReadString();
                    int length = reader.ReadInt32();
                    float[] points = new float[length];
                    for (int i = 0; i < length; i++)
                    {
                        points[i] = reader.ReadSingle();
                    }

                    return new GestureData(gestureName, points);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Ошибка при загрузке " + filePath + ": " + ex.Message);
                return null;
            }
        }
    }
}