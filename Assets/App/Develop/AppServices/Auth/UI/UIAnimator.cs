using System.Collections;
using UnityEngine;

namespace App.Develop.AppServices.Auth.UI
{
    public class UIAnimator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeTime = 0.3f;
        
        private Coroutine _fadeCoroutine;
        
        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }
        
        public void Show()
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
                
            _fadeCoroutine = StartCoroutine(FadeIn());
        }
        
        public void Hide()
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            
            if (!gameObject.activeSelf)
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0;
                    _canvasGroup.interactable = false;
                    _canvasGroup.blocksRaycasts = false;
                }
                return;
            }
                
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
        
        public void SetHiddenState()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
                
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
        
        public void SetVisibleState()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
                
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
    }
}