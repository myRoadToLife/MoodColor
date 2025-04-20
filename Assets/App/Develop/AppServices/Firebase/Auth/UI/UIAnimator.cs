using System.Collections;
using UnityEngine;

namespace App.Develop.AppServices.Auth.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIAnimator : MonoBehaviour
    {
        [SerializeField] private float _fadeDuration = 0.3f;

        private CanvasGroup _canvasGroup;
        private Coroutine _fadeCoroutine;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Show()
        {
            // Гарантируем включение объекта
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            StartFade(1f, true);
        }

        public void Hide()
        {
            // Гарантируем включение объекта, чтобы coroutine не крашился
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            StartFade(0f, false);
        }

        private void StartFade(float targetAlpha, bool enableInteraction)
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeTo(targetAlpha, enableInteraction));
        }

        private IEnumerator FadeTo(float targetAlpha, bool enableInteraction)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            _canvasGroup.interactable = enableInteraction;
            _canvasGroup.blocksRaycasts = enableInteraction;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _fadeDuration);
                _canvasGroup.alpha = alpha;
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }

        public bool IsVisible => _canvasGroup.alpha > 0.95f;
    }
}
