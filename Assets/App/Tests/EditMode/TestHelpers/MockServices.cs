using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.Configs.Common.Emotion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace App.Tests.EditMode.TestHelpers
{
    public class MockSaveLoadService : ISaveLoadService
    {
        public void Save<TData>(TData data) where TData : ISaveData
        { }
        public bool TryLoad<TData>(out TData data) where TData : ISaveData
        {
            data = default;
            return false;
        }
    }

    public class MockResourcesLoader : IResourcesLoader
    {
        public T LoadAsset<T>(string path) where T : Object
        {
            if (typeof(T).IsSubclassOf(typeof(ScriptableObject)))
            {
                return ScriptableObject.CreateInstance(typeof(T)) as T;
            }
            return default;
        }

        public void UnloadAsset(Object asset) { }
    }

    public class MockConfigsProviderService : IConfigsProvider
    {
        private readonly StartEmotionConfig _startEmotionConfig;
        private readonly Dictionary<EmotionTypes, EmotionConfig> _emotionConfigs;

        public MockConfigsProviderService()
        {
            _startEmotionConfig = ScriptableObject.CreateInstance<StartEmotionConfig>();
            _emotionConfigs = new Dictionary<EmotionTypes, EmotionConfig>();
            InitializeStartEmotionConfig();
            InitializeEmotionConfigs();
        }

        public StartEmotionConfig StartEmotionConfig => _startEmotionConfig;

        private void InitializeStartEmotionConfig()
        {
            #if UNITY_EDITOR
            var serializedObject = new SerializedObject(_startEmotionConfig);
            var startValuesProperty = serializedObject.FindProperty("_startValues");
            
            foreach (EmotionTypes type in System.Enum.GetValues(typeof(EmotionTypes)))
            {
                startValuesProperty.InsertArrayElementAtIndex(startValuesProperty.arraySize);
                var element = startValuesProperty.GetArrayElementAtIndex(startValuesProperty.arraySize - 1);
                element.FindPropertyRelative("Type").enumValueIndex = (int)type;
                element.FindPropertyRelative("Value").floatValue = 0f;
                element.FindPropertyRelative("Color").colorValue = Color.white;
            }
            
            serializedObject.ApplyModifiedProperties();
            #endif
        }

        private void InitializeEmotionConfigs()
        {
            foreach (EmotionTypes type in System.Enum.GetValues(typeof(EmotionTypes)))
            {
                _emotionConfigs[type] = CreateMockEmotionConfig(type);
            }
        }

        private EmotionConfig CreateMockEmotionConfig(EmotionTypes type)
        {
            var config = ScriptableObject.CreateInstance<EmotionConfig>();
            #if UNITY_EDITOR
            var serializedObject = new SerializedObject(config);
            serializedObject.FindProperty("_type").enumValueIndex = (int)type;
            serializedObject.FindProperty("_maxCapacity").floatValue = 100f;
            serializedObject.FindProperty("_defaultDrainRate").floatValue = 0.5f;
            serializedObject.FindProperty("_bubbleThreshold").floatValue = 80f;
            serializedObject.FindProperty("_baseColor").colorValue = Color.white;
            serializedObject.ApplyModifiedProperties();
            #endif
            return config;
        }

        public EmotionConfig LoadEmotionConfig(EmotionTypes type)
        {
            if (_emotionConfigs.TryGetValue(type, out var config))
            {
                return config;
            }

            var newConfig = CreateMockEmotionConfig(type);
            _emotionConfigs[type] = newConfig;
            return newConfig;
        }

        public IEnumerable<EmotionTypes> GetAllEmotionTypes()
        {
            return _emotionConfigs.Keys;
        }

        public bool HasConfig(EmotionTypes type)
        {
            return _emotionConfigs.ContainsKey(type);
        }

        public IReadOnlyDictionary<EmotionTypes, EmotionConfig> GetAllConfigs()
        {
            return _emotionConfigs;
        }
    }
} 