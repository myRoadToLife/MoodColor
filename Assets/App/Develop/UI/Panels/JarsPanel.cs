using System.Collections.Generic;
using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.AppServices.Firebase.Database.Services;
using App.Develop.CommonServices.Emotion;
using App.Develop.DI;
using App.Develop.UI.Base;
using App.Develop.UI.Components;
using UnityEngine;

namespace App.Develop.UI.Panels
{
    /// <summary>
    /// Панель для отображения банок с эмоциями
    /// </summary>
    public class JarsPanel : BasePanel, IInjectable
    {
        #region UI Elements
        [Header("UI Elements")]
        [SerializeField] private Transform _jarsContainer;    // Контейнер для банок
        [SerializeField] private GameObject _jarPrefab;       // Префаб банки
        #endregion

        #region Private Fields
        private DatabaseService _databaseService;
        private EmotionService _emotionService;
        private Dictionary<EmotionTypes, JarView> _jars = new Dictionary<EmotionTypes, JarView>();
        #endregion

        #region IInjectable Implementation
        public void Inject(DIContainer container)
        {
            _databaseService = container.Resolve<DatabaseService>();
            _emotionService = container.Resolve<EmotionService>();
            
            // Инициализируем банки после инъекции зависимостей
            InitializeJars();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Обновляет данные для конкретной банки
        /// </summary>
        public void UpdateJar(EmotionTypes type, JarData jarData)
        {
            if (_jars.TryGetValue(type, out var jarController))
            {
                jarController.UpdateJarData(jarData);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Инициализирует все банки
        /// </summary>
        private async void InitializeJars()
        {
            try
            {
                // Проверяем, что компоненты назначены
                if (_jarsContainer == null || _jarPrefab == null)
                {
                    Debug.LogError("Не все компоненты назначены для JarsPanel");
                    return;
                }

                // Получаем данные о банках пользователя из базы данных
                Dictionary<string, JarData> jarData = null;
                
                try
                {
                    jarData = await _databaseService.GetUserJars();
                    Debug.Log($"Загружено банок: {jarData?.Count ?? 0}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Ошибка при загрузке банок: {ex.Message}");
                }

                // Создаем визуальные представления для каждой банки
                foreach (EmotionTypes type in System.Enum.GetValues(typeof(EmotionTypes)))
                {
                    var jarObj = Instantiate(_jarPrefab, _jarsContainer);
                    var jarView = jarObj.GetComponent<JarView>();
                    
                    if (jarView == null)
                    {
                        Debug.LogError("Префаб банки не содержит компонент JarView");
                        continue;
                    }

                    // Проверяем, есть ли данные для банки этой эмоции
                    if (jarData != null && jarData.ContainsKey(type.ToString()))
                    {
                        jarView.Initialize(jarData[type.ToString()], type);
                        Debug.Log($"Инициализирована банка для {type} из данных Firebase");
                    }
                    else
                    {
                        // Создаем новую банку по умолчанию
                        jarView.Initialize(new JarData
                        {
                            Type = type.ToString(),
                            Level = 1,
                            Capacity = 100,
                            CurrentAmount = 0
                        }, type);
                        Debug.Log($"Создана новая банка для {type} с дефолтными значениями");
                    }
                    
                    _jars.Add(type, jarView);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Ошибка при инициализации банок: {ex.Message}");
            }
        }
        #endregion
    }
} 