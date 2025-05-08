using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    /// <summary>
    /// Компонент для анимации нажатия на кнопку.
    /// Применяет визуальный эффект масштабирования при нажатии и отпускании.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonClickAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Масштаб кнопки при нажатии")]
        [SerializeField] private float _pressedScale = 0.95f;
        
        [Tooltip("Скорость анимации нажатия")]
        [SerializeField] private float _animationSpeed = 15f;
        
        [Tooltip("Визуальный объект, который будет анимироваться")]
        [SerializeField] private RectTransform _targetTransform;

        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private bool _isAnimating = false;
        private bool _isPressed = false;

        private void Awake()
        {
            if (_targetTransform == null)
                _targetTransform = transform.GetChild(0).GetComponent<RectTransform>();
                
            _originalScale = _targetTransform.localScale;
            _targetScale = _originalScale;
        }

        private void Update()
        {
            if (_isAnimating)
            {
                // Плавно анимируем к целевому масштабу
                _targetTransform.localScale = Vector3.Lerp(
                    _targetTransform.localScale, 
                    _targetScale, 
                    Time.deltaTime * _animationSpeed);

                // Если достаточно близко к целевому масштабу, прекращаем анимацию
                if (Vector3.Distance(_targetTransform.localScale, _targetScale) < 0.001f)
                {
                    _targetTransform.localScale = _targetScale;
                    _isAnimating = false;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            _targetScale = _originalScale * _pressedScale;
            _isAnimating = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            _targetScale = _originalScale;
            _isAnimating = true;
        }

        /// <summary>
        /// Метод для программного запуска анимации нажатия
        /// </summary>
        public void PlayAnimation()
        {
            // Запускаем анимацию нажатия
            _isAnimating = true;
            _targetScale = _originalScale * _pressedScale;
            
            // С помощью Invoke возвращаемся к нормальному размеру через небольшую задержку
            Invoke(nameof(ResetScale), 0.1f);
        }

        private void ResetScale()
        {
            if (!_isPressed) // Только если кнопка больше не нажата
            {
                _targetScale = _originalScale;
                _isAnimating = true;
            }
        }

        private void OnValidate()
        {
            if (_targetTransform == null && transform.childCount > 0)
                _targetTransform = transform.GetChild(0).GetComponent<RectTransform>();
        }

#if UNITY_EDITOR
        // Метод для отладки
        [ContextMenu("Проверить анимацию")]
        private void TestAnimation()
        {
            PlayAnimation();
        }
#endif
    }
}