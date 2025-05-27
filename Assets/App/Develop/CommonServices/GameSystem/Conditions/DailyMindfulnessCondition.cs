using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.DataManagement.DataProviders;
using UnityEngine;

namespace App.Develop.CommonServices.GameSystem.Conditions
{
    /// <summary>
    /// Условие для достижения "Рутина осознанности" - отмечать настроение 7 дней подряд
    /// </summary>
    public class DailyMindfulnessCondition : IAchievementCondition
    {
        // Необходимое количество дней подряд
        private const int RequiredConsecutiveDays = 7;
        
        /// <summary>
        /// Проверяет, отмечал ли пользователь настроение 7 дней подряд
        /// </summary>
        public bool CheckCondition(PlayerData playerData)
        {
            int consecutiveDays = CalculateConsecutiveDays(playerData);
            return consecutiveDays >= RequiredConsecutiveDays;
        }
        
        /// <summary>
        /// Вычисляет прогресс выполнения условия
        /// </summary>
        public float CalculateProgress(PlayerData playerData)
        {
            int consecutiveDays = CalculateConsecutiveDays(playerData);
            return Mathf.Clamp01((float)consecutiveDays / RequiredConsecutiveDays);
        }
        
        /// <summary>
        /// Вычисляет количество дней подряд, когда пользователь отмечал настроение
        /// </summary>
        private int CalculateConsecutiveDays(PlayerData playerData)
        {
            // Если данные отсутствуют, возвращаем 0
            if (playerData?.GameData?.PointsTransactions == null || playerData.GameData.PointsTransactions.Count == 0)
            {
                return 0;
            }
            
            // Получаем даты транзакций, связанных с отметкой эмоций
            List<DateTime> emotionDates = playerData.GameData.PointsTransactions
                .Where(t => t.Source == PointsSource.EmotionMarked)
                .Select(t => t.Timestamp.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();
            
            if (emotionDates.Count == 0)
            {
                return 0;
            }
            
            // Проверяем, сколько дней подряд есть отметки эмоций
            int consecutiveDays = 1; // Начинаем с 1, т.к. у нас уже есть как минимум одна дата
            DateTime lastDate = emotionDates[0];
            
            for (int i = 1; i < emotionDates.Count; i++)
            {
                // Если текущая дата на 1 день меньше предыдущей, увеличиваем счетчик
                if (lastDate.AddDays(-1) == emotionDates[i])
                {
                    consecutiveDays++;
                    lastDate = emotionDates[i];
                }
                else
                {
                    // Прерывается последовательность
                    break;
                }
            }
            
            return consecutiveDays;
        }
    }
} 