using System;
using System.Collections;
using System.Collections.Generic;
using App.Develop.CommonServices.Social;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using App.Develop.Utils.Logging;
using Logger = App.Develop.Utils.Logging.Logger;

namespace App.Develop.CommonServices.UI
{
    /// <summary>
    /// Менеджер уведомлений в пользовательском интерфейсе
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        #region SerializeFields
        [SerializeField] private GameObject _notificationPrefab;
        [SerializeField] private Transform _notificationsContainer;
        [SerializeField] private int _maxNotifications = 3;
        #endregion
        
        #region Private Fields
        private Queue<NotificationView> _notificationPool = new Queue<NotificationView>();
        private List<NotificationView> _activeNotifications = new List<NotificationView>();
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Создаем пул уведомлений
            InitializePool();
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Показывает уведомление с указанной конфигурацией
        /// </summary>
        public void ShowNotification(NotificationConfig config)
        {
            if (config == null)
            {
                Logger.LogError("Попытка показать уведомление с null конфигурацией");
                return;
            }
            
            // Получаем свободное уведомление из пула
            NotificationView notification = GetNotificationFromPool();
            
            // Настраиваем и показываем уведомление
            notification.Configure(
                config.Title,
                config.Message,
                config.Duration,
                config.ShowAcceptButton,
                config.ShowDeclineButton,
                config.OnAccept,
                config.OnDecline,
                () => ReturnToPool(notification)
            );
            
            // Добавляем в список активных
            _activeNotifications.Add(notification);
            
            // Ограничиваем количество одновременно отображаемых уведомлений
            if (_activeNotifications.Count > _maxNotifications)
            {
                // Скрываем самое старое уведомление
                _activeNotifications[0].Hide();
            }
            
            Logger.Log($"Показано уведомление: {config.Title}");
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Инициализирует пул уведомлений
        /// </summary>
        private void InitializePool()
        {
            if (_notificationPrefab == null || _notificationsContainer == null)
            {
                Logger.LogError("Не настроен префаб уведомления или контейнер");
                return;
            }
            
            // Создаем начальный пул из нескольких уведомлений
            for (int i = 0; i < _maxNotifications + 2; i++)
            {
                GameObject notificationObject = Instantiate(_notificationPrefab, _notificationsContainer);
                NotificationView notification = notificationObject.GetComponent<NotificationView>();
                
                if (notification != null)
                {
                    notification.gameObject.SetActive(false);
                    _notificationPool.Enqueue(notification);
                }
            }
        }
        
        /// <summary>
        /// Получает уведомление из пула или создает новое
        /// </summary>
        private NotificationView GetNotificationFromPool()
        {
            if (_notificationPool.Count > 0)
            {
                NotificationView notification = _notificationPool.Dequeue();
                notification.gameObject.SetActive(true);
                return notification;
            }
            else
            {
                // Создаем новое уведомление, если пул пуст
                GameObject notificationObject = Instantiate(_notificationPrefab, _notificationsContainer);
                return notificationObject.GetComponent<NotificationView>();
            }
        }
        
        /// <summary>
        /// Возвращает уведомление в пул
        /// </summary>
        private void ReturnToPool(NotificationView notification)
        {
            if (notification == null)
                return;
                
            // Удаляем из списка активных
            _activeNotifications.Remove(notification);
            
            // Деактивируем и возвращаем в пул
            notification.gameObject.SetActive(false);
            _notificationPool.Enqueue(notification);
        }
        #endregion
    }
    
    /// <summary>
    /// Представление отдельного уведомления
    /// </summary>
    public class NotificationView : MonoBehaviour
    {
        #region SerializeFields
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Button _declineButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private float _fadeSpeed = 1f;
        #endregion
        
        #region Private Fields
        private Action _onAccept;
        private Action _onDecline;
        private Action _onClose;
        private Coroutine _hideCoroutine;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClick);
                
            if (_acceptButton != null)
                _acceptButton.onClick.AddListener(OnAcceptClick);
                
            if (_declineButton != null)
                _declineButton.onClick.AddListener(OnDeclineClick);
        }
        
        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClick);
                
            if (_acceptButton != null)
                _acceptButton.onClick.RemoveListener(OnAcceptClick);
                
            if (_declineButton != null)
                _declineButton.onClick.RemoveListener(OnDeclineClick);
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Настраивает уведомление
        /// </summary>
        public void Configure(
            string title,
            string message,
            float duration,
            bool showAcceptButton,
            bool showDeclineButton,
            Action onAccept,
            Action onDecline,
            Action onClose)
        {
            // Останавливаем предыдущий корутин, если есть
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
            
            // Устанавливаем текст
            if (_titleText != null) _titleText.text = title ?? "Уведомление";
            if (_messageText != null) _messageText.text = message ?? "";
            
            // Настраиваем кнопки
            if (_acceptButton != null) _acceptButton.gameObject.SetActive(showAcceptButton);
            if (_declineButton != null) _declineButton.gameObject.SetActive(showDeclineButton);
            
            // Сохраняем колбэки
            _onAccept = onAccept;
            _onDecline = onDecline;
            _onClose = onClose;
            
            // Показываем уведомление
            gameObject.SetActive(true);
            
            // Устанавливаем скрытие по таймеру
            if (duration > 0)
            {
                _hideCoroutine = StartCoroutine(HideAfterDelay(duration));
            }
        }
        
        /// <summary>
        /// Скрывает уведомление
        /// </summary>
        public void Hide()
        {
            // Останавливаем корутин таймера, если есть
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
            
            // Запускаем корутин скрытия с анимацией
            _hideCoroutine = StartCoroutine(HideWithAnimation());
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Обработчик нажатия на кнопку принятия
        /// </summary>
        private void OnAcceptClick()
        {
            _onAccept?.Invoke();
            Hide();
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку отклонения
        /// </summary>
        private void OnDeclineClick()
        {
            _onDecline?.Invoke();
            Hide();
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку закрытия
        /// </summary>
        private void OnCloseClick()
        {
            Hide();
        }
        
        /// <summary>
        /// Корутин для скрытия уведомления после задержки
        /// </summary>
        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Hide();
        }
        
        /// <summary>
        /// Корутин для скрытия уведомления с анимацией
        /// </summary>
        private IEnumerator HideWithAnimation()
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            canvasGroup.alpha = 1f;
            
            // Плавно уменьшаем прозрачность
            while (canvasGroup.alpha > 0)
            {
                canvasGroup.alpha -= Time.deltaTime * _fadeSpeed;
                yield return null;
            }
            
            // Вызываем колбэк закрытия
            _onClose?.Invoke();
        }
        #endregion
    }
} 