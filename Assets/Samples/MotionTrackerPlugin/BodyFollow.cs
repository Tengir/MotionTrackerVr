using UnityEngine;
using UnityEngine.InputSystem;

public class BodyFollow : MonoBehaviour
{
    [Tooltip("Ссылка на объект головы, который движется с помощью Tracked Pose Driver")]
    [SerializeField] private Transform head;

    [Tooltip("Ожидаемый вертикальный оффсет для головы относительно тела (например, 1.6 м)")]
    [SerializeField] private float headYOffset = 1.6f;

    [Tooltip("Скорость сглаживания движения тела")]
    [SerializeField] private float followSpeed = 5f;

    void Update()
    {
        if (head != null)
        {
            // Получаем глобальную позицию головы
            Vector3 headGlobalPos = head.position;
            // Целевая позиция тела: берем X и Z от головы, Y фиксированную (уровень земли)
            Vector3 targetBodyPos = new Vector3(headGlobalPos.x, headGlobalPos.y - headYOffset, headGlobalPos.z);
            // Плавно перемещаем тело к целевой позиции
            transform.position = Vector3.Lerp(transform.position, targetBodyPos, followSpeed * Time.deltaTime);
        }
    }
}
