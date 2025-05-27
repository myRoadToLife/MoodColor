using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using UnityEngine;

namespace App.Develop.CommonServices.GameSystem.Conditions
{
    /// <summary>
    /// Условие для достижения "Эмоциональный спектр" - испытать каждую эмоцию хотя бы раз
    /// </summary>
    public class EmotionSpectrumCondition : IAchievementCondition
    {
        // Список всех типов эмоций, которые нужно испытать
        private readonly HashSet<string> _allEmotionTypes;
        
        public EmotionSpectrumCondition()
        {
            // Получаем все возможные типы эмоций из enum
            _allEmotionTypes = new HashSet<string>();
            foreach (EmotionTypes emotionType in System.Enum.GetValues(typeof(EmotionTypes)))
            {
                _allEmotionTypes.Add(emotionType.ToString());
            }
        }
        
        /// <summary>
        /// Проверяет, испытал ли пользователь все эмоции хотя бы раз
        /// </summary>
        public bool CheckCondition(PlayerData playerData)
        {
            // Получаем множество всех испытанных эмоций пользователя
            HashSet<string> experiencedEmotions = GetExperiencedEmotions(playerData);
            
            // Проверяем, содержит ли множество испытанных эмоций все необходимые типы
            return _allEmotionTypes.IsSubsetOf(experiencedEmotions);
        }
        
        /// <summary>
        /// Вычисляет прогресс выполнения условия
        /// </summary>
        public float CalculateProgress(PlayerData playerData)
        {
            // Получаем множество всех испытанных эмоций пользователя
            HashSet<string> experiencedEmotions = GetExperiencedEmotions(playerData);
            
            // Считаем, сколько разных типов эмоций испытал пользователь
            int experiencedCount = 0;
            foreach (string emotionType in _allEmotionTypes)
            {
                if (experiencedEmotions.Contains(emotionType))
                {
                    experiencedCount++;
                }
            }
            
            // Вычисляем прогресс как отношение количества испытанных эмоций к общему количеству
            return (float)experiencedCount / _allEmotionTypes.Count;
        }
        
        /// <summary>
        /// Получает множество всех испытанных эмоций пользователя
        /// </summary>
        private HashSet<string> GetExperiencedEmotions(PlayerData playerData)
        {
            HashSet<string> experiencedEmotions = new HashSet<string>();
            
            // Если данные эмоций отсутствуют, возвращаем пустое множество
            if (playerData?.EmotionData == null)
            {
                return experiencedEmotions;
            }
            
            // Добавляем в множество типы всех эмоций, у которых значение больше нуля
            foreach (var pair in playerData.EmotionData)
            {
                if (pair.Value != null && pair.Value.Value > 0)
                {
                    experiencedEmotions.Add(pair.Key.ToString());
                }
            }
            
            return experiencedEmotions;
        }
    }
} 