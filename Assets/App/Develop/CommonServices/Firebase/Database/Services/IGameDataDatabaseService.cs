using System.Threading.Tasks;
using App.Develop.CommonServices.DataManagement.DataProviders;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с игровыми данными в Firebase Database
    /// </summary>
    public interface IGameDataDatabaseService
    {
        /// <summary>
        /// Сохраняет игровые данные пользователя
        /// </summary>
        /// <param name="gameData">Игровые данные для сохранения</param>
        Task SaveUserGameData(GameData gameData);
        
        /// <summary>
        /// Загружает игровые данные пользователя
        /// </summary>
        Task<GameData> LoadUserGameData();
    }
} 