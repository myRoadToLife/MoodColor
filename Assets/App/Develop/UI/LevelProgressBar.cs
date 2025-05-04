using System;
using App.Develop.CommonServices.GameSystem;
using App.Develop.DI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.UI
{
    /// <summary>
    /// Компонент UI для отображения прогресса уровня
    /// </summary>
    public class LevelProgressBar : MonoBehaviour, IInjectable
    {
        #region Inspector Fields
        
        [SerializeField] private Image _fillImage;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _xpText;
        [SerializeField] private GameObject _levelUpEffectPrefab;
        [SerializeField] private float _fillSpeed = 5f;
        
        #endregion
        
        #region Private Fields
        
        private ILevelSystem _levelSystem;
        private float _targetFillAmount;
        private int _lastXP;
        private int _lastLevel;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (_levelSystem == null)
            {
                Debug.LogError("LevelSystem не инициализирован. Используйте метод Inject.");
                return;
            }
            
            _levelSystem.OnXPChanged += HandleXPChanged;
            _levelSystem.OnLevelUp += HandleLevelUp;
            
            UpdateUI(true);
        }
        
        private void Update()
        {
            if (_fillImage.fillAmount != _targetFillAmount)
            {
                _fillImage.fillAmount = Mathf.Lerp(_fillImage.fillAmount, _targetFillAmount, Time.deltaTime * _fillSpeed);
                
                // Если мы достаточно близко к целевому значению, установим его точно
                if (Mathf.Abs(_fillImage.fillAmount - _targetFillAmount) < 0.01f)
                {
                    _fillImage.fillAmount = _targetFillAmount;
                }
            }
        }
        
        private void OnDestroy()
        {
            if (_levelSystem != null)
            {
                _levelSystem.OnXPChanged -= HandleXPChanged;
                _levelSystem.OnLevelUp -= HandleLevelUp;
            }
        }
        
        #endregion
        
        #region IInjectable Implementation
        
        public void Inject(DIContainer container)
        {
            _levelSystem = container.Resolve<ILevelSystem>();
            _lastXP = _levelSystem.CurrentXP;
            _lastLevel = _levelSystem.CurrentLevel;
        }
        
        #endregion
        
        #region Private Methods
        
        private void HandleXPChanged(int currentXP, int amountAdded)
        {
            _lastXP = currentXP;
            UpdateUI();
        }
        
        private void HandleLevelUp(int newLevel)
        {
            _lastLevel = newLevel;
            
            // Показываем эффект повышения уровня
            if (_levelUpEffectPrefab != null)
            {
                var effect = Instantiate(_levelUpEffectPrefab, transform);
                Destroy(effect, 2f); // Уничтожаем эффект через 2 секунды
            }
            
            UpdateUI();
        }
        
        private void UpdateUI(bool instant = false)
        {
            // Обновляем текст уровня
            if (_levelText != null)
            {
                _levelText.text = _lastLevel.ToString();
            }
            
            // Обновляем текст опыта
            if (_xpText != null)
            {
                int requiredXP = _levelSystem.RequiredXPForNextLevel;
                int currentLevelXP = _levelSystem.CalculateRequiredXP(_lastLevel);
                int currentProgress = _lastXP - currentLevelXP;
                int requiredProgress = requiredXP - currentLevelXP;
                
                _xpText.text = $"{currentProgress}/{requiredProgress}";
            }
            
            // Обновляем заполнение прогресс-бара
            _targetFillAmount = _levelSystem.LevelProgress;
            
            if (instant && _fillImage != null)
            {
                _fillImage.fillAmount = _targetFillAmount;
            }
        }
        
        #endregion
    }
} 