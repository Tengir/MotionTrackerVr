using System;
using System.Collections.Generic;
using System.IO;
using MotionTracker;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Samples.MotionTrackerPlugin
{
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Ссылка на объект головы, который движется с помощью Tracked Pose Driver")] 
        [SerializeField] private Transform head;

        [Tooltip("Ссылка на объект CameraOffset, который задает относительное смещение камере")]
        [SerializeField] private Transform cameraOffset;

        [Tooltip("Ссылка на объект RightHandOffset, который задает относительное смещение правой руке")]
        [SerializeField] private Transform rightHandOffset;

        [Tooltip("Ссылка на объект LeftHandOffset, который задает относительное смещение левой руке")] 
        [SerializeField] private Transform leftHandOffset;

        [Tooltip("Ссылка на объект, относительно которого надо обновить высоту шлема")]
        [SerializeField] private Transform floor;

        [Tooltip("Высота головы по умолчанию")] 
        [SerializeField] private float defaultHeadY = 0.1f;

        [Header("Настройки перемещения")]
        [SerializeField] private float moveSpeed = 2f;

        // Ссылка на действие перемещения, настроенное в Input Action Asset
        [SerializeField] private InputActionReference moveAction;

        [SerializeField] private CharacterController characterController;

        [Header("Настройки жестов")]
        [SerializeField] private string savedGestureName = "new_gesture";
        [SerializeField] private string pathToSaveGesture;
        // Ссылка на наш компонент записи жестов
        [SerializeField] public GestureRecorder gestureRecorder;

        [SerializeField] public GestureRecognizer gestureRecognizer;

        [SerializeField] private Transform leftHandVisualize;
        [SerializeField] private Transform rightHandVisualize;
        
        [SerializeField] private TextMeshProUGUI gestureText;

        private Controls _controls;

        // Текущее состояние жестов
        private PlayerStateGestures _currentState;

        // Менеджер жестов
        private GestureManager _gestureManager;

        // Input Action для правого и левого контроллера (настраиваются в инспекторе через новую систему ввода)
        public InputAction recordRightGesture;
        public InputAction recordLeftGesture;
        public InputAction saveRightGesture;

        [Header("Reset Height Action")] [Tooltip("Input Action для сброса высоты головы")] [SerializeField]
        private InputActionProperty resetHeightAction;

        private void Awake()
        {
            Controls controls = new Controls();
            recordRightGesture = controls.Main.RecordRightGesture;
            recordLeftGesture = controls.Main.RecordLeftGesture;
            saveRightGesture = controls.Main.SaveRightGesture;
        }

        private void OnEnable()
        {
            // Подписываемся на события для правого контроллера
            recordRightGesture.performed += OnRecordPressedRight;
            recordRightGesture.canceled += OnRecordReleasedRight;

            // Подписываемся на события для левого контроллера
            recordLeftGesture.performed += OnRecordPressedLeft;
            recordLeftGesture.canceled += OnRecordReleasedLeft;
            
            saveRightGesture.performed += OnSavePressedRight;
            saveRightGesture.canceled += OnSaveReleasedRight;

            recordRightGesture.Enable();
            recordLeftGesture.Enable();
            saveRightGesture.Enable();

            resetHeightAction.action.performed += OnResetHeight;
            resetHeightAction.action.Enable();
        }

        private void OnDisable()
        {
            // Отписываемся от событий для правого контроллера
            recordRightGesture.performed -= OnRecordPressedRight;
            recordRightGesture.canceled -= OnRecordReleasedRight;

            // Отписываемся от событий для левого контроллера
            recordLeftGesture.performed -= OnRecordPressedLeft;
            recordLeftGesture.canceled -= OnRecordReleasedLeft;
            
            saveRightGesture.performed -= OnSavePressedRight;
            saveRightGesture.canceled -= OnSaveReleasedRight;
            
            recordRightGesture.Disable();
            recordLeftGesture.Disable();
            saveRightGesture.Disable();

            resetHeightAction.action.performed -= OnResetHeight;
            resetHeightAction.action.Disable();
        }

        private void OnRecordPressedRight(InputAction.CallbackContext context)
        {
            // Начинаем запись для правой руки
            gestureRecorder.BeginGestureRecord(rightHandVisualize);
            Debug.Log("Right gesture recording started.");
        }

        private void OnRecordReleasedRight(InputAction.CallbackContext context)
        {
            // Завершаем запись для правой руки и запускаем распознавание
            GestureData gesture = gestureRecorder.EndGestureRecord(rightHandVisualize, "RightRecordedGesture");
            Debug.Log("Right gesture recording ended.");
            RecognizeGesture(gesture);
        }

        private void OnRecordPressedLeft(InputAction.CallbackContext context)
        {
            // Начинаем запись для левой руки
            gestureRecorder.BeginGestureRecord(leftHandVisualize);
            Debug.Log("Left gesture recording started.");
        }

        private void OnRecordReleasedLeft(InputAction.CallbackContext context)
        {
            // Завершаем запись для левой руки и запускаем распознавание
            GestureData gesture = gestureRecorder.EndGestureRecord(leftHandVisualize, "LeftRecordedGesture");
            Debug.Log("Left gesture recording ended.");
            RecognizeGesture(gesture);
        }
        
        private void OnSavePressedRight(InputAction.CallbackContext context)
        {
            // Начинаем запись для правой руки.
            gestureRecorder.BeginGestureRecord(rightHandVisualize);
            Debug.Log("Right gesture save started.");
        }

        private void OnSaveReleasedRight(InputAction.CallbackContext context)
        {
            if (pathToSaveGesture is null)
            {
                Debug.LogWarning($"pathToSaveGesture {savedGestureName} is null");
                return;
            }
            // Завершаем запись для правой руки и сохраняем.
            GestureData gesture = gestureRecorder.EndGestureRecord(rightHandVisualize, savedGestureName);
            FileManagerGestureData.SaveGestureBinary(gesture, pathToSaveGesture);
            Debug.Log("Right gesture saved ended.");
        }

        private void OnResetHeight(InputAction.CallbackContext context)
        {
            float offsetY = floor.position.y + defaultHeadY- cameraOffset.position.y; // Величина на которую должны переместиться.
            cameraOffset.position =
                new Vector3(cameraOffset.position.x, cameraOffset.position.y + offsetY, cameraOffset.position.z); // Задаем высоту голове.
            rightHandOffset.position = new Vector3(rightHandOffset.position.x, rightHandOffset.position.y + offsetY,
                rightHandOffset.position.z);
            leftHandOffset.position = new Vector3(leftHandOffset.position.x, leftHandOffset.position.y + offsetY,
                leftHandOffset.position.z);
            Debug.Log("Reset height started.");
        }


        private void RecognizeGesture(GestureData gesture)
        {
            _gestureManager.ProcessRecordedGesture(gesture);
        }

        private void Start()
        {
            string baseGesturesFolder = @"Assets/Samples/MotionTrackerPlugin/SceneGestures";
            // Список названий жестов с первой заглавной буквой каждого слова
            string[] gestureNames = new string[]
            {
                "left_right",
                "right_left",
                "double_up_down",
                "letter_c"
            };

            // Создаем словарь мэппингов жестей: ключ – путь до файла, значение – действие (лямбда, которая выводит название жеста)
            Dictionary<string, Action> gestureMappings = new Dictionary<string, Action>();
            foreach (string gestureName in gestureNames)
            {
                // Формируем полный путь (например, "Assets/MyPlugin/SampleScene/SceneGestures/Line_Left_Right.bin")
                string filePath = Path.Combine(baseGesturesFolder, gestureName + ".bin");
                gestureMappings[filePath] = () =>
                {
                    Debug.Log("Executed gesture: " + gestureName);
                    if (gestureText != null)
                    {
                        gestureText.text = $"Gesture: {gestureName}";
                    }
                };
            }

            // Создаем состояние с именем "SceneGestures" и передаем в него словарь мэппингов
            _currentState = new PlayerStateGestures("SceneGestures", gestureMappings);

            // Предположим, что у нас уже есть класс GestureRecognizer, реализующий метод GetGesture.
            // Здесь мы используем его для создания менеджера жестов.
            _gestureManager = new GestureManager(_currentState, gestureRecognizer);

            Debug.Log("PlayerStateGestures и GestureManager успешно созданы.");
        }

        private void Update()
        {
            // Получаем значение движения со стика (Vector2)
            Vector2 input = moveAction.action.ReadValue<Vector2>();

            // Преобразуем его в 3D-вектор (движение по X и Z, без изменения Y)
            Vector3 move = new Vector3(input.x, 0, input.y);

            // Если хотим движение относительно направления камеры:
            if (cameraOffset != null)
            {
                // Берем направление камеры по горизонтали (игнорируем поворот по X и Z)
                Quaternion camRotation = Quaternion.Euler(0, cameraOffset.eulerAngles.y, 0);
                move = camRotation * move;
            }

            // Применяем движение. Если используем CharacterController:
            characterController.Move(move * (moveSpeed * Time.deltaTime));
        }
    }
}