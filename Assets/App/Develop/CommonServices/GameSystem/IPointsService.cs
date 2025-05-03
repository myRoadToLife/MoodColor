using System;
using System.Collections.Generic;

namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Интерфейс сервиса управления очками
    /// </summary>
    public interface IPointsService
    {
        /// <summary>
        /// Текущее количество очков
        /// </summary>
        int CurrentPoints { get; }
        
        /// <summary>
        /// Событие изменения очков
        /// </summary>
        event Action<int> OnPointsChanged;
        
        /// <summary>
        /// Событие получения очков
        /// </summary>
        event Action<int, PointsSource> OnPointsEarned;
        
        /// <summary>
        /// Добавить очки пользователю
        /// </summary>
        /// <param name="amount">Количество очков</param>
        /// <param name="source">Источник очков</param>
        void AddPoints(int amount, PointsSource source);
        
        /// <summary>
        /// Использовать очки (списать)
        /// </summary>
        /// <param name="amount">Количество очков</param>
        /// <returns>True, если списание выполнено успешно</returns>
        bool SpendPoints(int amount);
        
        /// <summary>
        /// Получить историю транзакций очков
        /// </summary>
        /// <param name="count">Количество последних транзакций (0 - все)</param>
        /// <returns>Список транзакций</returns>
        List<PointsTransaction> GetTransactionsHistory(int count = 0);
        
        /// <summary>
        /// Добавить очки за отметку эмоции
        /// </summary>
        void AddPointsForEmotion();
        
        /// <summary>
        /// Добавить бонусные очки за ежедневное использование
        /// </summary>
        void AddDailyBonus();
    }
} 