using System;
using System.Threading.Tasks;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.Utils.Logging;
using Firebase.Database;
using Newtonsoft.Json;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Сервис для работы с игровыми данными в Firebase Database
    /// </summary>
    public class GameDataDatabaseService : FirebaseDatabaseServiceBase, IGameDataDatabaseService
    {
        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса игровых данных
        /// </summary>
        /// <param name="database">Ссылка на базу данных</param>
        /// <param name="cacheManager">Менеджер кэша Firebase</param>
        /// <param name="validationService">Сервис валидации данных</param>
        public GameDataDatabaseService(
            DatabaseReference database,
            FirebaseCacheManager cacheManager,
            DataValidationService validationService = null) 
            : base(database, cacheManager, validationService)
        {
            MyLogger.Log("✅ GameDataDatabaseService инициализирован", MyLogger.LogCategory.Firebase);
        }
        #endregion

        #region IGameDataDatabaseService Implementation
        /// <summary>
        /// Сохраняет игровые данные пользователя
        /// </summary>
        public async Task SaveUserGameData(GameData gameData)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogError("[GameDataDatabaseService] Невозможно сохранить GameData: пользователь не аутентифицирован.", MyLogger.LogCategory.Firebase);
                return;
            }
            
            if (gameData == null)
            {
                MyLogger.LogError("[GameDataDatabaseService] Невозможно сохранить GameData: передан null объект.", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                string jsonData = JsonConvert.SerializeObject(gameData, Formatting.Indented); // Formatting.Indented для читаемости в Firebase
                DatabaseReference gameDataRef = _database.Child("users").Child(_userId).Child("gameData");
                await gameDataRef.SetRawJsonValueAsync(jsonData);
                MyLogger.Log($"[GameDataDatabaseService] GameData для пользователя {_userId} успешно сохранено в Firebase.", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[GameDataDatabaseService] Ошибка при сохранении GameData в Firebase: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Загружает игровые данные пользователя
        /// </summary>
        public async Task<GameData> LoadUserGameData()
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("[GameDataDatabaseService] Невозможно загрузить GameData: пользователь не аутентифицирован.", MyLogger.LogCategory.Firebase);
                return null;
            }

            try
            {
                DatabaseReference gameDataRef = _database.Child("users").Child(_userId).Child("gameData");
                DataSnapshot snapshot = await gameDataRef.GetValueAsync();

                if (snapshot.Exists)
                {
                    string jsonData = snapshot.GetRawJsonValue();
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        GameData gameData = JsonConvert.DeserializeObject<GameData>(jsonData);
                        MyLogger.Log($"[GameDataDatabaseService] GameData для пользователя {_userId} успешно загружено из Firebase.", MyLogger.LogCategory.Firebase);
                        return gameData;
                    }
                    else
                    {
                        MyLogger.LogWarning($"[GameDataDatabaseService] GameData для пользователя {_userId} существует в Firebase, но содержит пустые данные.", MyLogger.LogCategory.Firebase);
                        return new GameData(); // Возвращаем новый экземпляр, чтобы избежать null
                    }
                }
                else
                {
                    MyLogger.Log($"[GameDataDatabaseService] GameData для пользователя {_userId} не найдено в Firebase. Будут использованы данные по умолчанию.", MyLogger.LogCategory.Firebase);
                    return new GameData(); // Возвращаем новый экземпляр, если данных нет
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[GameDataDatabaseService] Ошибка при загрузке GameData из Firebase: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
                return new GameData(); // В случае ошибки возвращаем новый экземпляр
            }
        }
        #endregion
    }
} 