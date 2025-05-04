using System.Collections;
using App.Develop.DI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.UI
{
    /// <summary>
    /// Компонент UI для отображения уведомления о повышении уровня
    /// </summary>
    public class LevelUpNotification : MonoBehaviour
    {
        #region Inspector Fields
        
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private float _showDuration = 5f;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _showAnimationTrigger = "Show";
        [SerializeField] private string _hideAnimationTrigger = "Hide";
        
        #endregion
        
        #region Private Fields
        
        private Coroutine _hideCoroutine;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Hide);
            }
            
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
        }
        
        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Hide);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Показывает уведомление о повышении уровня
        /// </summary>
        /// <param name="level">Новый уровень</param>
        /// <param name="reward">Награда за уровень</param>
        public void Show(int level, int reward)
        {
            // Обновляем текстовые поля
            if (_levelText != null)
            {
                _levelText.text = level.ToString();
            }
            
            if (_rewardText != null)
            {
                _rewardText.text = $"+{reward} очков";
            }
            
            // Показываем уведомление
            gameObject.SetActive(true);
            
            // Запускаем анимацию, если она есть
            if (_animator != null)
            {
                _animator.SetTrigger(_showAnimationTrigger);
            }
            
            // Запускаем таймер скрытия
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
            }
            
            _hideCoroutine = StartCoroutine(HideAfterDelay());
        }
        
        /// <summary>
        /// Скрывает уведомление
        /// </summary>
        public void Hide()
        {
            // Останавливаем корутину, если она запущена
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
            
            // Запускаем анимацию скрытия, если она есть
            if (_animator != null)
            {
                _animator.SetTrigger(_hideAnimationTrigger);
                StartCoroutine(DisableAfterAnimation());
            }
            else
            {
                // Просто скрываем объект, если анимации нет
                gameObject.SetActive(false);
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Корутина для скрытия уведомления после задержки
        /// </summary>
        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(_showDuration);
            Hide();
        }
        
        /// <summary>
        /// Корутина для отключения объекта после завершения анимации
        /// </summary>
        private IEnumerator DisableAfterAnimation()
        {
            // Ждем, пока завершится анимация (предполагаем, что она длится 1 секунду)
            yield return new WaitForSeconds(1f);
            gameObject.SetActive(false);
        }
        
        #endregion
    }
} 