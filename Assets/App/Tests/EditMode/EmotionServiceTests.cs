using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Tests.EditMode.TestHelpers;
using UnityEngine;
using NUnit.Framework;

namespace App.Tests.EditMode
{
    public class EmotionServiceTests
    {
        private EmotionService _emotionService;
        private Dictionary<EmotionTypes, EmotionData> _initialEmotions;
        private PlayerDataProvider _mockPlayerDataProvider;
        private MockConfigsProviderService _mockConfigsProvider;

        [SetUp]
        public void Setup()
        {
            Debug.Log("Setup: Initializing EmotionServiceTests");
            
            _initialEmotions = new Dictionary<EmotionTypes, EmotionData>
            {
                {
                    EmotionTypes.Joy,
                    new EmotionData
                    {
                        Type = EmotionTypes.Joy.ToString(),
                        Value = 0.5f,
                        LastUpdate = DateTime.UtcNow
                    }
                },
                {
                    EmotionTypes.Sadness,
                    new EmotionData
                    {
                        Type = EmotionTypes.Sadness.ToString(),
                        Value = 0.5f,
                        LastUpdate = DateTime.UtcNow
                    }
                }
            };

            Debug.Log($"Setup: Created initial emotions - Joy: {_initialEmotions[EmotionTypes.Joy].Value}, Sadness: {_initialEmotions[EmotionTypes.Sadness].Value}");

            _mockConfigsProvider = new MockConfigsProviderService();
            _mockPlayerDataProvider = new PlayerDataProvider(new MockSaveLoadService(), _mockConfigsProvider);
            
            // Инициализируем данные через protected метод GetOriginData
            var playerData = new PlayerData { EmotionData = _initialEmotions };
            var dataProvider = _mockPlayerDataProvider as DataProvider<PlayerData>;
            typeof(DataProvider<PlayerData>)
                .GetField("_data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(dataProvider, playerData);

            _emotionService = new EmotionService(_mockPlayerDataProvider, _mockConfigsProvider);
            Debug.Log("Setup: Created EmotionService with initial emotions");

            // Проверяем начальное состояние истории
            var initialHistory = _emotionService.GetEmotionHistory(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
            Debug.Log($"Setup: Initial history count: {initialHistory.Count()}");
            
            foreach (var entry in initialHistory)
            {
                Debug.Log($"Setup: Initial history entry - Type: {entry.EmotionData.Type}, Value: {entry.EmotionData.Value}, Timestamp: {entry.Timestamp}");
            }
        }

        [TearDown]
        public void TearDown()
        {
            _emotionService = null;
            _initialEmotions = null;
        }

        [Test]
        public void UpdateEmotionValue_ShouldUpdateValueAndRaiseEvent()
        {
            // Arrange
            var emotionType = EmotionTypes.Joy;
            var newValue = 0.5f;
            var eventRaised = false;
            
            _emotionService.OnEmotionEvent += (sender, e) =>
            {
                if (e.Type == emotionType && e.EventType == EmotionEventType.ValueChanged)
                {
                    eventRaised = true;
                    Assert.That(e.Value, Is.EqualTo(newValue));
                }
            };

            // Act
            _emotionService.UpdateEmotionValue(emotionType, newValue);

            // Assert
            var emotion = _emotionService.GetEmotion(emotionType);
            Assert.That(emotion.Value, Is.EqualTo(newValue));
            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void UpdateEmotionIntensity_ShouldUpdateIntensityAndRaiseEvent()
        {
            // Arrange
            var emotionType = EmotionTypes.Joy;
            var intensity = 0.7f;
            var eventRaised = false;
            
            _emotionService.OnEmotionEvent += (sender, e) =>
            {
                if (e.Type == emotionType && e.EventType == EmotionEventType.IntensityChanged)
                {
                    eventRaised = true;
                    Assert.That(e.Intensity, Is.EqualTo(intensity));
                }
            };

            // Act
            _emotionService.UpdateEmotionIntensity(emotionType, intensity);

            // Assert
            var emotion = _emotionService.GetEmotion(emotionType);
            Assert.That(emotion.Intensity, Is.EqualTo(intensity));
            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void TryMixEmotions_ShouldMixEmotionsAndRaiseEvent()
        {
            // Arrange
            var source1 = EmotionTypes.Joy;
            var source2 = EmotionTypes.Sadness;
            var eventRaised = false;

            _emotionService.UpdateEmotionValue(source1, 0.5f);
            _emotionService.UpdateEmotionValue(source2, 0.3f);
            
            _emotionService.OnEmotionEvent += (sender, e) =>
            {
                if (e.EventType == EmotionEventType.EmotionMixed)
                {
                    eventRaised = true;
                }
            };

            // Act
            var success = _emotionService.TryMixEmotions(source1, source2);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void GetEmotionHistory_ShouldReturnCorrectHistory()
        {
            // Arrange
            var emotionType = EmotionTypes.Joy;
            var startTime = DateTime.UtcNow.AddHours(-1);
            var endTime = DateTime.UtcNow.AddMinutes(5); // Добавляем 5 минут, чтобы включить записи, созданные во время теста

            Debug.Log($"Test: GetEmotionHistory - Start time: {startTime}, End time: {endTime}");

            // Проверяем начальное состояние истории
            var initialHistory = _emotionService.GetEmotionHistory(startTime, endTime);
            Debug.Log($"Test: Initial history count: {initialHistory.Count()}");

            foreach (var entry in initialHistory)
            {
                Debug.Log($"Test: Initial history entry - Type: {entry.EmotionData.Type}, Value: {entry.EmotionData.Value}, Timestamp: {entry.Timestamp}");
            }

            // Act
            Debug.Log("Test: Updating Joy emotion value to 0.7");
            _emotionService.UpdateEmotionValue(emotionType, 0.7f);

            // Добавим небольшую задержку
            System.Threading.Thread.Sleep(100);

            // Проверяем историю после обновления
            var history = _emotionService.GetEmotionHistory(startTime, endTime);
            Debug.Log($"Test: Final history count: {history.Count()}");

            foreach (var entry in history)
            {
                Debug.Log($"Test: History entry - Type: {entry.EmotionData.Type}, Value: {entry.EmotionData.Value}, Timestamp: {entry.Timestamp}");
            }

            // Assert
            Assert.That(history.Count, Is.GreaterThanOrEqualTo(1), "История должна содержать хотя бы одну запись после обновления");
            Assert.That(history.Any(h => h.EmotionData.Type == emotionType.ToString() && Math.Abs(h.EmotionData.Value - 0.7f) < 0.001f), 
                Is.True, "История должна содержать запись Joy со значением 0.7");
        }

        [Test]
        public void GetPopularEmotionCombinations_ShouldReturnCorrectCombinationStats()
        {
            // Arrange
            _emotionService.UpdateEmotionValue(EmotionTypes.Joy, 0.5f);
            _emotionService.UpdateEmotionValue(EmotionTypes.Sadness, 0.3f);
            _emotionService.TryMixEmotions(EmotionTypes.Joy, EmotionTypes.Sadness);

            // Act
            var combinationStats = _emotionService.GetPopularEmotionCombinations();

            // Assert
            Assert.That(combinationStats, Is.Not.Null);
            Assert.That(combinationStats.Count, Is.GreaterThan(0));
            Assert.That(combinationStats[0].CombinationCount, Is.GreaterThan(0));
        }

        [Test]
        public void GetEmotionTrends_ShouldReturnCorrectTrendStats()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddDays(-1);
            var endTime = DateTime.UtcNow.AddMinutes(5); // Добавляем 5 минут, чтобы включить записи, созданные во время теста
            
            Debug.Log($"Test: GetEmotionTrends - Start time: {startTime}, End time: {endTime}");
            
            _emotionService.UpdateEmotionValue(EmotionTypes.Joy, 0.5f);
            System.Threading.Thread.Sleep(100); // Небольшая задержка для разных временных меток
            _emotionService.UpdateEmotionValue(EmotionTypes.Joy, 0.7f);

            Debug.Log("Test: History after updates:");
            var history = _emotionService.GetEmotionHistory(startTime, endTime);
            foreach (var entry in history)
            {
                Debug.Log($"Test: History entry - Type: {entry.EmotionData.Type}, Value: {entry.EmotionData.Value}, Timestamp: {entry.Timestamp}");
            }

            // Act
            Debug.Log("Test: Getting emotion trends");
            var trendStats = _emotionService.GetEmotionTrends(startTime, endTime);

            // Assert
            Debug.Log($"Test: Got {trendStats.Count} trend stats");
            foreach (var stat in trendStats)
            {
                Debug.Log($"Test: Trend stat for {stat.Date}: DominantEmotion={stat.DominantEmotion}, TrendValue={stat.TrendValue}");
                foreach (var kvp in stat.AverageIntensities)
                {
                    Debug.Log($"Test: - Average {kvp.Key}: {kvp.Value}");
                }
            }

            Assert.That(trendStats, Is.Not.Null, "Статистика трендов не должна быть null");
            Assert.That(trendStats.Count, Is.GreaterThanOrEqualTo(1), "Должна быть хотя бы одна запись тренда");
            
            if (trendStats.Count > 0)
            {
                Assert.That(trendStats[0].AverageIntensities.ContainsKey(EmotionTypes.Joy), Is.True, "Тренд должен содержать данные для Joy");
                Assert.That(trendStats[0].TrendValue, Is.GreaterThan(0), "Тренд должен быть положительным");
            }
        }
    }
} 