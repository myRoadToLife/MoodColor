# Система Сбора Эмоциональных Данных MoodColor

## 📋 Обзор

Система сбора эмоциональных данных отвечает за регистрацию, анализ и агрегацию информации о эмоциональных состояниях пользователей в приложении MoodColor. Система работает на двух уровнях: локальном (персональные данные) и глобальном (региональная статистика).

## 🏗️ Текущая Архитектура

### Основные Компоненты

#### 1. EmotionService
**Назначение:** Центральный сервис управления эмоциями  
**Файл:** `Assets/App/Develop/CommonServices/Emotion/EmotionService.cs`  
**Функции:**
- Хранение и управление эмоциями пользователя
- Логирование событий через `LogEmotionEvent()`
- Синхронизация с Firebase
- Управление историей эмоций

#### 2. EmotionSyncService
**Назначение:** Синхронизация данных с облаком  
**Файл:** `Assets/App/Develop/CommonServices/Firebase/Database/Services/EmotionSyncService.cs`  
**Функции:**
- Батчинг операций для оптимизации
- Офлайн режим с последующей синхронизацией
- Разрешение конфликтов данных

#### 3. RegionalStatsService
**Назначение:** Управление региональной статистикой  
**Файл:** `Assets/App/Develop/CommonServices/Regional/RegionalStatsService.cs`  
**Функции:**
- Агрегация данных по регионам
- Кэширование статистики
- Интерфейс для получения глобальных данных

#### 4. EmotionHistoryCache
**Назначение:** Кэширование данных для производительности  
**Функции:**
- Быстрый доступ к истории эмоций
- Управление несинхронизированными записями

## 📊 Модели Данных

### EmotionData
```csharp
public class EmotionData
{
    public string Id { get; set; }              // Уникальный идентификатор
    public string Type { get; set; }            // Тип эмоции (Joy, Sadness, etc.)
    public float Value { get; set; }            // Значение (0-100)
    public float Intensity { get; set; }        // Интенсивность (0-1)
    public Color Color { get; set; }            // Цвет эмоции
    public string Note { get; set; }            // Заметка пользователя
    public string RegionId { get; set; }        // ID региона (НЕ ИСПОЛЬЗУЕТСЯ)
    public DateTime LastUpdated { get; set; }   // Время обновления
    public double? Latitude { get; set; }       // Широта (НЕ ИСПОЛЬЗУЕТСЯ)
    public double? Longitude { get; set; }      // Долгота (НЕ ИСПОЛЬЗУЕТСЯ)
}
```

### EmotionHistoryRecord
```csharp
public class EmotionHistoryRecord
{
    public string Id { get; set; }
    public string Type { get; set; }
    public float Value { get; set; }
    public float Intensity { get; set; }
    public string EventType { get; set; }
    public long Timestamp { get; set; }
    public string RegionId { get; set; }        // НЕ ЗАПОЛНЯЕТСЯ
    public double? Latitude { get; set; }       // НЕ ЗАПОЛНЯЕТСЯ
    public double? Longitude { get; set; }      // НЕ ЗАПОЛНЯЕТСЯ
    public SyncStatus SyncStatus { get; set; }
}
```

### RegionalEmotionStats
```csharp
public class RegionalEmotionStats
{
    public EmotionTypes DominantEmotion { get; set; }
    public Dictionary<EmotionTypes, int> EmotionCounts { get; set; }
    public int TotalEmotions { get; set; }
    public float DominantEmotionPercentage { get; set; }
}
```

## 🔄 Процесс Сбора Данных

### Текущий Workflow

1. **Взаимодействие пользователя**
   ```csharp
   // JarInteractionHandler.cs:68
   _emotionService.LogEmotionEvent(emotionType, EmotionEventType.JarClicked, "Jar Clicked");
   ```

2. **Локальное сохранение**
   ```csharp
   // EmotionService.cs:1290+
   var entry = new EmotionHistoryEntry
   {
       EmotionData = emotion.Clone(),
       Timestamp = DateTime.UtcNow,
       EventType = eventType,
       SyncId = uniqueId,
       IsSynced = false
   };
   _emotionHistory.AddEntryDirect(entry);
   ```

3. **Синхронизация с Firebase**
   ```csharp
   if (_isFirebaseInitialized && _databaseService?.IsAuthenticated == true)
   {
       SyncEmotionWithFirebaseById(emotion, eventType, uniqueId);
   }
   ```

## ❌ Выявленные Проблемы

### 1. Отсутствие Геолокации
**Проблема:** Нет сервиса для определения местоположения  
**Влияние:** 
- RegionId остается пустым
- Нет привязки к реальным регионам
- Невозможна корректная региональная аналитика

### 2. Разрыв Между Локальными и Глобальными Данными
**Проблема:** LogEmotionEvent не обновляет региональную статистику  
**Влияние:**
- Глобальная статистика показывает только мок-данные
- Нет реальной агрегации пользовательских данных
- Региональная аналитика неактуальна

### 3. Отсутствие Настроек Конфиденциальности
**Проблема:** Нет контроля пользователя над сбором данных  
**Влияние:**
- Потенциальные проблемы с GDPR
- Нет возможности отключить глобальный сбор
- Отсутствие анонимизации данных

### 4. Неэффективная Архитектура
**Проблема:** Компоненты слабо связаны между собой  
**Влияние:**
- Дублирование логики
- Сложность сопровождения
- Отсутствие единой точки управления

## 🚀 План Улучшений

### Этап 1: Добавление Геолокации

#### 1.1 Создать ILocationService
```csharp
public interface ILocationService
{
    Task<LocationData> GetCurrentLocationAsync();
    Task<string> GetRegionIdAsync(double latitude, double longitude);
    bool IsLocationPermissionGranted { get; }
    event Action<LocationData> OnLocationChanged;
}
```

#### 1.2 Реализовать LocationService
- Запрос разрешений на геолокацию
- Получение координат через Unity Location Services
- Определение региона по координатам
- Кэширование результатов

### Этап 2: Интеграция Региональной Статистики

#### 2.1 Модифицировать LogEmotionEvent
```csharp
public async void LogEmotionEvent(EmotionTypes type, EmotionEventType eventType, string note = null)
{
    // ... существующий код ...
    
    // Добавить обновление региональной статистики
    if (!string.IsNullOrEmpty(emotion.RegionId))
    {
        await UpdateRegionalStats(emotion);
    }
}
```

#### 2.2 Создать DataCollectionOrchestrator
```csharp
public class DataCollectionOrchestrator
{
    private readonly IEmotionService _emotionService;
    private readonly IRegionalStatsService _regionalStatsService;
    private readonly ILocationService _locationService;
    private readonly IPrivacyService _privacyService;
    
    public async Task ProcessEmotionEvent(EmotionEventData eventData)
    {
        // Централизованная обработка событий
        // Проверка настроек конфиденциальности
        // Обновление локальных и глобальных данных
    }
}
```

### Этап 3: Настройки Конфиденциальности

#### 3.1 Создать IPrivacyService
```csharp
public interface IPrivacyService
{
    bool AllowGlobalDataSharing { get; set; }
    bool AllowLocationTracking { get; set; }
    bool AnonymizeData { get; set; }
    Task<bool> RequestDataCollectionConsent();
    void RevokeConsent();
}
```

#### 3.2 Добавить UI настроек конфиденциальности
- Панель настроек в личном кабинете
- Запрос согласия при первом запуске
- Возможность отзыва согласия

### Этап 4: Оптимизация Архитектуры

#### 4.1 Создать Centralized Data Collection
```csharp
public class EmotionDataCollectionService : IEmotionDataCollectionService
{
    public async Task CollectEmotionData(EmotionCollectionRequest request)
    {
        // Валидация запроса
        // Проверка настроек конфиденциальности
        // Определение местоположения (если разрешено)
        // Сохранение локально
        // Обновление региональной статистики
        // Синхронизация с облаком
    }
}
```

## 📋 Техническое Задание для Реализации

### Приоритет 1: Критично
1. **LocationService** - Определение региона пользователя
2. **DataCollectionOrchestrator** - Централизованная обработка
3. **Интеграция региональной статистики** - Связь личных и глобальных данных

### Приоритет 2: Важно
1. **PrivacyService** - Настройки конфиденциальности
2. **UI для настроек** - Пользовательский контроль
3. **Анонимизация данных** - GDPR compliance

### Приоритет 3: Желательно
1. **Аналитические инструменты** - Расширенная статистика
2. **Экспорт данных** - Пользовательский доступ к данным
3. **Машинное обучение** - Предиктивная аналитика

## 🔍 Метрики Успеха

### Технические
- ✅ 100% записей с корректным RegionId
- ✅ Реальная региональная статистика вместо мок-данных
- ✅ Соответствие GDPR требованиям
- ✅ Производительность: < 100ms на обработку события

### Пользовательские
- ✅ Понятные настройки конфиденциальности
- ✅ Полезная региональная аналитика
- ✅ Прозрачность сбора данных

## 📅 Временная Оценка

- **Этап 1 (Геолокация):** 3-5 дней
- **Этап 2 (Интеграция статистики):** 2-3 дня  
- **Этап 3 (Конфиденциальность):** 4-6 дней
- **Этап 4 (Оптимизация):** 2-3 дня

**Общая оценка:** 11-17 рабочих дней

## 🛠️ Следующие Шаги

1. Создать ILocationService и базовую реализацию
2. Интегрировать геолокацию в EmotionService
3. Связать локальные данные с региональной статистикой
4. Добавить настройки конфиденциальности
5. Тестирование и оптимизация

---

*Документ создан: {DateTime.Now:yyyy-MM-dd}*  
*Статус: В разработке*  
*Ответственный: AI Assistant* 