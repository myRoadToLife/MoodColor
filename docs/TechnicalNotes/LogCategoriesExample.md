# Примеры группировки логов по категориям

## Sync (Синхронизация)
```csharp
MyLogger.Log("[HistoryPanelController] 🔄 Начало загрузки истории с синхронизацией", MyLogger.LogCategory.Sync);
MyLogger.Log("[HistoryPanelController] ☁️ Начинаем полную синхронизацию с Firebase", MyLogger.LogCategory.Sync);
MyLogger.LogError("[HistoryPanelController] ❌ Ошибка при синхронизации с Firebase", MyLogger.LogCategory.Sync);
MyLogger.Log("[HistoryPanelController] ✅ Синхронизация с Firebase завершена успешно", MyLogger.LogCategory.Sync);
MyLogger.LogWarning("[HistoryPanelController] ⚠️ Синхронизация недоступна", MyLogger.LogCategory.Sync);
```

## Firebase
```csharp
MyLogger.Log($"[HistoryPanelController] 🔗 Состояние Firebase: инициализирован={_emotionService?.IsFirebaseInitialized}", MyLogger.LogCategory.Firebase);
MyLogger.Log("[PersonalAreaManager] Загружаем профиль пользователя из Firebase...", MyLogger.LogCategory.Firebase);
MyLogger.LogWarning("[PersonalAreaManager] Пользователь не авторизован", MyLogger.LogCategory.Firebase);
```

## UI (Интерфейс)
```csharp
MyLogger.Log("🔄 [PersonalAreaUIController] Валидация компонентов...", MyLogger.LogCategory.UI);
MyLogger.Log("🔄 [PersonalAreaUIController] Начало инициализации...", MyLogger.LogCategory.UI);
MyLogger.Log("[HistoryPanelController] 🎨 Отображаем историю в UI...", MyLogger.LogCategory.UI);
MyLogger.Log("[HistoryPanelController] DisplayHistory вызван", MyLogger.LogCategory.UI);
MyLogger.LogError("❌ [PersonalAreaUIController] ProfileInfoComponent не назначен", MyLogger.LogCategory.UI);
```

## Bootstrap (Загрузка сцен)
```csharp
MyLogger.Log("✅ [PersonalAreaBootstrap] Сцена загружена", MyLogger.LogCategory.Bootstrap);
MyLogger.Log("🔄 [PersonalAreaBootstrap] Получение IAssetLoader...", MyLogger.LogCategory.Bootstrap);
MyLogger.Log("✅ [PersonalAreaBootstrap] IAssetLoader получен", MyLogger.LogCategory.Bootstrap);
MyLogger.LogError("❌ DIContainer не может быть null", MyLogger.LogCategory.Bootstrap);
```

## Gameplay (Игровая логика)
```csharp
MyLogger.LogWarning($"Неизвестный тип эмоции: {type}", MyLogger.LogCategory.Gameplay);
MyLogger.LogWarning($"Попытка добавить неположительное количество эмоций: {amount}", MyLogger.LogCategory.Gameplay);
MyLogger.Log($"🔄 [PersonalAreaUIController] Установка количества {amount} для банки типа {type}", MyLogger.LogCategory.Gameplay);
MyLogger.Log($"🔄 [PersonalAreaUIController] Установка очков: {points}", MyLogger.LogCategory.Gameplay);
```

## Network (Сеть)
```csharp
MyLogger.LogWarning($"[PersonalAreaManager] Не удалось получить IDatabaseService: {ex.Message}", MyLogger.LogCategory.Network);
MyLogger.LogError($"[PersonalAreaManager] Ошибка при загрузке профиля пользователя: {ex.Message}", MyLogger.LogCategory.Network);
```

## Editor (Редактор)
```csharp
MyLogger.EditorLog($"[WorkshopPanelGenerator] Префаб {panelName} создан");
MyLogger.EditorLogWarning($"[SettingsPanelGenerator] Текстура WoodenPlank.png не найдена");
MyLogger.EditorLogError($"Field {fieldName} not found in SettingsPanelController");
```

## Default (По умолчанию)
```csharp
MyLogger.Log("Общие логи без специфической категории");
MyLogger.LogWarning("Предупреждения общего характера");
```

## Управление через LoggerSettings

В инспекторе Unity на объекте с компонентом LoggerSettings можно:

1. **Общие настройки:**
   - Включить/отключить Debug логи
   - Включить/отключить Warning логи  
   - Включить/отключить Error логи

2. **По категориям:**
   - Default Category - общие логи
   - Sync Category - синхронизация
   - UI Category - интерфейс
   - Network Category - сеть
   - Firebase Category - Firebase
   - Editor Category - редактор
   - Gameplay Category - игровая логика
   - Bootstrap Category - загрузка сцен

Это позволяет гибко управлять логированием в зависимости от того, что нужно отладить. 