using App.Develop.CommonServices.Emotion;
using App.Develop.Utils.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

// Убедись, что это пространство имен доступно и содержит EmotionHistoryEntry и EmotionData

namespace App.Develop.Scenes.PersonalAreaScene.Panels.HistoryPanel
{
    public class HistoryItemView : MonoBehaviour
    {
        [SerializeField] private Image _emotionIndicator;
        [SerializeField] private TextMeshProUGUI _dateText;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _emotionNameText; // Для отображения типа эмоции

        public void Setup(EmotionHistoryEntry entry)
        {
            if (entry == null)
            {
                MyLogger.LogError("[HistoryItemView] Received entry is NULL!");
                gameObject.SetActive(false);
                return;
            }
            if (entry.EmotionData == null)
            {
                MyLogger.LogError($"[HistoryItemView] Received entry.EmotionData is NULL! Timestamp from entry: {entry.Timestamp}");
                if (_dateText != null) _dateText.text = entry.Timestamp.ToString("dd.MM.yyyy");
                if (_timeText != null) _timeText.text = entry.Timestamp.ToString("HH:mm");
                if (_emotionNameText != null) _emotionNameText.text = "Ошибка данных";
                if (_emotionIndicator != null) _emotionIndicator.color = Color.magenta; // Явный цвет ошибки
                gameObject.SetActive(true); // Показать, чтобы увидеть ошибку
                return;
            }

            MyLogger.Log($"[HistoryItemView Setup] Displaying: Timestamp='{entry.Timestamp}', EmotionType='{entry.EmotionData.Type}', Color='{entry.EmotionData.Color}', Value='{entry.EmotionData.Value}'");

            gameObject.SetActive(true);

            // Пробуем разные подходы к форматированию даты для гарантирования правильного отображения
            DateTime displayTime;
            try 
            {
                // Сначала пробуем преобразовать в локальное время, если метка времени в UTC
                displayTime = entry.Timestamp.Kind == DateTimeKind.Utc ? 
                    entry.Timestamp.ToLocalTime() : entry.Timestamp;
            }
            catch (Exception ex)
            {
                // В случае любой ошибки используем исходную метку времени
                MyLogger.LogWarning($"[HistoryItemView] Ошибка при конвертации времени: {ex.Message}. Используем исходную метку.");
                displayTime = entry.Timestamp;
            }

            if (_dateText != null)
                _dateText.text = displayTime.ToString("dd.MM.yyyy");
        
            if (_timeText != null)
                _timeText.text = displayTime.ToString("HH:mm");

            if (_emotionIndicator != null)
            {
                // Предполагается, что в EmotionData есть поле Color типа UnityEngine.Color
                _emotionIndicator.color = entry.EmotionData.Color; 
                // Если у тебя для каждой эмоции своя иконка, то здесь будет логика вроде:
                // _emotionIndicator.sprite = GetSpriteForEmotion(entry.EmotionData.Type);
            }

            if (_emotionNameText != null)
            {
                // В EmotionData.Type (string) хранится системное имя типа эмоции (например, "Joy", "Sadness")
                // Здесь можно его напрямую отобразить или получить более "красивое" или локализованное название
                _emotionNameText.text = entry.EmotionData.Type; 
            }
        }

        // Пример вспомогательного метода, если бы у тебя были спрайты для эмоций:
        // private Sprite GetSpriteForEmotion(string emotionType)
        // {
        //     // Здесь логика загрузки или выбора спрайта на основе emotionType
        //     // Например, из Resources или через ScriptableObject с конфигурацией эмоций
        //     return null; 
        // }
    }
}