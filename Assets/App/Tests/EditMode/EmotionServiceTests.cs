using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.DataManagement.DataProviders;
using UnityEngine;
using NUnit.Framework;

namespace App.Tests.EditMode
{
    public class EmotionServiceTests
    {
        private EmotionService _emotionService;
        private Dictionary<EmotionTypes, EmotionData> _initialEmotions;

        [SetUp]
        public void Setup()
        {
            _initialEmotions = new Dictionary<EmotionTypes, EmotionData>
            {
                {
                    EmotionTypes.Joy,
                    new EmotionData
                    {
                        Type = EmotionTypes.Joy.ToString(),
                        Value = 0f,
                        LastUpdate = DateTime.UtcNow
                    }
                },
                {
                    EmotionTypes.Sadness,
                    new EmotionData
                    {
                        Type = EmotionTypes.Sadness.ToString(),
                        Value = 0f,
                        LastUpdate = DateTime.UtcNow
                    }
                }
            };

            _emotionService = new EmotionService(_initialEmotions);
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
            var endTime = DateTime.UtcNow;

            _emotionService.UpdateEmotionValue(emotionType, 0.5f);
            _emotionService.UpdateEmotionValue(emotionType, 0.7f);

            // Act
            var history = _emotionService.GetEmotionHistory(startTime, endTime);

            // Assert
            Assert.That(history.Count(), Is.EqualTo(2));
            Assert.That(history.All(h => h.Timestamp >= startTime && h.Timestamp <= endTime), Is.True);
        }

        [Test]
        public void GetEmotionsByTimeOfDay_ShouldReturnCorrectStats()
        {
            // Arrange
            var emotionType = EmotionTypes.Joy;
            _emotionService.UpdateEmotionValue(emotionType, 0.5f);
            System.Threading.Thread.Sleep(100); // Небольшая задержка для разных временных меток
            _emotionService.UpdateEmotionValue(emotionType, 0.7f);

            // Act
            var timeStats = _emotionService.GetEmotionsByTimeOfDay();

            // Assert
            Assert.That(timeStats, Is.Not.Null);
            Assert.That(timeStats.Count, Is.GreaterThan(0));

            var currentTimeOfDay = TimeHelper.GetTimeOfDay(DateTime.UtcNow);
            Assert.That(timeStats.ContainsKey(currentTimeOfDay));
            Assert.That(timeStats[currentTimeOfDay].TotalEntries, Is.EqualTo(2));
        }

        [Test]
        public void GetLoggingFrequency_ShouldReturnCorrectFrequencyStats()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddDays(-1);
            var endTime = DateTime.UtcNow;
            
            _emotionService.UpdateEmotionValue(EmotionTypes.Joy, 0.5f);
            System.Threading.Thread.Sleep(100);
            _emotionService.UpdateEmotionValue(EmotionTypes.Sadness, 0.3f);

            // Act
            var frequencyStats = _emotionService.GetLoggingFrequency(startTime, endTime);

            // Assert
            Assert.That(frequencyStats, Is.Not.Null);
            Assert.That(frequencyStats.Count, Is.EqualTo(1)); // Все записи в один день
            Assert.That(frequencyStats[0].EntryCount, Is.EqualTo(2));
            Assert.That(frequencyStats[0].EmotionTypeCounts[EmotionTypes.Joy], Is.EqualTo(1));
            Assert.That(frequencyStats[0].EmotionTypeCounts[EmotionTypes.Sadness], Is.EqualTo(1));
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
            var endTime = DateTime.UtcNow;
            
            _emotionService.UpdateEmotionValue(EmotionTypes.Joy, 0.5f);
            System.Threading.Thread.Sleep(100);
            _emotionService.UpdateEmotionValue(EmotionTypes.Joy, 0.7f);

            // Act
            var trendStats = _emotionService.GetEmotionTrends(startTime, endTime);

            // Assert
            Assert.That(trendStats, Is.Not.Null);
            Assert.That(trendStats.Count, Is.EqualTo(1)); // Все записи в один день
            Assert.That(trendStats[0].AverageIntensities.ContainsKey(EmotionTypes.Joy));
            Assert.That(trendStats[0].TrendValue, Is.GreaterThan(0)); // Тренд положительный
        }
    }
} 