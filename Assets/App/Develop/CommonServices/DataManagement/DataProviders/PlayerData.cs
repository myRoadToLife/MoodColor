using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Emotion;
using UnityEngine;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    [Serializable]
    public class EmotionData : IEquatable<EmotionData>
    {
        public int Value;
        public Color Color;

        public bool Equals(EmotionData other)
        {
            if (other is null) return false;
            return Value == other.Value && Color.Equals(other.Color);
        }

        public override bool Equals(object obj) => Equals(obj as EmotionData);
        
        public override int GetHashCode()
        {
            unchecked
            {
                return (Value * 397) ^ Color.GetHashCode();
            }
        }

        public static bool operator ==(EmotionData left, EmotionData right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
            
        public static bool operator !=(EmotionData left, EmotionData right) => 
            !(left == right);
    }

    [Serializable]
    public class PlayerData : ISaveData
    {
        public Dictionary<EmotionTypes, EmotionData> EmotionData;
    }
}
