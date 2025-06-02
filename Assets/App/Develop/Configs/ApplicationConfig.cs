using UnityEngine;

namespace App.Develop.Configs
{
    /// <summary>
    /// Основная конфигурация приложения
    /// </summary>
    [CreateAssetMenu(fileName = "ApplicationConfig", menuName = "MoodColor/ApplicationConfig")]
    public class ApplicationConfig : ScriptableObject
    {
        [Header("Firebase Settings")]
        [SerializeField] private string _databaseUrl = "https://moodcolor-3ac59-default-rtdb.firebaseio.com/";
        [SerializeField] private string _firebaseAppName = "MoodColorApp";
        
        [Header("Application Settings")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private bool _enableVSync = false;
        
        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private bool _enableFirebaseDebug = false;
        
        public string DatabaseUrl => _databaseUrl;
        public string FirebaseAppName => _firebaseAppName;
        public int TargetFrameRate => _targetFrameRate;
        public bool EnableVSync => _enableVSync;
        public bool EnableDebugLogs => _enableDebugLogs;
        public bool EnableFirebaseDebug => _enableFirebaseDebug;
        
        private void OnValidate()
        {
            if (_targetFrameRate < 30)
                _targetFrameRate = 30;
            if (_targetFrameRate > 120)
                _targetFrameRate = 120;
        }
    }
} 