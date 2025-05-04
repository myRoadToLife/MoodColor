using App.Develop.CommonServices.DataManagement.DataProviders;

namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Интерфейс для проверки условий достижений
    /// </summary>
    public interface IAchievementCondition
    {
        /// <summary>
        /// Проверяет, выполнено ли условие достижения
        /// </summary>
        /// <param name="playerData">Данные игрока</param>
        /// <returns>True, если условие выполнено</returns>
        bool CheckCondition(PlayerData playerData);
        
        /// <summary>
        /// Вычисляет текущий прогресс выполнения условия
        /// </summary>
        /// <param name="playerData">Данные игрока</param>
        /// <returns>Прогресс от 0.0f до 1.0f</returns>
        float CalculateProgress(PlayerData playerData);
    }
} 