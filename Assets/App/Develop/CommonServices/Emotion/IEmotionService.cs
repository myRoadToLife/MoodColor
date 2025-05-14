using System;
using System.Collections.Generic;
using UnityEngine;
using App.Develop.CommonServices.DataManagement.DataProviders;

namespace App.Develop.CommonServices.Emotion
{
    /// <summary>
    /// Интерфейс сервиса эмоций
    /// </summary>
    public interface IEmotionService
    {
        /// <summary>
        /// Событие изменения эмоции
        /// </summary>
        event EventHandler<EmotionEvent> OnEmotionEvent;
        
        /// <summary>
        /// Получить текущее значение эмоции
        /// </summary>
        float GetEmotionValue(EmotionTypes type);
        
        /// <summary>
        /// Получить текущую интенсивность эмоции
        /// </summary>
        float GetEmotionIntensity(EmotionTypes type);
        
        /// <summary>
        /// Получить цвет эмоции
        /// </summary>
        Color GetEmotionColor(EmotionTypes type);
        
        /// <summary>
        /// Получить данные эмоции
        /// </summary>
        EmotionData GetEmotionData(EmotionTypes type);
        
        /// <summary>
        /// Получить все эмоции
        /// </summary>
        Dictionary<EmotionTypes, EmotionData> GetAllEmotions();
        
        /// <summary>
        /// Установить значение эмоции
        /// </summary>
        void SetEmotionValue(EmotionTypes type, float value, bool needSave = true);
        
        /// <summary>
        /// Установить интенсивность эмоции
        /// </summary>
        void SetEmotionIntensity(EmotionTypes type, float intensity, bool needSave = true);
        
        /// <summary>
        /// Сбросить все эмоции к начальным значениям
        /// </summary>
        void ResetAllEmotions(bool needSave = true);

        // Метод для логирования событий эмоций, таких как клик по банке
        void LogEmotionEvent(EmotionTypes type, EmotionEventType eventType, string note = null);
    }
} 