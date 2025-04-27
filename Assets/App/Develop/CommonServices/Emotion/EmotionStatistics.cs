using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Develop.CommonServices.Emotion
{
    public enum TimeOfDay
    {
        Night = 0,     // 00:00 - 05:59
        Morning = 1,   // 06:00 - 11:59
        Afternoon = 2, // 12:00 - 17:59
        Evening = 3    // 18:00 - 23:59
    }

    public class EmotionTimeStats
    {
        public TimeOfDay TimeOfDay { get; set; }
        public Dictionary<EmotionTypes, float> AverageIntensities { get; set; }
        public Dictionary<EmotionTypes, int> EmotionCounts { get; set; }
        public float AverageValue { get; set; }
        public int TotalEntries { get; set; }
    }

    public class EmotionFrequencyStats
    {
        public DateTime Date { get; set; }
        public int EntryCount { get; set; }
        public TimeSpan AverageTimeBetweenEntries { get; set; }
        public Dictionary<EmotionTypes, int> EmotionTypeCounts { get; set; }
    }

    public class EmotionCombinationStats
    {
        public EmotionTypes FirstEmotion { get; set; }
        public EmotionTypes SecondEmotion { get; set; }
        public int CombinationCount { get; set; }
        public float AverageResultIntensity { get; set; }
        public EmotionTypes MostCommonResult { get; set; }
    }

    public class EmotionTrendStats
    {
        public DateTime Date { get; set; }
        public Dictionary<EmotionTypes, float> AverageIntensities { get; set; }
        public EmotionTypes DominantEmotion { get; set; }
        public float TrendValue { get; set; } // Положительное значение - рост, отрицательное - спад
    }

    public static class TimeHelper
    {
        public static TimeOfDay GetTimeOfDay(DateTime time)
        {
            int hour = time.Hour;
            
            if (hour >= 0 && hour < 6)
                return TimeOfDay.Night;
            if (hour >= 6 && hour < 12)
                return TimeOfDay.Morning;
            if (hour >= 12 && hour < 18)
                return TimeOfDay.Afternoon;
            
            return TimeOfDay.Evening;
        }
    }
} 