using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MotionTracker
{
    /// <summary>
    /// Класс для конфигурации жестов для определённого состояния игрока.
    /// В каждом состоянии назначаются жесты (представленные файлом, путь которого является уникальным ключом)
    /// и соответствующее им действие (Action). При необходимости GestureData загружается с помощью метода LoadGesture.
    /// Используются два словаря:
    ///  - gestureMappings: ключ – путь (string), значение – GestureMappingEntry (GestureData и Action)
    ///  - dataToPath: ключ – GestureData, значение – путь (string)
    /// </summary>
    public class PlayerStateGestures
    {   
        /// <summary>
        /// Структура для хранения данных о жесте: GestureData и привязанное действие.
        /// </summary>
        public struct GestureMappingEntry
        {
            public GestureData Data;
            public Action Action;

            public GestureMappingEntry(GestureData data, Action action)
            {
                Data = data;
                Action = action;
            }
        }
        
        /// <summary>
        /// Название состояния, например "Interface", "Mage" или "Game".
        /// </summary>
        public string StateName { get; private set; }

        /// <summary>
        /// Словарь, где ключ – путь до файла жеста, значение – структура (GestureData и Action).
        /// </summary>
        private Dictionary<string, GestureMappingEntry> _gestureMappings = new Dictionary<string, GestureMappingEntry>();

        /// <summary>
        /// Словарь, где ключ – GestureData, значение – путь до файла жеста.
        /// </summary>
        private Dictionary<GestureData, string> dataToPath = new Dictionary<GestureData, string>();

        /// <summary>
        /// Конструктор, принимающий название состояния.
        /// </summary>
        public PlayerStateGestures(string stateName)
        {
            StateName = stateName;
        }

        /// <summary>
        /// Конструктор, принимающий название состояния и начальную конфигурацию мэппингов.
        /// Параметр initialMappings – словарь, где ключ – путь до файла жеста, значение – действие.
        /// При вызове конструктора GestureData загружается из файла.
        /// </summary>
        public PlayerStateGestures(string stateName, Dictionary<string, Action> initialMappings)
        {
            StateName = stateName;
            foreach (var kvp in initialMappings)
            {
                AddOrUpdateGestureMapping(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Добавляет или обновляет мэппинг жеста по указанному пути.
        /// GestureData загружается с помощью LoadGesture.
        /// </summary>
        /// <param name="gestureFilePath">Путь до файла жеста.</param>
        /// <param name="action">Действие, которое будет вызвано при распознавании.</param>
        public void AddOrUpdateGestureMapping(string gestureFilePath, Action action)
        {
            if (string.IsNullOrEmpty(gestureFilePath))
            {
                Debug.LogWarning("Путь к файлу жеста не задан.");
                return;
            }

            if (action == null)
            {
                Debug.LogWarning("Действие не задано для жеста: " + gestureFilePath);
                return;
            }

            // Если по данному пути запись уже существует, просто обновляем действие.
            if (_gestureMappings.TryGetValue(gestureFilePath, out GestureMappingEntry existingEntry))
            {
                existingEntry.Action = action;
                _gestureMappings[gestureFilePath] = existingEntry;
                Debug.Log($"Обновили действие для жеста {gestureFilePath}");
                return;
            }

            // Если записи нет, загружаем GestureData из файла и создаём новую запись.
            GestureData data = FileManagerGestureData.LoadGesture(gestureFilePath);
            if (data == null)
            {
                Debug.LogWarning("Не удалось загрузить GestureData из: " + gestureFilePath);
                return;
            }

            dataToPath.Add(data, gestureFilePath);
            GestureMappingEntry entry = new GestureMappingEntry(data, action);
            _gestureMappings[gestureFilePath] = entry;
            Debug.Log($"Добавили новый жест {gestureFilePath}");
        }

        /// <summary>
        /// Удаляет мэппинг жеста по указанному пути.
        /// </summary>
        /// <param name="gestureFilePath">Путь до файла жеста.</param>
        /// <returns>True, если удаление прошло успешно, иначе false.</returns>
        public bool RemoveGestureMapping(string gestureFilePath)
        {
            if (_gestureMappings.TryGetValue(gestureFilePath, out GestureMappingEntry entry))
            {
                _gestureMappings.Remove(gestureFilePath);
                dataToPath.Remove(entry.Data);
                return true;
            }

            Debug.LogWarning("Мэппинг для файла " + gestureFilePath + " не найден.");
            return false;
        }

        /// <summary>
        /// Возвращает массив всех GestureData из словаря dataToPath.
        /// </summary>
        public GestureData[] GetAllGestureDataArray()
        {
            GestureData[] array = new GestureData[dataToPath.Count];
            int index = 0;
            foreach (var kvp in dataToPath)
            {
                array[index++] = kvp.Key;
            }

            return array;
        }

        /// <summary>
        /// Выполняет действие, связанное с указанным файлом жеста.
        /// </summary>
        public void InvokeGestureAction(GestureData gestureData)
        {
            string gestureFilePath = dataToPath[gestureData];
            Action action = _gestureMappings[gestureFilePath].Action;
            if (action != null)
            {
                action.Invoke();
                return;
            }

            Debug.LogWarning("Нет действия для файла " + gestureFilePath);
        }

        /// <summary>
        /// Выводит в консоль все текущие мэппинги жестов.
        /// </summary>
        public void PrintMappings()
        {
            Debug.Log($"Состояние: {StateName}");
            foreach (var kvp in _gestureMappings)
            {
                string gestureInfo = kvp.Value.Data != null ? kvp.Value.Data.GestureName : "не загружено";
                Debug.Log($"Файл: {kvp.Key} -> Действие: {kvp.Value.Action.Method.Name}, GestureData: {gestureInfo}");
            }
        }
    }
}