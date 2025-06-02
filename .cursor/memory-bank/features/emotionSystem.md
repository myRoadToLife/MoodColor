# Система Эмоций MoodColor

## Обзор
Система эмоций является ядром приложения MoodColor, предоставляя пользователям интуитивный способ отслеживания, анализа и управления своими эмоциональными состояниями через цветовую визуализацию и игровые механики.

## Архитектура Системы

### Основные Компоненты
- **EmotionService**: Центральный сервис управления эмоциями
- **EmotionConfigService**: Управление конфигурациями эмоций через Addressables
- **EmotionSyncService**: Синхронизация с Firebase
- **EmotionHistoryCache**: Кэширование истории для производительности
- **EmotionMixingRules**: Правила смешивания эмоций

### Типы Эмоций (11 типов)
```csharp
public enum EmotionTypes
{
    Joy,           // Радость - Желтый
    Sadness,       // Грусть - Синий
    Anger,         // Гнев - Красный
    Fear,          // Страх - Фиолетовый
    Disgust,       // Отвращение - Зеленый
    Trust,         // Доверие - Голубой
    Anticipation,  // Предвкушение - Оранжевый
    Surprise,      // Удивление - Лавандовый
    Love,          // Любовь - Розовый
    Anxiety,       // Тревога - Серый
    Neutral        // Нейтральное - Белый
}
```

## Модель Данных Эмоции

### EmotionData
```csharp
public class EmotionData
{
    public string Id { get; set; }
    public string Type { get; set; }
    public float Value { get; set; }           // Текущее значение (0-100)
    public float Intensity { get; set; }       // Интенсивность (0-1)
    public Color Color { get; set; }           // Цвет эмоции
    public float MaxCapacity { get; set; }     // Максимальная вместимость
    public float DrainRate { get; set; }       // Скорость убывания
    public float BubbleThreshold { get; set; } // Порог для "пузырей"
    public DateTime LastUpdated { get; set; }  // Время последнего обновления
    public string Note { get; set; }           // Заметка пользователя
}
```

### EmotionHistoryRecord
```csharp
public class EmotionHistoryRecord
{
    public string Id { get; set; }
    public string EmotionType { get; set; }
    public EmotionEventType EventType { get; set; }
    public float Value { get; set; }
    public float Intensity { get; set; }
    public DateTime Timestamp { get; set; }
    public string Note { get; set; }
    public bool IsSynced { get; set; }
}
```

## События Эмоций

### EmotionEventType
```csharp
public enum EmotionEventType
{
    ValueChanged,      // Изменение значения
    IntensityChanged,  // Изменение интенсивности
    CapacityExceeded,  // Превышение вместимости
    BubbleCreated,     // Создание "пузыря"
    EmotionMixed,      // Смешивание эмоций
    EmotionDepleted,   // Истощение эмоции
    JarClicked         // Клик по банке эмоций
}
```

## Система Смешивания Эмоций

### EmotionMixingRules
Позволяет комбинировать различные эмоции для создания новых состояний:

```csharp
public class EmotionMixResult
{
    public EmotionTypes ResultType { get; set; }
    public Color ResultColor { get; set; }
    public float ResultIntensity { get; set; }
}
```

### Примеры Смешивания
- **Joy + Trust = Love** (Радость + Доверие = Любовь)
- **Fear + Surprise = Anxiety** (Страх + Удивление = Тревога)
- **Anger + Disgust = Contempt** (Гнев + Отвращение = Презрение)

## Аналитика и Статистика

### Статистика по Времени Суток
```csharp
public class EmotionTimeStats
{
    public TimeOfDay TimeOfDay { get; set; }
    public Dictionary<EmotionTypes, float> AverageValues { get; set; }
    public int TotalEntries { get; set; }
}

public enum TimeOfDay
{
    Morning,    // 6:00 - 12:00
    Afternoon,  // 12:00 - 18:00
    Evening,    // 18:00 - 22:00
    Night       // 22:00 - 6:00
}
```

### Частота Логирования
```csharp
public class EmotionFrequencyStats
{
    public DateTime Date { get; set; }
    public int EntryCount { get; set; }
    public Dictionary<EmotionTypes, int> EmotionCounts { get; set; }
}
```

## Конфигурация Эмоций

### EmotionConfig
```csharp
public class EmotionConfig
{
    public EmotionTypes Type { get; set; }
    public Color BaseColor { get; set; }
    public float MaxCapacity { get; set; }
    public float DefaultDrainRate { get; set; }
    public float BubbleThreshold { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
}
```

### Загрузка через Addressables
Конфигурации эмоций загружаются динамически через Unity Addressables:
- Гибкость в настройке параметров
- Возможность обновления без пересборки
- Локализация названий и описаний

## Синхронизация с Firebase

### Workflow Синхронизации
1. **Локальное Сохранение**: Данные сначала сохраняются в кэш
2. **Батчинг**: Несколько операций объединяются в пакеты
3. **Отправка в Firebase**: При наличии сети данные синхронизируются
4. **Обработка Конфликтов**: Разрешение конфликтов при одновременном редактировании
5. **Офлайн Режим**: Работа без интернета с последующей синхронизацией

### Оптимизации
- **Уникальные ID**: Предотвращение дублирования записей
- **Кэширование**: EmotionHistoryCache для быстрого доступа
- **Пакетные Операции**: Снижение нагрузки на Firebase
- **Инкрементальная Синхронизация**: Только измененные данные

## Игровые Механики

### Интеграция с Системой Очков
```csharp
public enum PointsSource
{
    EmotionTracking,    // +5 очков за отметку эмоции
    JarInteraction,     // +3 очка за взаимодействие с банкой
    EmotionMixed,       // +10 очков за смешивание эмоций
    DailyStreak,        // Бонус за ежедневное использование
    SocialActivity      // Очки за социальные взаимодействия
}
```

### Система Опыта
```csharp
public enum XPSource
{
    EmotionMarked,      // +5 XP за отметку эмоции
    EmotionMixed,       // +15 XP за смешивание
    Achievement,        // Переменный XP за достижения
    SocialInteraction   // +10 XP за социальные действия
}
```

## API Методы

### Основные Операции
```csharp
// Получение данных эмоции
EmotionData GetEmotionData(EmotionTypes type);
float GetEmotionValue(EmotionTypes type);
Color GetEmotionColor(EmotionTypes type);

// Обновление эмоций
void SetEmotionValue(EmotionTypes type, float value);
void SetEmotionIntensity(EmotionTypes type, float intensity);
void UpdateEmotionValue(EmotionTypes type, float value);

// Смешивание эмоций
bool TryMixEmotions(EmotionTypes source1, EmotionTypes source2);

// История и статистика
IEnumerable<EmotionHistoryEntry> GetEmotionHistory(DateTime? from, DateTime? to);
Dictionary<TimeOfDay, EmotionTimeStats> GetEmotionsByTimeOfDay();
List<EmotionFrequencyStats> GetLoggingFrequency(DateTime from, DateTime to);
```

## Производительность

### Оптимизации
- **Кэширование**: Многоуровневое кэширование для быстрого доступа
- **Lazy Loading**: Отложенная загрузка исторических данных
- **Object Pooling**: Переиспользование объектов для UI
- **Batch Operations**: Пакетная обработка операций Firebase

### Метрики
- **Время Отклика**: <100ms для основных операций
- **Использование Памяти**: <50MB для истории до 1000 записей
- **Синхронизация**: <5 секунд для пакета из 100 записей

## Будущие Улучшения
- **ИИ Рекомендации**: Персональные советы на основе паттернов
- **Расширенная Аналитика**: Корреляции с внешними факторами
- **Новые Типы Эмоций**: Добавление культурно-специфичных эмоций
- **Голосовой Ввод**: Распознавание эмоций по голосу
- **Интеграция с Носимыми**: Автоматическое отслеживание через фитнес-трекеры 