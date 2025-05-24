// Assets/App/Develop/MoodColor/UI/EmotionSelectionManager.cs

using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.Configs.Common.Emotion;
using App.Develop.DI;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using App.Develop.Utils.Logging;

// Используем UnityEngine.UI

namespace App.Develop.CommonServices.Firebase.Database.Services
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
        private DatabaseService _databaseService;

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
            _databaseService = container.Resolve<DatabaseService>();
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
                MyLogger.LogError("Панель выбора интенсивности не настроена!", MyLogger.LogCategory.Firebase);
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
                var emotionData = new EmotionData
                {
                    Type = _selectedEmotionType.ToString(),
                    Intensity = intensity,
                    Note = note,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    ColorHex = GetColorHexForEmotion(_selectedEmotionType)
                    // TODO: Добавить RegionId и Location, если нужно
                };

                // 2. Сохраняем запись об эмоции в Firebase
                await _databaseService.AddEmotion(emotionData);

                // 3. Обновляем текущую эмоцию пользователя
                await _databaseService.UpdateCurrentEmotion(emotionData.Type, emotionData.Intensity);

                // 4. Обновляем количество в баночке
                await _databaseService.UpdateJarAmount(emotionData.Type, AMOUNT_PER_EMOTION);

                // 5. Начисляем очки пользователю
                await _databaseService.AddPointsToProfile(POINTS_PER_EMOTION);

                MyLogger.Log($"✅ Эмоция {_selectedEmotionType} с интенсивностью {intensity} сохранена.", MyLogger.LogCategory.Firebase);

                // Скрываем панель выбора
                if (_intensitySelectionPanel != null)
                {
                    _intensitySelectionPanel.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при подтверждении эмоции: {ex.Message}", MyLogger.LogCategory.Firebase);
                // TODO: Показать пользователю сообщение об ошибке
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

        private void OnDestroy()
        {
            // Отписываемся от событий кнопок
            _confirmButton?.onClick.RemoveListener(OnConfirmEmotion);
            _cancelButton?.onClick.RemoveListener(OnCancelSelection);
        }
    }
}
