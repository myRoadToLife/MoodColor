using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using App.Develop.DI;
using App.Develop.EntryPoint;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Тестовый скрипт для проверки работы системы очков
    /// </summary>
    public class PointsServiceTester : MonoBehaviour
    {
        #region Editor Fields
        
        [SerializeField] private Text _pointsText;
        [SerializeField] private Button _addPointsButton;
        [SerializeField] private Button _dailyBonusButton;
        [SerializeField] private Button _getHistoryButton;
        
        #endregion
        
        #region Private Fields
        
        private IPointsService _pointsService;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Получаем сервис через EntryPoint
            var entryPoint = FindObjectOfType<EntryPoint.EntryPoint>();
            if (entryPoint == null)
            {
                Debug.LogError("Не найден EntryPoint");
                return;
            }
            
            // Получаем DIContainer через рефлексию (т.к. он приватный)
            var containerField = entryPoint.GetType().GetField("_projectContainer", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (containerField == null)
            {
                Debug.LogError("Не найдено поле _projectContainer в EntryPoint");
                return;
            }
            
            var container = containerField.GetValue(entryPoint) as DIContainer;
            if (container == null)
            {
                Debug.LogError("Не удалось получить DIContainer");
                return;
            }
            
            try
            {
                _pointsService = container.Resolve<IPointsService>();
                
                if (_pointsService == null)
                {
                    Debug.LogError("Не удалось получить сервис очков");
                    return;
                }
                
                InitUI();
                _pointsService.OnPointsChanged += UpdatePointsText;
                UpdatePointsText(_pointsService.CurrentPoints);
                
                Debug.Log("PointsServiceTester успешно инициализирован");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка при инициализации PointsServiceTester: {e.Message}");
            }
        }
        
        private void OnDestroy()
        {
            if (_pointsService != null)
            {
                _pointsService.OnPointsChanged -= UpdatePointsText;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitUI()
        {
            if (_addPointsButton != null)
            {
                _addPointsButton.onClick.AddListener(AddPoints);
            }
            
            if (_dailyBonusButton != null)
            {
                _dailyBonusButton.onClick.AddListener(AddDailyBonus);
            }
            
            if (_getHistoryButton != null)
            {
                _getHistoryButton.onClick.AddListener(ShowHistory);
            }
        }
        
        private void UpdatePointsText(int points)
        {
            if (_pointsText != null)
            {
                _pointsText.text = $"Очки: {points}";
            }
        }
        
        private void AddPoints()
        {
            _pointsService.AddPoints(10, PointsSource.Achievement);
        }
        
        private void AddDailyBonus()
        {
            _pointsService.AddDailyBonus();
        }
        
        private void ShowHistory()
        {
            var history = _pointsService.GetTransactionsHistory();
            Debug.Log($"История транзакций (всего {history.Count}):");
            
            foreach (var transaction in history)
            {
                Debug.Log($"[{transaction.Timestamp}] {transaction.Source}: {transaction.Amount} очков");
            }
        }
        
        #endregion
    }
} 