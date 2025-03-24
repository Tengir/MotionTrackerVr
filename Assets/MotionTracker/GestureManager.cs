using UnityEngine;

namespace MotionTracker
{
    /// <summary>
    /// Менеджер жестов, который управляет текущим состоянием, позволяет менять его и обрабатывает распознавание.
    /// Он использует объект GestureRecognizer для нахождения наиболее похожего жеста.
    /// </summary>
    public class GestureManager
    {
        private PlayerStateGestures _currentState;
        private GestureRecognizer _gestureRecognizer;

        /// <summary>
        /// Конструктор, принимающий начальное состояние и распознаватель жестов.
        /// </summary>
        public GestureManager(PlayerStateGestures initialState, GestureRecognizer recognizer)
        {
            _currentState = initialState;
            _gestureRecognizer = recognizer;
        }

        /// <summary>
        /// Меняет текущее состояние жестов.
        /// </summary>
        public void ChangeState(PlayerStateGestures newState)
        {
            _currentState = newState;
            Debug.Log("State changed to " + newState.StateName);
        }

        /// <summary>
        /// Обрабатывает записанный жест: сравнивает его с базовыми жестами из текущего состояния,
        /// получает наиболее похожий GestureData и выполняет привязанное к нему действие.
        /// </summary>
        public void ProcessRecordedGesture(GestureData recordedGesture)
        {
            if (recordedGesture == null)
            {
                Debug.LogWarning("Recorded gesture is null.");
                return;
            }
            // Получаем массив базовых жестов из текущего состояния.
            GestureData[] baseGestures = _currentState.GetAllGestureDataArray();
            // Используем распознаватель для поиска наиболее похожего жеста.
            GestureData bestMatch = _gestureRecognizer.GetGesture(recordedGesture, baseGestures);
            if (bestMatch != null)
            {   
                Debug.Log($"Жест распознан как {bestMatch.GestureName}.");
                _currentState.InvokeGestureAction(bestMatch);
            }
            else
            {
                Debug.LogWarning("Жест не распознан.");
            }
        }
    }
}
