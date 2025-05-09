using UnityEngine;
using UnityEngine.EventSystems;

namespace App.Develop.UI.Shared
{
    // Простой компонент для эффекта нажатия на кнопку
    public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Vector3 OriginalScale;
        public Vector3 PressedScale = new Vector3(0.95f, 0.95f, 1f);
        // public GameObject ShadowObject; // Для управления тенью

        private RectTransform _rectTransform;
        private bool _isPressed = false;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            // Сохраняем начальный скейл, если он не был установлен извне и OriginalScale не задан в инспекторе/коде
            if (OriginalScale == Vector3.zero) 
            {
                OriginalScale = _rectTransform.localScale;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_rectTransform == null) Awake(); // На случай если Awake не был вызван (например, объект был неактивен)
            _rectTransform.localScale = PressedScale;
            _isPressed = true;
            // if (ShadowObject != null) ShadowObject.SetActive(true); // Показать тень
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isPressed)
            {
                if (_rectTransform == null) Awake();
                _rectTransform.localScale = OriginalScale;
                _isPressed = false;
                // if (ShadowObject != null) ShadowObject.SetActive(false); // Скрыть тень
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Если палец ушел с кнопки, не отпуская, возвращаем скейл
             if (_isPressed)
            {
                if (_rectTransform == null) Awake();
                _rectTransform.localScale = OriginalScale;
                _isPressed = false;
                // if (ShadowObject != null) ShadowObject.SetActive(false);
            }
        }
    }
} 