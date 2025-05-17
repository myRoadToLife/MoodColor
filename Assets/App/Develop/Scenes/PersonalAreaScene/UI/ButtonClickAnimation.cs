using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using App.Develop.Utils.Logging;
using Logger = App.Develop.Utils.Logging.Logger;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class ButtonClickAnimation : MonoBehaviour
    {
        [SerializeField] private float _animationDuration = 0.1f;
        [SerializeField] private float _animationScale = 0.95f;
        [SerializeField] private AnimationCurve _pressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _releaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private Button _button;
        private RectTransform _rect;
        private Vector3 _originalScale;
        
        private void Awake()
        {
            _button = GetComponent<Button>();
            _rect = GetComponent<RectTransform>();
            _originalScale = _rect.localScale;
            
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClick);
            }
        }
        
        private void OnButtonClick()
        {
            StartCoroutine(AnimateButton());
        }
        
        private IEnumerator AnimateButton()
        {
            // Анимация нажатия
            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                float t = elapsed / _animationDuration;
                float curveValue = _pressCurve.Evaluate(t);
                Vector3 targetScale = _originalScale * _animationScale;
                _rect.localScale = Vector3.Lerp(_originalScale, targetScale, curveValue);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Анимация возврата
            elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                float t = elapsed / _animationDuration;
                float curveValue = _releaseCurve.Evaluate(t);
                Vector3 startScale = _originalScale * _animationScale;
                _rect.localScale = Vector3.Lerp(startScale, _originalScale, curveValue);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            _rect.localScale = _originalScale;
        }
        
        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClick);
            }
        }
        
        // Для возможности программного запуска анимации
        public void PlayAnimation()
        {
            StartCoroutine(AnimateButton());
        }
        
        #if UNITY_EDITOR
        // Сбросить масштаб в редакторе, если что-то пошло не так
        [ContextMenu("Reset Scale")]
        private void ResetScale()
        {
            if (_rect == null)
                _rect = GetComponent<RectTransform>();
                
            _rect.localScale = Vector3.one;
            _originalScale = Vector3.one;
            Logger.Log($"[ButtonClickAnimation] Масштаб кнопки {gameObject.name} сброшен");
        }
        #endif
    }
} 