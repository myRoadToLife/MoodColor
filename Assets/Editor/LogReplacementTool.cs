using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    public class LogReplacementTool : EditorWindow
    {
        private bool _replaceDebugLog = true;
        private bool _replaceDebugLogWarning = true;
        private bool _replaceDebugLogError = true;
        private string _targetFolder = "Assets/App/Develop";
        
        [MenuItem("Tools/Log Replacement Tool")]
        public static void ShowWindow()
        {
            GetWindow<LogReplacementTool>("Log Replacement Tool");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Замена Debug.Log на MyLogger", EditorStyles.boldLabel);
            
            _replaceDebugLog = EditorGUILayout.Toggle("Заменить Debug.Log", _replaceDebugLog);
            _replaceDebugLogWarning = EditorGUILayout.Toggle("Заменить Debug.LogWarning", _replaceDebugLogWarning);
            _replaceDebugLogError = EditorGUILayout.Toggle("Заменить Debug.LogError", _replaceDebugLogError);
            
            _targetFolder = EditorGUILayout.TextField("Папка для обработки:", _targetFolder);
            
            if (GUILayout.Button("Заменить логи"))
            {
                ReplaceLogsInFolder();
            }
            
            if (GUILayout.Button("Предварительный просмотр"))
            {
                PreviewChanges();
            }
        }
        
        private void ReplaceLogsInFolder()
        {
            if (!Directory.Exists(_targetFolder))
            {
                Debug.LogError($"Папка не существует: {_targetFolder}");
                return;
            }
            
            string[] files = Directory.GetFiles(_targetFolder, "*.cs", SearchOption.AllDirectories);
            int processedFiles = 0;
            int totalReplacements = 0;
            
            foreach (string file in files)
            {
                // Пропускаем файлы тестов и уже обработанные файлы
                if (file.Contains("Test") || file.Contains("MyLogger.cs") || file.Contains("LoggerSettings.cs"))
                    continue;
                
                string content = File.ReadAllText(file);
                string originalContent = content;
                int fileReplacements = 0;
                
                // Проверяем, есть ли уже using для MyLogger
                bool hasMyLoggerUsing = content.Contains("using App.Develop.Utils.Logging;");
                
                if (_replaceDebugLog)
                {
                    // Заменяем Debug.Log на MyLogger.Log с категорией по умолчанию
                    var matches = Regex.Matches(content, @"Debug\.Log\s*\(\s*([^)]+)\s*\)");
                    foreach (Match match in matches)
                    {
                        string replacement = DetermineLogReplacement(match.Groups[1].Value, file);
                        content = content.Replace(match.Value, replacement);
                        fileReplacements++;
                    }
                }
                
                if (_replaceDebugLogWarning)
                {
                    // Заменяем Debug.LogWarning на MyLogger.LogWarning
                    var matches = Regex.Matches(content, @"Debug\.LogWarning\s*\(\s*([^)]+)\s*\)");
                    foreach (Match match in matches)
                    {
                        string replacement = DetermineWarningReplacement(match.Groups[1].Value, file);
                        content = content.Replace(match.Value, replacement);
                        fileReplacements++;
                    }
                }
                
                if (_replaceDebugLogError)
                {
                    // Заменяем Debug.LogError на MyLogger.LogError
                    var matches = Regex.Matches(content, @"Debug\.LogError\s*\(\s*([^)]+)\s*\)");
                    foreach (Match match in matches)
                    {
                        string replacement = DetermineErrorReplacement(match.Groups[1].Value, file);
                        content = content.Replace(match.Value, replacement);
                        fileReplacements++;
                    }
                }
                
                // Добавляем using, если были замены и его еще нет
                if (fileReplacements > 0 && !hasMyLoggerUsing)
                {
                    content = AddMyLoggerUsing(content);
                }
                
                // Сохраняем файл, если были изменения
                if (content != originalContent)
                {
                    File.WriteAllText(file, content);
                    processedFiles++;
                    totalReplacements += fileReplacements;
                    Debug.Log($"Обработан файл: {file} ({fileReplacements} замен)");
                }
            }
            
            Debug.Log($"Обработка завершена. Файлов: {processedFiles}, замен: {totalReplacements}");
            AssetDatabase.Refresh();
        }
        
        private string DetermineLogReplacement(string logContent, string filePath)
        {
            string category = DetermineCategory(filePath);
            return $"MyLogger.Log({logContent}, MyLogger.LogCategory.{category})";
        }
        
        private string DetermineWarningReplacement(string logContent, string filePath)
        {
            string category = DetermineCategory(filePath);
            return $"MyLogger.LogWarning({logContent}, MyLogger.LogCategory.{category})";
        }
        
        private string DetermineErrorReplacement(string logContent, string filePath)
        {
            string category = DetermineCategory(filePath);
            return $"MyLogger.LogError({logContent}, MyLogger.LogCategory.{category})";
        }
        
        private string DetermineCategory(string filePath)
        {
            if (filePath.Contains("Firebase")) return "Firebase";
            if (filePath.Contains("Emotion")) return "Emotion";
            if (filePath.Contains("Network")) return "Network";
            if (filePath.Contains("UI")) return "UI";
            if (filePath.Contains("EntryPoint") || filePath.Contains("Bootstrap")) return "Bootstrap";
            if (filePath.Contains("Gameplay")) return "Gameplay";
            if (filePath.Contains("Editor")) return "Editor";
            if (filePath.Contains("Sync")) return "Sync";
            
            return "Default";
        }
        
        private string AddMyLoggerUsing(string content)
        {
            // Ищем последний using и добавляем после него
            var usingMatches = Regex.Matches(content, @"using\s+[^;]+;");
            if (usingMatches.Count > 0)
            {
                var lastUsing = usingMatches[usingMatches.Count - 1];
                int insertIndex = lastUsing.Index + lastUsing.Length;
                content = content.Insert(insertIndex, "\nusing App.Develop.Utils.Logging;");
            }
            else
            {
                // Если нет using'ов, добавляем в начало файла
                content = "using App.Develop.Utils.Logging;\n" + content;
            }
            
            return content;
        }
        
        private void PreviewChanges()
        {
            Debug.Log("Предварительный просмотр изменений:");
            // Здесь можно добавить логику для показа изменений без их применения
        }
    }
} 