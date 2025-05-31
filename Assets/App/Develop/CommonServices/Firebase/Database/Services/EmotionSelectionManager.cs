// Assets/App/Develop/MoodColor/UI/EmotionSelectionManager.cs

using System;
using System.Collections.Generic; // Not used
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.DataManagement.DataProviders; // For EmotionData, JarData etc. if used by DatabaseService
using App.Develop.CommonServices.Emotion;
// using App.Develop.Configs.Common.Emotion; // Not used
using App.Develop.DI;
// using Firebase.Database; // Not used directly
using UnityEngine;
using UnityEngine.UI; // For Slider, InputField, Button
// using App.Develop.Utils.Logging; // MyLogger removed

namespace App.Develop.CommonServices.Firebase.Database.Services // Namespace adjusted to reflect location
{
    public class EmotionSelectionManager : MonoBehaviour, IInjectable
    {
        // --- Настройки UI ---
        [Header("UI Elements")]
        [SerializeField] private GameObject _intensitySelectionPanel; // Панель выбора интенсивности
        [SerializeField] private Slider _intensitySlider;           // Слайдер интенсивности (1-5)
        [SerializeField] private InputField _noteInput;             // Поле для заметки (TMP_InputField или стандартный)
        [SerializeField] private Button _confirmButton;             // Кнопка подтверждения
        [SerializeField] private Button _cancelButton;              // Кнопка отмены

        // --- Зависимости ---
        private IDatabaseService _databaseService;

        // --- Внутренние переменные ---
        private EmotionTypes _selectedEmotionType;
        private bool _isProcessing = false; // Флаг для предотвращения двойных нажатий

        // --- Константы ---
        private const int POINTS_PER_EMOTION = 10; // Очки за отметку эмоции
        private const int AMOUNT_PER_EMOTION = 5;  // Количество "жидкости" за отметку

        /// <summary>
        /// Внедрение зависимостей
        /// </summary>
        public void Inject(DIContainer container)
        {
            _databaseService = container.Resolve<IDatabaseService>();
            if (_databaseService == null)
            {
                throw new InvalidOperationException("IDatabaseService not resolved in EmotionSelectionManager.");
            }
        }

        private void Start()
        {
            // Скрываем панель выбора при старте
            if (_intensitySelectionPanel != null)
            {
                _intensitySelectionPanel.SetActive(false);
            }

            // Настраиваем обработчики событий кнопок
            _confirmButton?.onClick.AddListener(OnConfirmEmotion);
            _cancelButton?.onClick.AddListener(OnCancelSelection);
        }

        private void OnDestroy()
        {
            // Отписываемся от событий кнопок
            _confirmButton?.onClick.RemoveListener(OnConfirmEmotion);
            _cancelButton?.onClick.RemoveListener(OnCancelSelection);
        }

        /// <summary>
        /// Вызывается при выборе эмоции (например, нажатии на банку)
        /// </summary>
        /// <param name="emotionType">Выбранный тип эмоции</param>
        public void SelectEmotion(EmotionTypes emotionType)
        {
            if (_isProcessing) return; // Игнорируем, если уже идет обработка

            _selectedEmotionType = emotionType;

            // Показываем панель выбора интенсивности
            if (_intensitySelectionPanel != null)
            {
                // Сбрасываем значения UI
                if (_intensitySlider != null) _intensitySlider.value = 3; // Средняя интенсивность
                if (_noteInput != null) _noteInput.text = "";
                _intensitySelectionPanel.SetActive(true);
            }
            else
            {
                throw new InvalidOperationException("Панель выбора интенсивности не настроена!");
            }
        }

        /// <summary>
        /// Вызывается при подтверждении выбора эмоции
        /// </summary>
        private async void OnConfirmEmotion()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                int intensity = 3; // Значение по умолчанию
                if (_intensitySlider != null)
                {
                    intensity = Mathf.RoundToInt(_intensitySlider.value);
                }

                string note = string.Empty;
                if (_noteInput != null)
                {
                    note = _noteInput.text.Trim();
                }

                // 1. Создаем объект EmotionData
                EmotionData emotionData = new EmotionData // Explicit type
                {
                    Type = _selectedEmotionType.ToString(),
                    Intensity = intensity, // Assuming EmotionData.Intensity is float/double, int might be fine too
                    Note = note,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    ColorHex = GetColorHexForEmotion(_selectedEmotionType)
                    // TODO: Добавить RegionId и Location, если нужно
                };

                // 2. Сохраняем запись об эмоции в Firebase
                await _databaseService.UpdateUserEmotion(emotionData);

                // 3. Обновляем текущую эмоцию пользователя
                await _databaseService.UpdateCurrentEmotion(emotionData.Type, emotionData.Intensity);

                // 4. Обновляем количество в баночке
                await _databaseService.UpdateJarAmount(emotionData.Type, AMOUNT_PER_EMOTION);

                // 5. Начисляем очки пользователю
                await _databaseService.UpdateUserProfileField("totalPoints", POINTS_PER_EMOTION);

                // Скрываем панель выбора
                if (_intensitySelectionPanel != null)
                {
                    _intensitySelectionPanel.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                // MyLogger.LogError($"❌ Ошибка при подтверждении эмоции: {ex.Message}", MyLogger.LogCategory.Firebase); // Replaced by throw
                // TODO: Показать пользователю сообщение об ошибке (UI handling)
                _isProcessing = false; // Ensure flag is reset before throwing
                throw new Exception($"❌ Ошибка при подтверждении эмоции: {ex.Message}", ex);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// Вызывается при отмене выбора
        /// </summary>
        private void OnCancelSelection()
        {
            if (_isProcessing) return;

            if (_intensitySelectionPanel != null)
            {
                _intensitySelectionPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Возвращает HEX-код цвета для указанной эмоции
        /// </summary>
        private string GetColorHexForEmotion(EmotionTypes emotionType)
        {
            // TODO: Заменить на получение цветов из конфигурации
            Color color;
            switch (emotionType)
            {
                case EmotionTypes.Joy: color = Color.yellow; break;
                case EmotionTypes.Sadness: color = Color.blue; break;
                case EmotionTypes.Anger: color = Color.red; break;
                case EmotionTypes.Fear: color = new Color(0.5f, 0, 0.5f); break; // Purple
                case EmotionTypes.Disgust: color = Color.green; break;
                // Добавь остальные цвета
                default: color = Color.gray; break;
            }
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }
    }
}
