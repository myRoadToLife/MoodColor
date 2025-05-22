using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.IO;

namespace App.Editor
{
    public static class AddressableSetup
    {
        private const string UIGroupName = "UI";
        
        [MenuItem("MoodColor/Addressables/Setup FriendsPanel")]
        public static void SetupFriendsPanelAddressable()
        {
            // Путь к префабу
            string prefabPath = "Assets/App/Addressables/UI/Panels/UIPanel_Friends.prefab";
            
            // Проверяем существование префаба
            if (!File.Exists(Path.Combine(Application.dataPath.Replace("Assets", ""), prefabPath)))
            {
                Debug.LogError($"[AddressableSetup] Префаб не найден по пути: {prefabPath}");
                return;
            }
            
            // Получаем GUID ассета
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[AddressableSetup] Не удалось получить GUID для префаба: {prefabPath}");
                return;
            }
            
            // Получаем настройки Addressables
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[AddressableSetup] Настройки Addressables не найдены. Проверьте, что Addressable Assets инициализированы в проекте.");
                return;
            }
            
            // Находим группу UI или создаем ее, если она не существует
            AddressableAssetGroup uiGroup = null;
            foreach (var group in settings.groups)
            {
                if (group.Name == UIGroupName)
                {
                    uiGroup = group;
                    break;
                }
            }
            
            if (uiGroup == null)
            {
                Debug.LogError($"[AddressableSetup] Группа {UIGroupName} не найдена в настройках Addressables.");
                return;
            }
            
            // Адрес для префаба
            string address = "UIPanel_Friends";
            
            // Проверяем, есть ли уже этот ассет в Addressables
            bool exists = false;
            AddressableAssetEntry existingEntry = null;
            
            foreach (var group in settings.groups)
            {
                var existingGuid = settings.FindAssetEntry(guid);
                if (existingGuid != null)
                {
                    exists = true;
                    existingEntry = existingGuid;
                    break;
                }
            }
            
            // Если ассет уже существует в Addressables, обновляем его
            if (exists && existingEntry != null)
            {
                // Если ассет уже в нужной группе с нужным адресом, ничего не делаем
                if (existingEntry.parentGroup == uiGroup && existingEntry.address == address)
                {
                    Debug.Log($"[AddressableSetup] Префаб уже настроен как Addressable с адресом {address} в группе {UIGroupName}.");
                    return;
                }
                
                // Удаляем существующую запись
                settings.RemoveAssetEntry(guid);
            }
            
            // Создаем новую запись Addressable
            var newEntry = settings.CreateOrMoveEntry(guid, uiGroup);
            newEntry.address = address;
            
            // Добавляем метки
            newEntry.SetLabel("Prefab", true);
            newEntry.SetLabel("UI", true);
            
            // Сохраняем настройки
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryAdded, newEntry, true);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[AddressableSetup] Префаб успешно настроен как Addressable с адресом {address} в группе {UIGroupName}.");
        }
        
        [MenuItem("MoodColor/Addressables/Setup FriendSearchPanel")]
        public static void SetupFriendSearchPanelAddressable()
        {
            // Путь к префабу
            string prefabPath = "Assets/App/Addressables/UI/Panels/UIPanel_FriendSearch.prefab";
            
            // Проверяем существование префаба
            if (!File.Exists(Path.Combine(Application.dataPath.Replace("Assets", ""), prefabPath)))
            {
                Debug.LogError($"[AddressableSetup] Префаб не найден по пути: {prefabPath}");
                return;
            }
            
            // Получаем GUID ассета
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[AddressableSetup] Не удалось получить GUID для префаба: {prefabPath}");
                return;
            }
            
            // Получаем настройки Addressables
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[AddressableSetup] Настройки Addressables не найдены. Проверьте, что Addressable Assets инициализированы в проекте.");
                return;
            }
            
            // Находим группу UI
            AddressableAssetGroup uiGroup = null;
            foreach (var group in settings.groups)
            {
                if (group.Name == UIGroupName)
                {
                    uiGroup = group;
                    break;
                }
            }
            
            if (uiGroup == null)
            {
                Debug.LogError($"[AddressableSetup] Группа {UIGroupName} не найдена в настройках Addressables.");
                return;
            }
            
            // Адрес для префаба
            string address = "UIPanel_FriendSearch";
            
            // Проверяем, есть ли уже этот ассет в Addressables
            bool exists = false;
            AddressableAssetEntry existingEntry = null;
            
            foreach (var group in settings.groups)
            {
                var existingGuid = settings.FindAssetEntry(guid);
                if (existingGuid != null)
                {
                    exists = true;
                    existingEntry = existingGuid;
                    break;
                }
            }
            
            // Если ассет уже существует в Addressables, обновляем его
            if (exists && existingEntry != null)
            {
                // Если ассет уже в нужной группе с нужным адресом, ничего не делаем
                if (existingEntry.parentGroup == uiGroup && existingEntry.address == address)
                {
                    Debug.Log($"[AddressableSetup] Префаб уже настроен как Addressable с адресом {address} в группе {UIGroupName}.");
                    return;
                }
                
                // Удаляем существующую запись
                settings.RemoveAssetEntry(guid);
            }
            
            // Создаем новую запись Addressable
            var newEntry = settings.CreateOrMoveEntry(guid, uiGroup);
            newEntry.address = address;
            
            // Добавляем метки
            newEntry.SetLabel("Prefab", true);
            newEntry.SetLabel("UI", true);
            
            // Сохраняем настройки
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryAdded, newEntry, true);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[AddressableSetup] Префаб успешно настроен как Addressable с адресом {address} в группе {UIGroupName}.");
        }
    }
} 