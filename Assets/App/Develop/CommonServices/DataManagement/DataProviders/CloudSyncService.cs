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

            if (!_saveLoadService.TryLoad(out _syncStatus))
            {
                _syncStatus = new SyncStatusData();
                _syncStatus.IsLastSyncSuccessful = true;
                _saveLoadService.Save(_syncStatus);
            }

            Application.quitting += OnApplicationQuitting;
            Application.focusChanged += OnApplicationFocusChanged;

            CheckAndLoadFromCloud();
        }

        private void OnApplicationQuitting()
        {
            SyncDataToCloudSync();
        }

        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
            {
                SyncToCloudAsync().ConfigureAwait(false);
            }
            else
            {
                CheckAndLoadFromCloud();
            }
        }

        private async void CheckAndLoadFromCloud()
        {
            SyncStatusData syncStatus = GetLastSyncStatus();

            if (syncStatus.IsLastSyncSuccessful)
            {
                bool loadSuccess = await LoadDataFromCloud();
// Log success/failure if needed, or throw if critical
            }
            else
            {
// Log warning if needed, or throw if critical
            }
        }

        public async Task<bool> SyncDataToCloud()
        {
            if (_isSyncInProgress)
                return false;

            _isSyncInProgress = true;

            try
            {
                bool isConnected = await _databaseService.CheckConnection();

                if (!isConnected)
                {
                    _isSyncInProgress = false;
                    return false;
                }

                if (!_databaseService.IsAuthenticated)
                {
                    _isSyncInProgress = false;
                    return false;
                }

                if (!_saveLoadService.TryLoad(out GameData gameData))
                {
                    _isSyncInProgress = false;
                    return false;
                }

                await _databaseService.SaveUserGameData(gameData);

                _syncStatus.LastSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _syncStatus.IsLastSyncSuccessful = true;
                _syncStatus.SyncErrorMessage = string.Empty;
                _saveLoadService.Save(_syncStatus);

                _isSyncInProgress = false;
                return true;
            }
            catch (Exception ex)
            {
                SaveSyncStatus(false, $"Исключение: {ex.Message}");
                _isSyncInProgress = false;
                throw new Exception($"Ошибка при синхронизации данных с облаком: {ex.Message}", ex);
            }
        }

        private void SyncDataToCloudSync()
        {
            try
            {
                if (!_saveLoadService.TryLoad(out GameData gameData))
                {
                    return;
                }

                Task.Run(async () =>
                {
                    try
                    {
                        if (_databaseService.IsAuthenticated)
                        {
                            await _databaseService.SaveUserGameData(gameData);
                            _syncStatus.LastSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            _syncStatus.IsLastSyncSuccessful = true;
                            _syncStatus.SyncErrorMessage = string.Empty;
                            _saveLoadService.Save(_syncStatus);
                        }
                    }
                    catch (Exception ex)
                    {
                        SaveSyncStatus(false, $"Исключение при закрытии: {ex.Message}");
// Consider re-throwing or handling more gracefully
                    }
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
// Consider re-throwing or handling more gracefully
                throw new Exception($"Критическая ошибка при синхронизации данных при закрытии: {ex.Message}", ex);
            }
        }

        private async Task SyncToCloudAsync()
        {
            if (_isSyncInProgress)
                return;

            _isSyncInProgress = true;

            try
            {
                bool syncSuccess = await SyncDataToCloud();
// Log success/failure if needed
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
                if (!_syncStatus.IsLastSyncSuccessful)
                {
                    _isSyncInProgress = false;
                    return false;
                }

                bool isConnected = await _databaseService.CheckConnection();

                if (!isConnected)
                {
                    _isSyncInProgress = false;
                    return false;
                }

                if (!_databaseService.IsAuthenticated)
                {
                    _isSyncInProgress = false;
                    return false;
                }

                GameData gameData = await _databaseService.LoadUserGameData();

                if (gameData != null)
                {
                    _saveLoadService.Save(gameData);
                    _isSyncInProgress = false;
                    return true;
                }

                _isSyncInProgress = false;
                return false;
            }
            catch (Exception ex)
            {
                _isSyncInProgress = false;
                throw new Exception($"Ошибка при загрузке данных из облака: {ex.Message}", ex);
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
        }

        public void Dispose()
        {
            Application.quitting -= OnApplicationQuitting;
            Application.focusChanged -= OnApplicationFocusChanged;
        }
    }
}
