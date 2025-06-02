# Игровая Система MoodColor

## Обзор
Игровая система MoodColor использует принципы геймификации для мотивации пользователей к регулярному отслеживанию эмоций и взаимодействию с приложением. Система включает очки, уровни, достижения и социальные элементы.

## Архитектура Игровой Системы

### Основные Компоненты
- **PointsService**: Управление очками пользователя
- **LevelSystem**: Система уровней и опыта
- **AchievementService**: Система достижений
- **SocialService**: Социальные взаимодействия и соревнования

## Система Очков

### PointsService
Центральный сервис для управления очками пользователя с различными источниками начисления.

### Источники Очков
```csharp
public enum PointsSource
{
    EmotionTracking,    // +5 очков за отметку эмоции
    JarInteraction,     // +3 очка за взаимодействие с банкой эмоций
    Achievement,        // Переменное количество за достижения
    DailyBonus,         // +20 очков за ежедневный вход
    SocialActivity,     // +10 очков за социальные взаимодействия
    EmotionMixed,       // +10 очков за смешивание эмоций
    WeeklyStreak,       // Бонус за недельную активность
    MonthlyGoal         // Бонус за достижение месячных целей
}
```

### Механика Начисления
- **Базовые Очки**: Фиксированное количество за основные действия
- **Множители**: Увеличение очков в зависимости от уровня пользователя
- **Стрики**: Дополнительные очки за последовательные дни использования
- **Бонусы**: Специальные события с увеличенными наградами

### API Методов
```csharp
// Основные операции с очками
void AddPoints(int amount, PointsSource source);
void AddPointsForEmotion();
void AddPointsForAchievement(int achievementPoints);
int GetCurrentPoints();
int GetTotalEarnedPoints();

// Статистика очков
Dictionary<PointsSource, int> GetPointsBySource();
List<PointsHistoryEntry> GetPointsHistory(DateTime from, DateTime to);
```

## Система Уровней

### LevelSystem
Система прогрессии пользователя через накопление опыта (XP) и повышение уровней.

### Источники Опыта
```csharp
public enum XPSource
{
    EmotionMarked,      // +5 XP за отметку эмоции
    EmotionMixed,       // +15 XP за смешивание эмоций
    Achievement,        // Переменный XP за достижения
    SocialInteraction,  // +10 XP за социальные действия
    DailyGoal,          // +25 XP за выполнение дневной цели
    WeeklyChallenge,    // +100 XP за недельные вызовы
    FirstTimeAction     // Бонус XP за первое выполнение действия
}
```

### Формула Прогрессии
```csharp
// Опыт, необходимый для следующего уровня
int GetXPForLevel(int level)
{
    return 100 + (level * 50); // Прогрессивное увеличение
}

// Текущий прогресс в процентах
float GetLevelProgress()
{
    int currentLevelXP = GetXPForLevel(CurrentLevel);
    int nextLevelXP = GetXPForLevel(CurrentLevel + 1);
    return (float)(CurrentXP - currentLevelXP) / (nextLevelXP - currentLevelXP);
}
```

### Награды за Уровни
- **Уровень 5**: Разблокировка смешивания эмоций
- **Уровень 10**: Доступ к расширенной статистике
- **Уровень 15**: Социальные функции
- **Уровень 20**: Персональные рекомендации
- **Уровень 25+**: Эксклюзивные цветовые схемы

### API Методов
```csharp
// Управление опытом
void AddXP(int amount, XPSource source);
int GetCurrentLevel();
int GetCurrentXP();
float GetLevelProgress();

// Информация об уровнях
int GetXPForNextLevel();
List<LevelReward> GetAvailableRewards();
bool IsFeatureUnlocked(GameFeature feature);
```

## Система Достижений

### AchievementService
Система наград за выполнение определенных действий и достижение целей.

### Типы Достижений
```csharp
public enum AchievementType
{
    // Основные достижения
    FirstEmotion,           // Первая отметка эмоции
    EmotionStreak,          // Серия дней с отметками
    EmotionMaster,          // Использование всех типов эмоций
    
    // Социальные достижения
    FirstFriend,            // Первый друг
    SocialButterfly,        // 10 друзей
    Influencer,             // 50 друзей
    
    // Прогрессивные достижения
    Beginner,               // 10 отметок эмоций
    Intermediate,           // 100 отметок эмоций
    Expert,                 // 1000 отметок эмоций
    Master,                 // 5000 отметок эмоций
    
    // Специальные достижения
    EarlyBird,              // Отметки утром
    NightOwl,               // Отметки поздно вечером
    Consistent,             // 30 дней подряд
    Analyzer,               // Просмотр статистики 50 раз
}
```

### Модель Достижения
```csharp
public class Achievement
{
    public string Id { get; set; }
    public AchievementType Type { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int PointsReward { get; set; }
    public int XPReward { get; set; }
    public AchievementRarity Rarity { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public bool IsUnlocked { get; set; }
    public float Progress { get; set; }
    public int RequiredCount { get; set; }
}

public enum AchievementRarity
{
    Common,     // Серый
    Uncommon,   // Зеленый
    Rare,       // Синий
    Epic,       // Фиолетовый
    Legendary   // Золотой
}
```

### API Методов
```csharp
// Управление достижениями
void CheckAchievements();
void UnlockAchievement(AchievementType type);
List<Achievement> GetUnlockedAchievements();
List<Achievement> GetAvailableAchievements();

// Прогресс достижений
float GetAchievementProgress(AchievementType type);
void UpdateAchievementProgress(AchievementType type, int increment);
```

## Социальная Система

### SocialService
Система социальных взаимодействий, включающая друзей, лидерборды и соревнования.

### Социальные Функции
- **Система Друзей**: Поиск и добавление друзей
- **Лидерборды**: Рейтинги по очкам и уровням
- **Соревнования**: Еженедельные и месячные вызовы
- **Обмен Достижениями**: Возможность делиться успехами

### Модель Друга
```csharp
public class Friend
{
    public string UserId { get; set; }
    public string DisplayName { get; set; }
    public int Level { get; set; }
    public int TotalPoints { get; set; }
    public DateTime LastActive { get; set; }
    public List<AchievementType> RecentAchievements { get; set; }
}
```

### Лидерборды
```csharp
public class LeaderboardEntry
{
    public string UserId { get; set; }
    public string DisplayName { get; set; }
    public int Rank { get; set; }
    public int Score { get; set; }
    public LeaderboardType Type { get; set; }
}

public enum LeaderboardType
{
    WeeklyPoints,       // Очки за неделю
    MonthlyPoints,      // Очки за месяц
    TotalLevel,         // Общий уровень
    EmotionStreak,      // Серия дней
    AchievementCount    // Количество достижений
}
```

## Игровые События

### Ежедневные Цели
```csharp
public class DailyGoal
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public GoalType Type { get; set; }
    public int RequiredCount { get; set; }
    public int CurrentProgress { get; set; }
    public int PointsReward { get; set; }
    public int XPReward { get; set; }
    public bool IsCompleted { get; set; }
}

public enum GoalType
{
    MarkEmotions,       // Отметить N эмоций
    MixEmotions,        // Смешать N эмоций
    SocialInteraction,  // Социальные действия
    ViewStatistics,     // Просмотр статистики
    AchieveStreak       // Поддержать серию дней
}
```

### Еженедельные Вызовы
- **Эмоциональный Исследователь**: Использовать все типы эмоций
- **Социальная Бабочка**: Взаимодействовать с 5 друзьями
- **Аналитик**: Просмотреть статистику 10 раз
- **Мастер Смешивания**: Создать 20 смешанных эмоций

## Система Наград

### Типы Наград
```csharp
public enum RewardType
{
    Points,             // Очки
    XP,                 // Опыт
    Achievement,        // Достижение
    ColorScheme,        // Новая цветовая схема
    EmotionType,        // Новый тип эмоции
    Feature,            // Разблокировка функции
    Cosmetic            // Косметические улучшения
}
```

### Система Стриков
```csharp
public class StreakSystem
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime LastActivity { get; set; }
    
    // Награды за стрики
    public Dictionary<int, StreakReward> StreakRewards { get; set; }
}

public class StreakReward
{
    public int StreakDay { get; set; }
    public int PointsBonus { get; set; }
    public int XPBonus { get; set; }
    public RewardType SpecialReward { get; set; }
}
```

## Балансировка

### Экономика Очков
- **Базовые Действия**: 3-5 очков
- **Ежедневные Цели**: 20-50 очков
- **Достижения**: 50-500 очков
- **Стрики**: Множитель x1.5-x3

### Прогрессия Уровней
- **Начальные Уровни**: Быстрая прогрессия (100-200 XP)
- **Средние Уровни**: Умеренная прогрессия (300-500 XP)
- **Высокие Уровни**: Медленная прогрессия (600+ XP)

## Аналитика Игровой Системы

### Метрики Вовлеченности
- **Daily Active Users (DAU)**: Ежедневные активные пользователи
- **Retention Rate**: Процент возвращающихся пользователей
- **Session Length**: Средняя длительность сессии
- **Feature Usage**: Использование игровых функций

### A/B Тестирование
- **Награды**: Тестирование различных размеров наград
- **Сложность**: Балансировка сложности достижений
- **Социальные Функции**: Эффективность социальных элементов

## Будущие Улучшения
- **Сезонные События**: Специальные события с уникальными наградами
- **Гильдии**: Групповые соревнования и цели
- **Персонализированные Вызовы**: ИИ-генерируемые цели
- **Интеграция с Реальным Миром**: Награды за активность вне приложения
- **Расширенная Кастомизация**: Больше косметических наград 