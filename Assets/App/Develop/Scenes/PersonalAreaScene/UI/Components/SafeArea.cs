using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    /// <summary>
    /// Компонент для обеспечения безопасной области UI в зависимости от устройства.
    /// Автоматически подстраивает размеры прямоугольника под безопасную зону экрана.
    /// </summary>
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _safeArea;
        private Vector2 _minAnchor;
        private Vector2 _maxAnchor;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _safeArea = Screen.safeArea;
            _minAnchor = _safeArea.position;
            _maxAnchor = _minAnchor + _safeArea.size;

            // Конвертируем в нормализованные координаты анкоров
            _minAnchor.x /= Screen.width;
            _minAnchor.y /= Screen.height;
            _maxAnchor.x /= Screen.width;
            _maxAnchor.y /= Screen.height;

            // Применяем значения
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            // Сохраняем текущие значения
            Vector2 anchorMin = _rectTransform.anchorMin;
            Vector2 anchorMax = _rectTransform.anchorMax;

            // Изменяем анкоры только если значения SafeArea отличаются от полного экрана
            if (_minAnchor.x > 0) anchorMin.x = _minAnchor.x;
            if (_minAnchor.y > 0) anchorMin.y = _minAnchor.y;
            if (_maxAnchor.x < 1) anchorMax.x = _maxAnchor.x;
            if (_maxAnchor.y < 1) anchorMax.y = _maxAnchor.y;

            // Установка новых значений
            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }

        // Для обновления в редакторе (если нужно)
        private void OnValidate()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            // В редакторе просто растягиваем на полный экран
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}