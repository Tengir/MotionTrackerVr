using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MotionTracker
{
    public class GestureRecorder : MonoBehaviour
    {
        [Header("Настройки визуализации")] [SerializeField]
        private bool visualizeGesture = true;

        //[SerializeField] private float displayDuration = 3f; // Время отображения итогового жеста
        [SerializeField] private float recordInterval = 1f / 60f; // Частота выборки

        // Объект XR Origin (или другой родительский объект) для преобразования координат.
        [SerializeField] private Transform referenceTransform;

        // Словари для хранения состояния записи по Transform
        private Dictionary<Transform, bool> _isRecordingDict = new Dictionary<Transform, bool>();
        private Dictionary<Transform, List<float>> _rawPointsDict = new Dictionary<Transform, List<float>>();

        private Dictionary<Transform, (Vector3, Quaternion)> _initialPoseDict =
            new Dictionary<Transform, (Vector3, Quaternion)>();

        private Dictionary<Transform, LineRenderer> _lineRenderersDict = new Dictionary<Transform, LineRenderer>();

        private float _recordTimer = 0f;

        /// <summary>
        /// Инициализация: можно создать LineRenderer для каждого устройства по запросу.
        /// Здесь мы не создаём их заранее, а при начале записи.
        /// </summary>
        private void Awake()
        {
            // Если referenceTransform не установлен, используем текущий transform
            if (referenceTransform == null)
                referenceTransform = this.transform;
        }

        /// <summary>
        /// Начинает запись жеста для переданного объекта Transform.
        /// Сбрасывает предыдущие данные, сохраняет начальную позу и создаёт (или сбрасывает) LineRenderer.
        /// </summary>
        public void BeginGestureRecord(Transform recTransform)
        {
            if (recTransform == null)
            {
                Debug.LogWarning("Передан null для записи жеста.");
                return;
            }

            if (_isRecordingDict.ContainsKey(recTransform) && _isRecordingDict[recTransform])
            {
                Debug.LogWarning($"Transform {recTransform.name} already recording.");
                return;
            }

            _isRecordingDict[recTransform] = true;
            if (!_rawPointsDict.ContainsKey(recTransform))
                _rawPointsDict[recTransform] = new List<float>();
            else
                _rawPointsDict[recTransform].Clear();

            Vector3 initialPos = referenceTransform.position;
            Vector3 forward = Vector3.ProjectOnPlane(referenceTransform.forward, Vector3.up).normalized;
            Quaternion fixedRotation = Quaternion.LookRotation(forward, Vector3.up);
            _initialPoseDict[recTransform] = (initialPos, fixedRotation);
            
            // Создаем или сбрасываем LineRenderer для данного Transform
            if (!_lineRenderersDict.ContainsKey(recTransform))
            {
                GameObject lrObj = new GameObject("LineRenderer_" + recTransform.name);
                lrObj.transform.parent = this.transform;
                LineRenderer lr = lrObj.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.material = new Material(Shader.Find("Unlit/Color"));
                lr.material.color = Color.red;
                lr.positionCount = 0;
                lr.widthMultiplier = 0.01f;
                _lineRenderersDict[recTransform] = lr;
            }
            else
            {
                _lineRenderersDict[recTransform].positionCount = 0;
            }

            _recordTimer = 0f;
            Debug.Log($"[{recTransform.name}] Gesture recording started.");
        }

        /// <summary>
        /// Завершает запись жеста для переданного Transform.
        /// Преобразует записанные данные и возвращает GestureData.
        /// Также запускает корутину для очистки линии.
        /// </summary>
        public GestureData EndGestureRecord(Transform recTransform, string gestureName)
        {
            if (recTransform == null || !_isRecordingDict.ContainsKey(recTransform) || !_isRecordingDict[recTransform])
            {
                Debug.LogWarning($"[{recTransform?.name}] Not recording gesture, cannot end.");
                return null;
            }

            _isRecordingDict[recTransform] = false;
            float[] pointsArray = _rawPointsDict[recTransform].ToArray();
            // Если нужно, можно нормализовать pointsArray здесь

            // Обновляем визуализацию финальной линии
            if (visualizeGesture && _lineRenderersDict.TryGetValue(recTransform, out LineRenderer lr))
            {
                int countPoints = pointsArray.Length / 3;
                lr.positionCount = countPoints;
                // Преобразуем локальные точки относительно сохраненной начальной позы в мировые координаты
                (Vector3 refPos, Quaternion refRot) = _initialPoseDict[recTransform];
                for (int i = 0; i < countPoints; i++)
                {
                    Vector3 localPoint =
                        new Vector3(pointsArray[i * 3], pointsArray[i * 3 + 1], pointsArray[i * 3 + 2]);
                    Vector3 worldPoint = refPos + refRot * localPoint;
                    lr.SetPosition(i, worldPoint);
                }

                //StartCoroutine(ClearLineRendererAfterDelay(displayDuration, recTransform));
                ClearLineRenderer(recTransform);
            }

            GestureData gesture = new GestureData(gestureName, pointsArray);
            Debug.Log(
                $"[{recTransform.name}] Gesture recording ended. Total {pointsArray.Length / 3} points. Name: {gestureName}");
            return QuantizeGestureByStep(ScaleByMaxAxis(TranslateToOrigin(gesture)));
        }

        /// <summary>
        /// В Update() обновляем запись для каждого Transform, для которого запись активна.
        /// Считываем позицию из переданного Transform и обновляем соответствующий LineRenderer.
        /// </summary>
        private void Update()
        {
            _recordTimer += Time.deltaTime;
            if (_recordTimer < recordInterval)
                return;
            _recordTimer = 0f;

            foreach (var kvp in _isRecordingDict)
            {
                Transform recTransform = kvp.Key;
                if (!kvp.Value)
                    continue;

                Vector3 currentPos = recTransform.position;

                // Получаем сохранённую позицию и "выпрямленную" ориентацию головы/игрока
                (Vector3 refPos, Quaternion fixedRotation) = _initialPoseDict[recTransform];

                // Вычисляем локальную позицию в системе координат игрока (без yaw и roll)
                Vector3 localPos = Quaternion.Inverse(fixedRotation) * (currentPos - refPos);

                _rawPointsDict[recTransform].Add(localPos.x);
                _rawPointsDict[recTransform].Add(localPos.y);
                _rawPointsDict[recTransform].Add(localPos.z);

                // Визуализация
                if (visualizeGesture && _lineRenderersDict.TryGetValue(recTransform, out LineRenderer lr))
                {
                    int posCount = lr.positionCount;
                    lr.positionCount = posCount + 1;
                    Vector3 worldPos = refPos + fixedRotation * localPos;
                    lr.SetPosition(posCount, worldPos);
                }
            }
        }

        private void ClearLineRenderer(Transform recTransform)
        {
            if (_lineRenderersDict.TryGetValue(recTransform, out LineRenderer lr))
            {
                lr.positionCount = 0;
            }
        }

        /// <summary>
        /// Переносит жест так, чтобы его первая точка стала (0,0,0).
        /// Вычитает координаты первой точки из всех остальных.
        /// </summary>
        private static GestureData TranslateToOrigin(GestureData gesture)
        {
            int countPoints = gesture.Points.Length / 3;
            if (countPoints == 0) return gesture;
            float offsetX = gesture.Points[0];
            float offsetY = gesture.Points[1];
            float offsetZ = gesture.Points[2];

            float[] newPoints = new float[gesture.Points.Length];
            for (int i = 0; i < countPoints; i++)
            {
                newPoints[i * 3] = gesture.Points[i * 3] - offsetX;
                newPoints[i * 3 + 1] = gesture.Points[i * 3 + 1] - offsetY;
                newPoints[i * 3 + 2] = gesture.Points[i * 3 + 2] - offsetZ;
            }

            return new GestureData(gesture.GestureName, newPoints);
        }

        /// <summary>
        /// Масштабирует жест так, чтобы размах по самой длинной оси стал равен 1.
        /// Находит минимальные и максимальные значения по X, Y, Z и выбирает максимальный диапазон.
        /// </summary>
        private static GestureData ScaleByMaxAxis(GestureData gesture)
        {
            int countPoints = gesture.Points.Length / 3;
            if (countPoints == 0) return gesture;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            // Определяем диапазоны по осям
            for (int i = 0; i < countPoints; i++)
            {
                float x = gesture.Points[i * 3];
                float y = gesture.Points[i * 3 + 1];
                float z = gesture.Points[i * 3 + 2];

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                if (z < minZ) minZ = z;
                if (z > maxZ) maxZ = z;
            }

            // Находим самую длинную ось
            float widthX = maxX - minX;
            float widthY = maxY - minY;
            float widthZ = maxZ - minZ;

            float maxWidth = Math.Max(widthX, Math.Max(widthY, widthZ));

            if (Math.Abs(maxWidth) < 1e-6) maxWidth = 1f; // Защита от деления на ноль
            float scale = 1f / maxWidth;

            // Масштабируем все точки
            float[] newPoints = new float[gesture.Points.Length];
            for (int i = 0; i < gesture.Points.Length; i++)
            {
                newPoints[i] = gesture.Points[i] * scale;
            }

            return new GestureData(gesture.GestureName, newPoints);
        }

        /// <summary>
        /// Квантует жест, добавляя промежуточные точки, так чтобы каждая точка имела координаты, кратные step.
        /// Начинается с (0,0,0) и для каждой исходной точки жеста добавляются все промежуточные сеточные точки.
        /// </summary>
        /// <param name="gesture">Исходный жест</param>
        /// <param name="step">Шаг квантования, например 0.02</param>
        /// <returns>Новый GestureData с квантуемыми точками</returns>
        public static GestureData QuantizeGestureByStep(GestureData gesture)
        {
            float step = 0.02f;

            int countPoints = gesture.Points.Length / 3;
            if (countPoints == 0)
                return gesture;

            List<Vector3> output = new List<Vector3>();
            // Начинаем с (0,0,0)
            Vector3 prev = Vector3.zero;
            output.Add(prev);

            // Для каждой исходной точки
            for (int i = 0; i < countPoints; i++)
            {
                float x = gesture.Points[i * 3];
                float y = gesture.Points[i * 3 + 1];
                float z = gesture.Points[i * 3 + 2];
                Vector3 P = new Vector3(x, y, z);

                // Округляем каждую координату до ближайшего кратного step
                Vector3 Q = new Vector3(
                    Convert.ToInt32(P.x / step) * step,
                    Convert.ToInt32(P.y / step) * step,
                    Convert.ToInt32(P.z / step) * step
                );

                // Если Q совпадает с предыдущей, пропускаем
                if (Q == prev)
                    continue;

                // Переводим точки в целочисленный формат (делим на step)
                Vector3Int prevInt = new Vector3Int(
                    Convert.ToInt32(prev.x / step),
                    Convert.ToInt32(prev.y / step),
                    Convert.ToInt32(prev.z / step)
                );
                Vector3Int qInt = new Vector3Int(
                    Convert.ToInt32(Q.x / step),
                    Convert.ToInt32(Q.y / step),
                    Convert.ToInt32(Q.z / step)
                );

                // Находим промежуточные точки на линии между prevInt и qInt
                List<Vector3Int> gridPoints = Bresenham3D(prevInt, qInt);
                // Добавляем промежуточные точки (исключая первую, если она уже совпадает с prev)
                foreach (var gp in gridPoints)
                {
                    Vector3 pt = new Vector3(gp.x * step, gp.y * step, gp.z * step);
                    if (pt != output[output.Count - 1])
                        output.Add(pt);
                }

                prev = Q;
            }

            // Преобразуем список в массив float[]
            float[] newPoints = new float[output.Count * 3];
            for (int i = 0; i < output.Count; i++)
            {
                newPoints[i * 3] = output[i].x;
                newPoints[i * 3 + 1] = output[i].y;
                newPoints[i * 3 + 2] = output[i].z;
            }

            return new GestureData(gesture.GestureName, newPoints);
        }

        /// <summary>
        /// Реализует 3D Bresenham алгоритм для получения всех целочисленных точек между start и end.
        /// </summary>
        public static List<Vector3Int> Bresenham3D(Vector3Int start, Vector3Int end)
        {
            List<Vector3Int> points = new List<Vector3Int>();

            int x0 = start.x, y0 = start.y, z0 = start.z;
            int x1 = end.x, y1 = end.y, z1 = end.z;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int dz = Math.Abs(z1 - z0);

            int xs = (x1 > x0) ? 1 : -1;
            int ys = (y1 > y0) ? 1 : -1;
            int zs = (z1 > z0) ? 1 : -1;

            // Определяем доминирующую ось
            if (dx >= dy && dx >= dz)
            {
                int p1 = 2 * dy - dx;
                int p2 = 2 * dz - dx;
                while (x0 != x1)
                {
                    points.Add(new Vector3Int(x0, y0, z0));
                    if (p1 >= 0)
                    {
                        y0 += ys;
                        p1 -= 2 * dx;
                    }

                    if (p2 >= 0)
                    {
                        z0 += zs;
                        p2 -= 2 * dx;
                    }

                    p1 += 2 * dy;
                    p2 += 2 * dz;
                    x0 += xs;
                }
            }
            else if (dy >= dx && dy >= dz)
            {
                int p1 = 2 * dx - dy;
                int p2 = 2 * dz - dy;
                while (y0 != y1)
                {
                    points.Add(new Vector3Int(x0, y0, z0));
                    if (p1 >= 0)
                    {
                        x0 += xs;
                        p1 -= 2 * dy;
                    }

                    if (p2 >= 0)
                    {
                        z0 += zs;
                        p2 -= 2 * dy;
                    }

                    p1 += 2 * dx;
                    p2 += 2 * dz;
                    y0 += ys;
                }
            }
            else
            {
                int p1 = 2 * dy - dz;
                int p2 = 2 * dx - dz;
                while (z0 != z1)
                {
                    points.Add(new Vector3Int(x0, y0, z0));
                    if (p1 >= 0)
                    {
                        y0 += ys;
                        p1 -= 2 * dz;
                    }

                    if (p2 >= 0)
                    {
                        x0 += xs;
                        p2 -= 2 * dz;
                    }

                    p1 += 2 * dy;
                    p2 += 2 * dx;
                    z0 += zs;
                }
            }

            points.Add(new Vector3Int(x1, y1, z1));
            return points;
        }
    }
}