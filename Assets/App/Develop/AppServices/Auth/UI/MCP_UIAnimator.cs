using System.Collections;
using UnityEngine;

namespace App.Develop.AppServices.Auth.UI
{
    /// <summary>
    /// Аниматор UI-панелей, который позволяет плавно показывать и скрывать панели через CanvasGroup
    /// </summary>
    public class MCP_UIAnimator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeTime = 0.3f;
        
        private Coroutine _fadeCoroutine;
        
        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
                
            if (_canvasGroup == null)
                Debug.LogError($"MCP_UIAnimator на {gameObject.name} не имеет CanvasGroup!");
        }
        
        /// <summary>
        /// Показать панель с анимацией появления
        /// </summary>
        public void ShowPanel()
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
                
            _fadeCoroutine = StartCoroutine(FadeIn());
        }
        
        /// <summary>
        /// Скрыть панель с анимацией исчезновения
        /// </summary>
        public void HidePanel()
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
                
            _fadeCoroutine = StartCoroutine(FadeOut());
        }
        
        private IEnumerator FadeIn()
        {
            gameObject.SetActive(true);
            
            float time = 0;
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            
            while (time < _fadeTime)
            {
                time += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(time / _fadeTime);
                yield return null;
            }
            
            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _fadeCoroutine = null;
        }
        
        private IEnumerator FadeOut()
        {
            float time = _fadeTime;
            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            
            while (time > 0)
            {
                time -= Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(time / _fadeTime);
                yield return null;
            }
            
            _canvasGroup.alpha = 0;
            gameObject.SetActive(false);
            _fadeCoroutine = null;
        }
    }
}