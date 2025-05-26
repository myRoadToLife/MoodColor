using System;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.Utils.Logging;
using UnityEngine;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    public class CloudSyncService : ICloudSyncService
    {
        private const string SyncStatusKey = "cloud_sync_status";
        
        private readonly ISaveLoadService _saveLoadService;
        private readonly IDatabaseService _databaseService;
        private SyncStatusData _syncStatus;
        private bool _isSyncInProgress = false;
        
        public CloudSyncService(ISaveLoadService saveLoadService, IDatabaseService databaseService)
        {
            _saveLoadService = saveLoadService;
            _databaseService = databaseService;
            
            // Загружаем статус синхронизации из локального хранилища
            if (!_saveLoadService.TryLoad(out _syncStatus))
            {
                _syncStatus = new SyncStatusData();
                // При первом запуске устанавливаем IsLastSyncSuccessful в true,
                // чтобы данные загрузились из облака
                _syncStatus.IsLastSyncSuccessful = true;
                _saveLoadService.Save(_syncStatus);
            }
            
            // Подписываемся на события жизненного цикла приложения
            Application.quitting += OnApplicationQuitting;
            Application.focusChanged += OnApplicationFocusChanged;
            
            // При запуске проверяем, нужно ли загрузить данные из облака
            CheckAndLoadFromCloud();
        }
        
        private void OnApplicationQuitting()
        {
            // Синхронизируем данные с облаком при закрытии приложения
            // Синхронный вызов, так как приложение закрывается
            SyncDataToCloudSync();
        }
        
        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Приложение теряет фокус - синхронизируем с облаком
                SyncToCloudAsync().ConfigureAwait(false);
            }
            else
            {
                // Приложение получает фокус - проверяем, нужно ли загрузить данные
                CheckAndLoadFromCloud();
            }
        }
        
        private async void CheckAndLoadFromCloud()
        {
            var syncStatus = GetLastSyncStatus();
            
            if (syncStatus.IsLastSyncSuccessful)
            {
                                // MyLogger.Log("🔄 Последняя синхронизация была успешной. Загружаем данные из облака...",
                // MyLogger.LogCategory.Sync);
                
                bool loadSuccess = await LoadDataFromCloud();
                
                MyLogger.Log($"🔄 Загрузка данных из облака: {(loadSuccess ? "✅ Успешно" : "❌ Неудачно")}",
                    MyLogger.LogCategory.Sync);
            }
            else
            {
                MyLogger.LogWarning($"⚠️ Последняя синхронизация не была успешной: {syncStatus.SyncErrorMessage}. " +
                                   "Используем локальные данные.",
                    MyLogger.LogCategory.Sync);
            }
        }
        
        public async Task<bool> SyncDataToCloud()
        {
            if (_isSyncInProgress)
                return false;
                
            _isSyncInProgress = true;
            
            try
            {
                // Проверяем подключение к Firebase
                bool isConnected = await _databaseService.CheckConnection();
                if (!isConnected)
                {
                    MyLogger.LogWarning("⚠️ Нет подключения к Firebase для синхронизации данных", 
                        MyLogger.LogCategory.Sync);
                    _isSyncInProgress = false;
                    return false;
                }
                
                // Проверяем, аутентифицирован ли пользователь
                if (!_databaseService.IsAuthenticated)
                {
                    MyLogger.LogWarning("⚠️ Пользователь не аутентифицирован для синхронизации данных", 
                        MyLogger.LogCategory.Sync);
                    _isSyncInProgress = false;
                    return false;
                }
                
                // Получаем игровые данные, которые нужно синхронизировать
                if (!_saveLoadService.TryLoad(out GameData gameData))
                {
                    MyLogger.LogWarning("⚠️ Нет локальных игровых данных для синхронизации", 
                        MyLogger.LogCategory.Sync);
                    _isSyncInProgress = false;
                    return false;
                }
                
                // Сохраняем данные в облако
                await _databaseService.SaveUserGameData(gameData);
                
                // Обновляем timestamp последней синхронизации
                _syncStatus.LastSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _syncStatus.IsLastSyncSuccessful = true;
                _syncStatus.SyncErrorMessage = string.Empty;
                _saveLoadService.Save(_syncStatus);
                
                MyLogger.Log("✅ Данные успешно синхронизированы с облаком", 
                    MyLogger.LogCategory.Sync);
                
                _isSyncInProgress = false;
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при синхронизации данных с облаком: {ex.Message}", 
                    MyLogger.LogCategory.Sync);
                SaveSyncStatus(false, $"Исключение: {ex.Message}");
                _isSyncInProgress = false;
                return false;
            }
        }
        
        // Синхронный метод для вызова при закрытии приложения
        private void SyncDataToCloudSync()
        {
            try
            {
                MyLogger.Log("🔄 Синхронизация данных с облаком при закрытии приложения...", 
                    MyLogger.LogCategory.Sync);
                
                // Получаем игровые данные, которые нужно синхронизировать
                if (!_saveLoadService.TryLoad(out GameData gameData))
                {
                    MyLogger.LogWarning("⚠️ Нет локальных игровых данных для синхронизации при закрытии", 
                        MyLogger.LogCategory.Sync);
                    return;
                }
                
                // Сохраняем данные в облако синхронно (только при закрытии)
                // Блокирующий вызов, так как приложение закрывается
                Task.Run(async () => 
                {
                    try
                    {
                        if (_databaseService.IsAuthenticated)
                        {
                            await _databaseService.SaveUserGameData(gameData);
                            // Обновляем статус синхронизации
                            _syncStatus.LastSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            _syncStatus.IsLastSyncSuccessful = true;
                            _syncStatus.SyncErrorMessage = string.Empty;
                            _saveLoadService.Save(_syncStatus);
                            MyLogger.Log("✅ Данные успешно синхронизированы с облаком при закрытии", 
                                MyLogger.LogCategory.Sync);
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"❌ Ошибка при синхронизации данных при закрытии: {ex.Message}", 
                            MyLogger.LogCategory.Sync);
                        SaveSyncStatus(false, $"Исключение при закрытии: {ex.Message}");
                    }
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Критическая ошибка при синхронизации данных при закрытии: {ex.Message}", 
                    MyLogger.LogCategory.Sync);
            }
        }
        
        private async Task SyncToCloudAsync()
        {
            if (_isSyncInProgress)
                return;
                
            _isSyncInProgress = true;
            
            try
            {
                MyLogger.Log("🔄 Синхронизация данных с облаком...", MyLogger.LogCategory.Sync);
                
                bool syncSuccess = await SyncDataToCloud();
                
                MyLogger.Log($"🔄 Синхронизация данных с облаком: {(syncSuccess ? "✅ Успешно" : "❌ Неудачно")}",
                    MyLogger.LogCategory.Sync);
            }
            finally
            {
                _isSyncInProgress = false;
            }
        }
        
        public async Task<bool> LoadDataFromCloud()
        {
            if (_isSyncInProgress)
                return false;
                
            _isSyncInProgress = true;
            
            try
            {
                // Проверяем, была ли последняя синхронизация успешной
                if (!_syncStatus.IsLastSyncSuccessful)
                {
                    MyLogger.LogWarning("⚠️ Последняя синхронизация не была успешной. Пропускаем загрузку из облака", 
                        MyLogger.LogCategory.Sync);
                    _isSyncInProgress = false;
                    return false;
                }
                
                // Проверяем подключение к Firebase
                bool isConnected = await _databaseService.CheckConnection();
                if (!isConnected)
                {
                    MyLogger.LogWarning("⚠️ Нет подключения к Firebase для загрузки данных", 
                        MyLogger.LogCategory.Sync);
                    _isSyncInProgress = false;
                    return false;
                }
                
                // Проверяем, аутентифицирован ли пользователь
                if (!_databaseService.IsAuthenticated)
                {
                    MyLogger.LogWarning("⚠️ Пользователь не аутентифицирован для загрузки данных", 
                        MyLogger.LogCategory.Sync);
                    _isSyncInProgress = false;
                    return false;
                }
                
                // Загружаем данные из облака
                GameData gameData = await _databaseService.LoadUserGameData();
                
                // Если данные успешно загружены, сохраняем их локально
                if (gameData != null)
                {
                    _saveLoadService.Save(gameData);
                    
                    MyLogger.Log("✅ Данные успешно загружены из облака", 
                        MyLogger.LogCategory.Sync);
                    
                    _isSyncInProgress = false;
                    return true;
                }
                
                MyLogger.LogWarning("⚠️ Нет данных в облаке для загрузки", 
                    MyLogger.LogCategory.Sync);
                _isSyncInProgress = false;
                return false;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при загрузке данных из облака: {ex.Message}", 
                    MyLogger.LogCategory.Sync);
                _isSyncInProgress = false;
                return false;
            }
        }
        
        public SyncStatusData GetLastSyncStatus()
        {
            return _syncStatus;
        }
        
        public void SaveSyncStatus(bool isSuccessful, string errorMessage = "")
        {
            _syncStatus.IsLastSyncSuccessful = isSuccessful;
            _syncStatus.LastSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _syncStatus.SyncErrorMessage = errorMessage;
            
            _saveLoadService.Save(_syncStatus);
            
            MyLogger.Log($"💾 Статус синхронизации сохранен: {(isSuccessful ? "✅ Успешно" : $"❌ Неудачно: {errorMessage}")}", 
                MyLogger.LogCategory.Sync);
        }
        
        public void Dispose()
        {
            // Отписываемся от событий при уничтожении сервиса
            Application.quitting -= OnApplicationQuitting;
            Application.focusChanged -= OnApplicationFocusChanged;
        }
    }
} 