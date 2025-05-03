using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

namespace App.Tests.EditMode.TestHelpers
{
    /// <summary>
    /// Интерфейс для мока DatabaseReference
    /// </summary>
    public interface IMockDatabaseReference
    {
        IMockDatabaseReference Child(string path);
        Task<IMockDataSnapshot> GetValueAsync();
        Task SetValueAsync(object value);
        Task UpdateChildrenAsync(IDictionary<string, object> update);
        Task RemoveValueAsync();
    }
    
    /// <summary>
    /// Интерфейс для мока DataSnapshot
    /// </summary>
    public interface IMockDataSnapshot
    {
        bool Exists { get; }
        object Value { get; }
    }
    
    /// <summary>
    /// Мок для DatabaseReference, чтобы тестировать без реального подключения к Firebase
    /// </summary>
    public class MockDatabaseReference : IMockDatabaseReference
    {
        private Dictionary<string, object> _data = new Dictionary<string, object>();
        private readonly string _path;
        private readonly MockFirebaseDatabase _database;
        
        public MockDatabaseReference(string path, MockFirebaseDatabase database)
        {
            _path = path;
            _database = database;
        }
        
        public IMockDatabaseReference Child(string path)
        {
            return new MockDatabaseReference(string.IsNullOrEmpty(_path) ? path : $"{_path}/{path}", _database);
        }
        
        public Task<IMockDataSnapshot> GetValueAsync()
        {
            var data = _database.GetData(_path);
            var snapshot = new MockDataSnapshot(data, _path);
            return Task.FromResult<IMockDataSnapshot>(snapshot);
        }
        
        public Task SetValueAsync(object value)
        {
            _database.SetData(_path, value);
            return Task.CompletedTask;
        }
        
        public Task UpdateChildrenAsync(IDictionary<string, object> update)
        {
            foreach (var item in update)
            {
                string childPath = string.IsNullOrEmpty(_path) ? item.Key : $"{_path}/{item.Key}";
                _database.SetData(childPath, item.Value);
            }
            
            return Task.CompletedTask;
        }
        
        public Task RemoveValueAsync()
        {
            _database.RemoveData(_path);
            return Task.CompletedTask;
        }
    }
    
    /// <summary>
    /// Мок для DataSnapshot
    /// </summary>
    public class MockDataSnapshot : IMockDataSnapshot
    {
        private readonly object _data;
        private readonly string _path;
        
        public MockDataSnapshot(object data, string path)
        {
            _data = data;
            _path = path;
        }
        
        public bool Exists => _data != null;
        
        public object Value => _data;
    }
    
    /// <summary>
    /// Мок для FirebaseDatabase
    /// </summary>
    public class MockFirebaseDatabase
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
        private readonly MockDatabaseReference _rootReference;
        
        public MockFirebaseDatabase()
        {
            _rootReference = new MockDatabaseReference("", this);
        }
        
        public static MockFirebaseDatabase GetInstance(string url = null)
        {
            return new MockFirebaseDatabase();
        }
        
        public IMockDatabaseReference RootReference => _rootReference;
        
        /// <summary>
        /// Получить данные по указанному пути
        /// </summary>
        /// <param name="path">Путь к данным</param>
        /// <returns>Данные или null, если не найдено</returns>
        internal object GetData(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return _data;
            }
            
            string[] pathParts = path.Split('/');
            Dictionary<string, object> currentLevel = _data;
            
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                if (!currentLevel.ContainsKey(pathParts[i]))
                {
                    return null;
                }
                
                currentLevel = currentLevel[pathParts[i]] as Dictionary<string, object>;
                if (currentLevel == null)
                {
                    return null;
                }
            }
            
            string lastPart = pathParts[pathParts.Length - 1];
            
            if (!currentLevel.ContainsKey(lastPart))
            {
                return null;
            }
            
            return currentLevel[lastPart];
        }
        
        /// <summary>
        /// Установить данные по указанному пути
        /// </summary>
        /// <param name="path">Путь к данным</param>
        /// <param name="value">Данные для установки</param>
        internal void SetData(string path, object value)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Попытка установить данные для пустого пути");
                return;
            }
            
            string[] pathParts = path.Split('/');
            Dictionary<string, object> currentLevel = _data;
            
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                if (!currentLevel.ContainsKey(pathParts[i]))
                {
                    currentLevel[pathParts[i]] = new Dictionary<string, object>();
                }
                
                var nextLevel = currentLevel[pathParts[i]] as Dictionary<string, object>;
                if (nextLevel == null)
                {
                    nextLevel = new Dictionary<string, object>();
                    currentLevel[pathParts[i]] = nextLevel;
                }
                
                currentLevel = nextLevel;
            }
            
            string lastPart = pathParts[pathParts.Length - 1];
            
            if (value is IDictionary<string, object> dict)
            {
                // Если это словарь, копируем его элементы
                if (!currentLevel.ContainsKey(lastPart))
                {
                    currentLevel[lastPart] = new Dictionary<string, object>();
                }
                
                var targetDict = currentLevel[lastPart] as Dictionary<string, object>;
                if (targetDict == null)
                {
                    targetDict = new Dictionary<string, object>();
                    currentLevel[lastPart] = targetDict;
                }
                
                foreach (var item in dict)
                {
                    targetDict[item.Key] = item.Value;
                }
            }
            else
            {
                // Иначе просто устанавливаем значение
                currentLevel[lastPart] = value;
            }
        }
        
        /// <summary>
        /// Удалить данные по указанному пути
        /// </summary>
        /// <param name="path">Путь к данным</param>
        internal void RemoveData(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                _data.Clear();
                return;
            }
            
            string[] pathParts = path.Split('/');
            Dictionary<string, object> currentLevel = _data;
            
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                if (!currentLevel.ContainsKey(pathParts[i]))
                {
                    return;
                }
                
                currentLevel = currentLevel[pathParts[i]] as Dictionary<string, object>;
                if (currentLevel == null)
                {
                    return;
                }
            }
            
            string lastPart = pathParts[pathParts.Length - 1];
            
            if (currentLevel.ContainsKey(lastPart))
            {
                currentLevel.Remove(lastPart);
            }
        }
        
        /// <summary>
        /// Очистить все данные
        /// </summary>
        public void Clear()
        {
            _data.Clear();
        }
    }
} 