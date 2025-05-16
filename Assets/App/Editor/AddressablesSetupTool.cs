using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace App.Editor
{
    public class AddressablesSetupTool : EditorWindow
    {
        #region Private Fields
        // Настройки для создаваемых групп по умолчанию
        // Добавил Configs и Core как отдельные типы групп для наглядности
        private string[] _groupTypes = { "UI", "Textures", "Prefabs", "Audio", "Materials", "ScriptableObjects", "Scenes", "Configs", "Core" }; 
    
        // Пути к папкам с ресурсами (настройте под свою структуру проекта)
        private Dictionary<string, string> _defaultFolderPaths = new Dictionary<string, string>
        {
            // Основные пути из твоей структуры:
            { "UI", "Assets/App/Addressables/UI" },
            { "Configs", "Assets/App/Addressables/Configs" }, // Для ScriptableObjects или специфичных конфигов
            { "Core", "Assets/App/Addressables/Core" },     // Для основных префабов/ассетов, если они там

            // Предполагаемые пути для других стандартных типов (можешь поправить, если они другие или не нужны):
            // Поменяй "Assets/App/Addressables/..." на реальные пути, если они другие
            { "Textures", "Assets/App/Addressables/Textures" }, 
            { "Prefabs", "Assets/App/Addressables/Prefabs" }, // или Assets/App/Addressables/Core/Prefabs и т.д.
            { "Audio", "Assets/App/Addressables/Audio" },
            { "Materials", "Assets/App/Addressables/Materials" },
            { "ScriptableObjects", "Assets/App/Addressables/ScriptableObjects" }, // Общие SO вне Configs
            { "Scenes", "Assets/App/Addressables/Scenes" } 
        };
    
        private bool _useCustomNaming = false;
        private string _addressNamingPrefix = "";
        private bool _includePathInAddress = true;
        private bool _stripAssetsFromPath = true; // Убирает "Assets/" из пути адреса
        private bool _createGroupsIfMissing = true;
        private bool _assignLabels = true;
        private bool _overwriteExistingEntries = true; // Перезаписывать ли существующие адреса и группы для ассетов
        private Vector2 _scrollPosition;
        #endregion

        #region Editor Window
        [MenuItem("Tools/Мой Проект/Addressables Setup Tool")]
        public static void ShowWindow()
        {
            GetWindow<AddressablesSetupTool>("Addressables Setup");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Инструмент настройки Addressables", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            if (!IsAddressablesInitialized())
            {
                EditorGUILayout.HelpBox("Система Addressables не инициализирована в проекте. " +
                                        "Пожалуйста, создайте настройки через Window > Asset Management > Addressables > Groups, " +
                                        "или нажмите кнопку ниже.", MessageType.Warning);
                if (GUILayout.Button("Инициализировать Addressables"))
                {
                    InitializeAddressableAssetSettings();
                }
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            #region Basic Settings
            EditorGUILayout.LabelField("Основные настройки", EditorStyles.boldLabel);
            _createGroupsIfMissing = EditorGUILayout.Toggle(new GUIContent("Создавать группы (если нет)", "Создаст стандартные группы, если они отсутствуют."), _createGroupsIfMissing);
            _assignLabels = EditorGUILayout.Toggle(new GUIContent("Назначать метки по типу", "Автоматически назначит метку, соответствующую типу группы (например, 'UI', 'Prefabs')."), _assignLabels);
            _overwriteExistingEntries = EditorGUILayout.Toggle(new GUIContent("Перезаписывать существующие", "Если ассет уже добавлен, его адрес и группа будут обновлены."), _overwriteExistingEntries);
            EditorGUILayout.Space(5);
            #endregion

            #region Naming Settings
            EditorGUILayout.LabelField("Настройки именования адресов", EditorStyles.boldLabel);
            _useCustomNaming = EditorGUILayout.Toggle(new GUIContent("Использовать префикс", "Добавить собственный префикс ко всем адресам."), _useCustomNaming);
            if (_useCustomNaming)
            {
                _addressNamingPrefix = EditorGUILayout.TextField("Префикс адреса", _addressNamingPrefix);
            }
        
            _includePathInAddress = EditorGUILayout.Toggle(new GUIContent("Включить путь в адрес", "Использовать относительный путь к файлу (от папки ассета) как часть адреса."), _includePathInAddress);
            if (_includePathInAddress)
            {
                _stripAssetsFromPath = EditorGUILayout.Toggle(new GUIContent("Убрать 'Assets/' из пути", "Если включено, путь в адресе будет начинаться с первой папки внутри 'Assets/'."), _stripAssetsFromPath);
            }
            EditorGUILayout.Space(5);
            #endregion

            #region Resource Group Settings
            EditorGUILayout.LabelField("Настройки групп и путей к ресурсам", EditorStyles.boldLabel);
        
            List<string> keys = _groupTypes.ToList(); // Используем _groupTypes чтобы гарантировать порядок и наличие всех UI элементов
            foreach (string groupType in keys)
            {
                _defaultFolderPaths.TryGetValue(groupType, out string currentPath);
                // Если для какого-то groupType нет пути в словаре, используем стандартный путь
                string displayPath = currentPath ?? $"Assets/App/Addressables/{groupType}";


                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(groupType, GUILayout.Width(120));
            
                string newPath = EditorGUILayout.TextField(displayPath);
                if (newPath != displayPath) // Обновляем, только если изменилось
                {
                    _defaultFolderPaths[groupType] = newPath;
                }
            
                if (GUILayout.Button("Выбрать", GUILayout.Width(80)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel($"Выберите папку для {groupType}", "Assets", "");
                    if (!string.IsNullOrEmpty(selectedPath) && selectedPath.StartsWith(Application.dataPath))
                    {
                        string relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        _defaultFolderPaths[groupType] = relativePath;
                        GUI.FocusControl(null); 
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(10);
            #endregion

            #region Actions
            EditorGUILayout.LabelField("Действия", EditorStyles.boldLabel);
            if (GUILayout.Button("1. Создать/Обновить группы"))
            {
                CreateAddressableGroups();
            }
        
            if (GUILayout.Button("2. Назначить адреса ассетам"))
            {
                AssignAddressesToAssets();
            }
        
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Выполнить всё (1 и 2)"))
            {
                if (_createGroupsIfMissing) CreateAddressableGroups();
                AssignAddressesToAssets();
            }
            EditorGUILayout.Space(10);
        
            if (GUILayout.Button("Проверить настройки Addressables"))
            {
                ValidateAddressableSettings();
            }
            #endregion

            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region Core Logic
        private bool IsAddressablesInitialized()
        {
            return AddressableAssetSettingsDefaultObject.Settings != null;
        }

        private void InitializeAddressableAssetSettings()
        {
            string settingsFolderPath = AddressableAssetSettingsDefaultObject.kDefaultConfigFolder; 
            string settingsFileName = "AddressableAssetSettings.asset"; 

            if (!Directory.Exists(settingsFolderPath))
            {
                Directory.CreateDirectory(settingsFolderPath);
            }

            string fullSettingsPath = Path.Combine(settingsFolderPath, settingsFileName);
            AddressableAssetSettings settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(fullSettingsPath);

            if (settings == null)
            {
                settings = AddressableAssetSettings.Create(
                    settingsFolderPath, 
                    settingsFileName,   
                    true,               
                    true                
                );
            }
            else
            {
                if (AddressableAssetSettingsDefaultObject.Settings != settings)
                {
                    AddressableAssetSettingsDefaultObject.Settings = settings;
                }
            }
        
            if (settings.DefaultGroup == null)
            {
                Debug.LogWarning("Группа по умолчанию не была автоматически создана или найдена. Создаю 'Default Local Group'.");
                AddressableAssetGroup defaultGroup = settings.CreateGroup(
                    "Default Local Group", 
                    false, 
                    false, 
                    true,  
                    null,  
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema)); 
                settings.DefaultGroup = defaultGroup; 
            }

            EditorUtility.SetDirty(settings); 
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Система Addressables успешно инициализирована или существующие настройки загружены.");
        }

        private void CreateAddressableGroups()
        {
            if (!IsAddressablesInitialized())
            {
                Debug.LogError("Addressable Asset Settings не найдены! Сначала инициализируйте систему.");
                return;
            }
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            foreach (var groupType in _groupTypes)
            {
                var existingGroup = settings.FindGroup(groupType);
                if (existingGroup != null)
                {
                    Debug.Log($"Группа '{groupType}' уже существует. Пропускаю создание.");
                    ConfigureGroupSchemas(existingGroup, settings); 
                    continue;
                }
            
                if (!_createGroupsIfMissing) continue;

                var newGroup = settings.CreateGroup(groupType, false, false, false, null); 
                ConfigureGroupSchemas(newGroup, settings);
                Debug.Log($"Создана группа Addressables: '{groupType}'");
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupAdded, null, true);
            AssetDatabase.SaveAssets();
            Debug.Log("Создание/обновление групп Addressables завершено.");
        }
    
        private void ConfigureGroupSchemas(AddressableAssetGroup group, AddressableAssetSettings settings)
        {
            if (group == null) return;

            var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
            if (bundledSchema == null)
            {
                bundledSchema = group.AddSchema<BundledAssetGroupSchema>();
            }
        
            bundledSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            bundledSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether; 
            bundledSchema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4; 

            if (group.GetSchema<ContentUpdateGroupSchema>() == null)
            {
                group.AddSchema<ContentUpdateGroupSchema>().StaticContent = false; 
            }
        }

        private void AssignAddressesToAssets()
        {
            if (!IsAddressablesInitialized())
            {
                Debug.LogError("Addressable Asset Settings не найдены! Сначала инициализируйте систему.");
                return;
            }
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            int assetsProcessedCount = 0;
            int assetsAddedOrUpdatedCount = 0;

            foreach (var groupType in _groupTypes) // Итерируем по всем определенным типам групп
            {
                if (!_defaultFolderPaths.TryGetValue(groupType, out string folderPath) || string.IsNullOrEmpty(folderPath))
                {
                    Debug.LogWarning($"Путь для группы '{groupType}' не определен или пуст. Пропускаю.");
                    continue;
                }
            
                if (!Directory.Exists(folderPath))
                {
                    Debug.LogWarning($"Папка '{folderPath}' для группы '{groupType}' не существует. Пропускаю.");
                    continue;
                }

                var targetGroup = settings.FindGroup(groupType);
                if (targetGroup == null)
                {
                    if (_createGroupsIfMissing)
                    {
                        Debug.LogWarning($"Группа '{groupType}' не найдена. Попытка создать...");
                        // Не вызываем CreateAddressableGroups() здесь, чтобы избежать рекурсии или лишних вызовов.
                        // Группы должны быть созданы на шаге "1. Создать/Обновить группы".
                        // Если группа не создалась, значит что-то пошло не так на предыдущем шаге.
                        targetGroup = settings.CreateGroup(groupType, false, false, false, null);
                        ConfigureGroupSchemas(targetGroup, settings);
                        if (targetGroup == null) {
                            Debug.LogError($"Не удалось создать группу '{groupType}' во время назначения ассетов. Пропускаю ассеты из '{folderPath}'.");
                            continue;
                        }
                        Debug.Log($"Создана группа '{groupType}' во время назначения ассетов.");
                    }
                    else
                    {
                        Debug.LogWarning($"Группа '{groupType}' не найдена и опция создания групп отключена. Использую группу по умолчанию.");
                        targetGroup = settings.DefaultGroup;
                        if (targetGroup == null)
                        {
                            Debug.LogError("Группа по умолчанию не найдена! Не могу обработать ассеты.");
                            return; 
                        }
                    }
                }
            
                string[] allAssetGuids = AssetDatabase.FindAssets("", new[] { folderPath });

                foreach (string guid in allAssetGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                    if (AssetDatabase.IsValidFolder(assetPath) || assetPath.EndsWith(".cs") || assetPath.EndsWith(".meta")) 
                        continue;

                    assetsProcessedCount++;

                    var entry = settings.FindAssetEntry(guid);
                    if (entry != null && !_overwriteExistingEntries)
                    {
                        continue; 
                    }
                
                    if (entry == null || entry.parentGroup != targetGroup || _overwriteExistingEntries)
                    {
                        entry = settings.CreateOrMoveEntry(guid, targetGroup, false, false); 
                    }
                
                    if (entry != null)
                    {
                        string assetAddress = GenerateAddressForAsset(assetPath);
                        entry.address = assetAddress;
                    
                        if (_assignLabels)
                        {
                            entry.SetLabel(groupType, true, true); 
                        
                            string extension = Path.GetExtension(assetPath).ToLower();
                            switch(extension)
                            {
                                case ".prefab": entry.SetLabel("Prefab", true, true); break;
                                case ".unity": entry.SetLabel("Scene", true, true); break; 
                                case ".png": case ".jpg": case ".jpeg": case ".tga": entry.SetLabel("Texture", true, true); break;
                                case ".mp3": case ".wav": case ".ogg": entry.SetLabel("Audio", true, true); break;
                                case ".mat": entry.SetLabel("Material", true, true); break;
                                case ".asset": 
                                    entry.SetLabel("ScriptableObject", true, true); 
                                    break;
                            }
                        }
                        assetsAddedOrUpdatedCount++;
                    }
                }
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true); 
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(settings); 
        
            Debug.Log($"Обработка ассетов завершена: Просмотрено {assetsProcessedCount} ассетов. Добавлено/Обновлено {assetsAddedOrUpdatedCount} записей Addressables.");
        }

        private string GenerateAddressForAsset(string assetPath) 
        {
            string address;

            if (_includePathInAddress)
            {
                address = assetPath;
                if (_stripAssetsFromPath && address.StartsWith("Assets/"))
                {
                    address = address.Substring("Assets/".Length);
                }
                string extension = Path.GetExtension(address);
                if (!string.IsNullOrEmpty(extension))
                {
                    address = address.Substring(0, address.Length - extension.Length);
                }
            }
            else
            {
                address = Path.GetFileNameWithoutExtension(assetPath);
            }
        
            if (_useCustomNaming && !string.IsNullOrEmpty(_addressNamingPrefix))
            {
                address = $"{_addressNamingPrefix.Trim('/')}/{address.TrimStart('/')}";
            }
        
            return address.Replace('\\', '/'); 
        }

        private void ValidateAddressableSettings()
        {
            if (!IsAddressablesInitialized())
            {
                EditorUtility.DisplayDialog("Ошибка", "Система Addressables не инициализирована.", "OK");
                return;
            }
            var settings = AddressableAssetSettingsDefaultObject.Settings;
        
            List<string> issues = new List<string>();
            List<string> warnings = new List<string>();

            Dictionary<string, List<AddressableAssetEntry>> addressMap = new Dictionary<string, List<AddressableAssetEntry>>();
            foreach (var group in settings.groups.Where(g => g != null))
            {
                foreach (var entry in group.entries)
                {
                    if (string.IsNullOrEmpty(entry.address))
                    {
                        warnings.Add($"У ассета '{entry.AssetPath}' пустой адрес в группе '{group.Name}'.");
                        continue;
                    }
                    if (!addressMap.ContainsKey(entry.address))
                    {
                        addressMap[entry.address] = new List<AddressableAssetEntry>();
                    }
                    addressMap[entry.address].Add(entry);
                }
            }
            foreach (var kvp in addressMap.Where(kvp => kvp.Value.Count > 1))
            {
                issues.Add($"Дублирующийся адрес: '{kvp.Key}'. Используется {kvp.Value.Count} раз(а) ассетами: {string.Join(", ", kvp.Value.Select(e => Path.GetFileName(e.AssetPath)))}");
            }

            string[] scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
            foreach (var filePath in scriptFiles)
            {
                try 
                {
                    string content = File.ReadAllText(filePath);
                    if (content.Contains(".WaitForCompletion()")) 
                    {
                        warnings.Add($"Файл '{filePath.Replace(Application.dataPath, "Assets")}' содержит вызов '.WaitForCompletion()'. Это может привести к зависанию основного потока во время выполнения.");
                    }
                } catch (IOException ex) { Debug.LogWarning($"Не удалось прочитать файл {filePath}: {ex.Message}"); }
            }
        
            foreach (var group in settings.groups.Where(g => g != null))
            {
                var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
                if (bundledSchema != null && bundledSchema.BundleMode == BundledAssetGroupSchema.BundlePackingMode.PackTogether)
                {
                    if (group.entries.Count > 50) 
                    {
                        warnings.Add($"Группа '{group.Name}' содержит {group.entries.Count} ассетов и использует режим 'Pack Together'. Это может привести к созданию очень больших бандлов и увеличению времени загрузки/объема памяти.");
                    }
                }
            }

            bool foundInitializationCall = scriptFiles.Any(filePath => {
                try { return File.ReadAllText(filePath).Contains("Addressables.InitializeAsync()"); }
                catch { return false; }
            });
            if (!foundInitializationCall)
            {
                warnings.Add("Не найден явный вызов 'Addressables.InitializeAsync()' в скриптах проекта. " +
                             "Убедитесь, что вы инициализируете систему Addressables перед первой загрузкой ассетов, особенно в билдах.");
            }

            System.Text.StringBuilder resultMessage = new System.Text.StringBuilder();
            if (issues.Count == 0 && warnings.Count == 0)
            {
                resultMessage.Append("Проверка завершена. Серьезных проблем не обнаружено!");
            }
            else
            {
                resultMessage.AppendLine($"Обнаружено проблем: {issues.Count}");
                foreach(var issue in issues) { resultMessage.AppendLine($"- ОШИБКА: {issue}"); Debug.LogError(issue); }
            
                resultMessage.AppendLine($"\nОбнаружено предупреждений: {warnings.Count}");
                foreach(var warning in warnings) { resultMessage.AppendLine($"- ПРЕДУПРЕЖДЕНИЕ: {warning}"); Debug.LogWarning(warning); }
            }

            EditorUtility.DisplayDialog("Результаты проверки Addressables", resultMessage.ToString(), "OK");
            Debug.Log("Проверка настроек Addressables завершена. Подробности выше и в консоли.");
        }
        #endregion
    }
}