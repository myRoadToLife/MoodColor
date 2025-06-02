# Системные Паттерны MoodColor

## Системная Архитектура
MoodColor следует модульной архитектуре с внедрением зависимостей (DI) для обеспечения слабой связи между компонентами. Архитектура построена на принципах SOLID и использует паттерн Facade для унификации доступа к Firebase сервисам.

## 🏗️ Архитектурная Группировка Скриптов

Проект MoodColor организован в **9 архитектурных групп** с четкими уровнями зависимостей:

### **ГРУППА 1: ЯДРО СИСТЕМЫ (Core Infrastructure)** 🔴
> ⚠️ **Высокая связанность** - изменения влияют на весь проект

**DI System:**
- `Assets/App/Develop/DI/DIContainer.cs` - Основной контейнер DI
- `Assets/App/Develop/DI/IInitializable.cs` - Интерфейс для инициализации
- `Assets/App/Develop/DI/IInjectable.cs` - Интерфейс для инъекции
- `Assets/App/Develop/DI/IServiceRegistrator.cs` - Регистрация сервисов
- `Assets/App/Develop/DI/Installers/` - Установщики сервисов

**EntryPoint:**
- `Assets/App/Develop/EntryPoint/EntryPoint.cs` - Главная точка входа приложения
- `Assets/App/Develop/EntryPoint/Bootstrap.cs` - Bootstrapper для инициализации

**Utils:**
- `Assets/App/Develop/Utils/Logging/MyLogger.cs` - Система логирования
- `Assets/App/Develop/Utils/Events/EventBus.cs` - Система событий
- `Assets/App/Develop/Utils/Reactive/` - Реактивные переменные

**Зависимости:** Нет внешних зависимостей
**Безопасность изменений:** ❌ Критично для всей системы

### **ГРУППА 2: БАЗОВЫЕ СЕРВИСЫ (Foundation Services)** 🟡
> ⚠️ **Средняя связанность** - изменения влияют на бизнес-логику

**Data Management:**
- `Assets/App/Develop/CommonServices/DataManagement/` - Система сохранения/загрузки
- Интерфейсы: `ISaveData`, `IDataSerializer`, `IDataRepository`
- Реализации: `JsonSerializer`, `LocalDataRepository`, `SaveDataKeys`
- Провайдеры: `DataProvider`, `GameData`, `PlayerDataProvider`

**Asset & Scene Management:**
- `Assets/App/Develop/CommonServices/AssetManagement/` - Управление Addressables
- `Assets/App/Develop/CommonServices/SceneManagement/` - Загрузчик сцен

**Зависимости:** Utils, DI System
**Безопасность изменений:** ⚠️ Влияет на сохранение данных

### **ГРУППА 3: FIREBASE ИНТЕГРАЦИЯ (External Integration)** 🟢
> ✅ **Слабая связанность** - изолировано через интерфейсы

**Firebase Core:**
```
Assets/App/Develop/CommonServices/Firebase/
├── IFirebaseServiceFacade.cs      # Главный интерфейс Firebase
├── FirebaseServiceFacade.cs       # Реализация фасада
├── Auth/Services/                 # Сервисы аутентификации
├── Database/Services/             # Сервисы базы данных
├── Analytics/Services/            # Сервисы аналитики
├── Messaging/Services/            # Сервисы сообщений
├── RemoteConfig/Services/         # Удаленная конфигурация
└── Common/                        # Общие компоненты (Cache, SecureStorage)
```

**Зависимости:** Foundation Services, Utils
**Безопасность изменений:** ✅ Независимо через интерфейсы

### **ГРУППА 4: ИГРОВАЯ ЛОГИКА (Game Logic)** 🟢
> ✅ **Слабая связанность** - независимые игровые механики

**Emotion System:**
- `Assets/App/Develop/CommonServices/Emotion/EmotionService.cs`
- `Assets/App/Develop/CommonServices/Emotion/EmotionSyncService.cs`

**Game System:**
- Интерфейсы: `IPointsService`, `IAchievementCondition`
- Модели: `PointsTransaction`, `XPSource`
- Условия: `DailyMindfulnessCondition`, `EmotionSpectrumCondition`

**Social System:**
- `Assets/App/Develop/CommonServices/Social/FirebaseSocialService.cs`

**Зависимости:** Firebase Services, Foundation Services
**Безопасность изменений:** ✅ Независимо

### **ГРУППА 5: СИСТЕМНЫЕ СЕРВИСЫ (System Services)** 🟢
> ✅ **Слабая связанность** - вспомогательные системы

**Notifications:**
- Интерфейсы: `INotificationService`, `INotificationManager`, `INotificationCoordinator`
- Реализации: `UserPreferencesManager`, `NotificationTriggerSystem`, `InGameNotificationService`

**Networking & Loading:**
- `Assets/App/Develop/CommonServices/Networking/ConnectivityManager.cs`
- `Assets/App/Develop/CommonServices/LoadingScreen/` - Экраны загрузки
- `Assets/App/Develop/CommonServices/CoroutinePerformer/` - Выполнители корутин

**Зависимости:** Foundation Services
**Безопасность изменений:** ✅ Независимо

### **ГРУППА 6: ПОЛЬЗОВАТЕЛЬСКИЙ ИНТЕРФЕЙС (UI Layer)** 🟢
> ✅ **Слабая связанность** - только UI логика

**Core UI Services:**
- `Assets/App/Develop/CommonServices/UI/UIFactory.cs` - Фабрика UI
- `Assets/App/Develop/CommonServices/UI/PanelManager.cs` - Менеджер панелей
- `Assets/App/Develop/CommonServices/UI/NotificationManager.cs` - UI уведомления

**Generic UI Components:**
- `Assets/App/Develop/UI/Components/` - Переиспользуемые UI компоненты
- Компоненты: `JarView`, `FriendsPanel`, `FriendsListGenerator`

**Зависимости:** Game Logic, System Services (через интерфейсы)
**Безопасность изменений:** ✅ Независимо

### **ГРУППА 7: СЦЕНЫ И СПЕЦИФИЧНАЯ ЛОГИКА (Scene-Specific)** 🟢
> ✅ **Слабая связанность** - логика конкретных сцен

**Personal Area Scene:**
```
Assets/App/Develop/Scenes/PersonalAreaScene/
├── Infrastructure/PersonalAreaBootstrap.cs    # Bootstrapper сцены
├── UI/PersonalAreaManager.cs                  # Менеджер UI сцены
├── Panels/                                    # Панели (History, Friends, Settings)
├── Handlers/JarInteractionHandler.cs          # Обработчики взаимодействий
└── UI/Components/                             # UI компоненты сцены
```

**Auth Scene:**
- `Assets/App/Develop/Scenes/AuthScene/` - Компоненты аутентификации

**Зависимости:** UI Layer, Game Logic (через интерфейсы)
**Безопасность изменений:** ✅ Независимо

### **ГРУППА 8: КОНФИГУРАЦИИ (Configuration)** 🟢
> ✅ **Слабая связанность** - только данные конфигурации

**Configs Management:**
- `Assets/App/Develop/CommonServices/ConfigsManagement/` - Сервисы управления конфигурациями

**Emotion Configs:**
```
Assets/App/Develop/Configs/Common/Emotion/
├── EmotionConfig.cs               # Базовая конфигурация
├── LoveConfig.cs, TrustConfig.cs  # Конфигурации конкретных эмоций
└── AngerConfig.cs, SadnessConfig.cs, etc.
```

**Application Configs:**
- `Assets/App/Develop/Configs/ApplicationConfig.cs` - Основная конфигурация

**Зависимости:** Нет
**Безопасность изменений:** ✅ Свободно

### **ГРУППА 9: ВНЕШНИЕ СЕРВИСЫ (External Services)** 🟢
> ✅ **Слабая связанность** - внешние интеграции

**Friends Service:**
- `Assets/App/Develop/Services/Friends/IFriendsService.cs`

**App Services:**
- `Assets/App/Develop/AppServices/Auth/UI/UIAnimator.cs`

**Зависимости:** Foundation Services
**Безопасность изменений:** ✅ Независимо

## 📊 Матрица Зависимостей

| ГРУППА              | Сложность | Влияние | Можно менять | Риск |
|-------------------|----------|---------|-------------|------|
| 1. Core Infra     | 🔴 Высокая | 🔴 Критичное | ❌ Нет | 🔴 Высокий |
| 2. Foundation     | 🟡 Средняя | 🟡 Заметное | ⚠️ Осторожно | 🟡 Средний |
| 3. Firebase       | 🟢 Низкая | 🟢 Низкое | ✅ Да | 🟢 Низкий |
| 4. Game Logic     | 🟢 Низкая | 🟢 Низкое | ✅ Да | 🟢 Низкий |
| 5. System Services| 🟢 Низкая | 🟢 Низкое | ✅ Да | 🟢 Низкий |
| 6. UI Layer       | 🟢 Низкая | 🟢 Низкое | ✅ Да | 🟢 Низкий |
| 7. Scene-Specific | 🟢 Низкая | 🟢 Низкое | ✅ Да | 🟢 Низкий |
| 8. Configuration  | 🟢 Низкая | 🟢 Низкое | ✅ Да | 🟢 Низкий |
| 9. External       | 🟢 Низкая | 🟢 Низкое | ✅ Да | 🟢 Низкий |

## Основные Архитектурные Компоненты

### Точка Входа (EntryPoint.cs)
EntryPoint служит центром инициализации приложения, где все сервисы регистрируются в контейнере DI. Он отвечает за:
- Инициализацию базовых настроек приложения (60 FPS, разрешение экрана)
- Настройку и запуск системы внедрения зависимостей с поддержкой циклических зависимостей
- Инициализацию Firebase (Auth, Database, Analytics, Messaging, RemoteConfig)
- Регистрацию основных сервисов и запуск Bootstrap-процесса
- Обеспечение корректного порядка инициализации компонентов

### Слой Сервисов
Сервисы инкапсулируют основную бизнес-логику и функциональность, следуя принципу единственной ответственности:
- **FirebaseServiceFacade**: Единая точка доступа ко всем Firebase сервисам
- **EmotionService**: Полностью реализованная система управления эмоциями с Firebase синхронизацией
- **AuthService & AuthStateService**: Управление аутентификацией и состоянием пользователя
- **DatabaseService**: Отвечает за хранение и синхронизацию данных с облаком
- **PointsService & LevelSystem**: Игровые механики (очки, уровни, опыт)
- **AchievementService**: Система достижений для мотивации пользователей
- **SocialService**: Социальные функции (друзья, поиск пользователей)
- **EmotionConfigService**: Управление конфигурациями эмоций через Addressables

### Слой UI
Реализация пользовательского интерфейса отделена от бизнес-логики:
- **PanelManager**: Централизованное управление UI-панелями
- **Компоненты UI**: Модульные элементы для различных экранов приложения
- **Экраны**: Основные представления приложения (Auth, PersonalArea, EmotionSelection)
- **MonoFactory**: Фабрика для создания UI компонентов с поддержкой DI

## Паттерны Проектирования

### Facade Pattern (FirebaseServiceFacade)
Центральный паттерн для унификации доступа к Firebase сервисам:
```csharp
public interface IFirebaseServiceFacade
{
    IDatabaseService Database { get; }
    IAuthService Auth { get; }
    IAuthStateService AuthState { get; }
    IFirebaseAnalyticsService Analytics { get; }
    IFirebaseMessagingService Messaging { get; }
    IFirebaseRemoteConfigService RemoteConfig { get; }
}
```

### Внедрение Зависимостей (DIContainer)
Собственная реализация контейнера зависимостей с расширенными возможностями:
- Регистрация синглтонов и фабричных методов
- Поддержка циклических зависимостей
- Отложенная (Lazy) и немедленная (NonLazy) инициализация
- Иерархические контейнеры (parent/child)
- Автоматическая инжекция зависимостей в MonoBehaviour компоненты

### Паттерн Repository
Используется для работы с данными в Firebase:
- **EmotionDatabaseService**: Репозиторий для работы с эмоциями
- **UserProfileDatabaseService**: Управление профилями пользователей
- **GameDataDatabaseService**: Игровые данные (очки, уровни, достижения)

### Паттерн Observer
Реализован через события для реактивных обновлений:
- **EmotionService.OnEmotionEvent**: События изменения эмоций
- **AuthStateService**: Отслеживание состояния аутентификации
- **Reactive Variables**: Для автоматического обновления UI

### Паттерн Cache
Оптимизированное кэширование для производительности:
- **EmotionHistoryCache**: Кэширование истории эмоций
- **FirebaseCacheManager**: Общий менеджер кэша Firebase
- **Addressables Cache**: Кэширование ассетов и конфигураций

### Паттерн Strategy
Используется для различных алгоритмов:
- **EmotionMixingRules**: Стратегии смешивания эмоций
- **PointsSource**: Различные источники начисления очков
- **XPSource**: Различные источники опыта

## Архитектура Firebase

### Модульная Структура Firebase
```
Firebase/
├── Auth/                 # Аутентификация (Email, Google)
├── Database/             # Realtime Database с оптимизированными запросами
│   ├── Services/         # Специализированные сервисы БД
│   ├── Models/           # Модели данных
│   └── Cache/            # Система кэширования
├── Analytics/            # Аналитика пользователей
├── Messaging/            # Push уведомления
├── RemoteConfig/         # Удаленная конфигурация
└── Common/               # Общие компоненты
```

### Database Services Hierarchy
```
IDatabaseService (Facade)
├── IUserProfileDatabaseService    # Профили пользователей
├── IJarDatabaseService           # Данные банок эмоций
├── IGameDataDatabaseService      # Игровые данные
├── ISessionManagementService     # Управление сессиями
├── IBackupDatabaseService        # Резервное копирование
├── IEmotionDatabaseService       # Эмоции и история
└── ISocialDatabaseService        # Социальные функции
```

## Система Эмоций

### Архитектурные Компоненты
- **EmotionService**: Основной сервис с полной Firebase интеграцией
- **EmotionConfigService**: Конфигурации эмоций через Addressables
- **EmotionSyncService**: Синхронизация с Firebase
- **EmotionHistoryCache**: Кэширование истории для производительности
- **EmotionMixingRules**: Правила смешивания эмоций

### Типы Эмоций (11 типов)
```csharp
public enum EmotionTypes
{
    Joy, Sadness, Anger, Fear, Disgust, Trust, 
    Anticipation, Surprise, Love, Anxiety, Neutral
}
```

### Workflow Эмоций
1. Пользователь выбирает эмоцию через UI
2. EmotionService создает EmotionHistoryRecord с уникальным ID
3. Данные сохраняются локально в кэш
4. При наличии сети - синхронизация с Firebase через батчинг
5. Обновление UI через события Observer pattern
6. Начисление очков и опыта через игровые сервисы

## Игровая Система

### Компоненты
- **PointsService**: Управление очками с различными источниками
- **LevelSystem**: Система уровней с опытом и прогрессией
- **AchievementService**: Достижения для мотивации пользователей

### Источники Очков и Опыта
```csharp
public enum PointsSource
{
    EmotionTracking,    // За отслеживание эмоций
    JarInteraction,     // За взаимодействие с банкой
    Achievement,        // За достижения
    DailyBonus,         // Ежедневный бонус
    SocialActivity      // Социальная активность
}

public enum XPSource
{
    EmotionMarked,      // За отметку эмоции
    EmotionMixed,       // За смешивание эмоций
    Achievement,        // За достижения
    SocialInteraction   // За социальные взаимодействия
}
```

## 🎯 Рекомендации по Работе с Архитектурой

### ✅ **Безопасно можно менять:**
- **Группы 3-9** - изолированы через интерфейсы
- UI компоненты сцен - независимая логика представления
- Конфигурации эмоций - только данные
- Firebase сервисы - через четкие интерфейсы
- Игровые механики - слабо связанные модули

### ⚠️ **Осторожно менять:**
- **Группу 2** - влияет на сохранение и синхронизацию данных
- Интерфейсы сервисов - могут нарушить совместимость
- Модели данных - влияют на Firebase структуру

### ❌ **Избегать изменений:**
- **Группу 1** - ядро всей системы
- DI Container - основа для всех зависимостей
- EntryPoint логику - центр инициализации
- Базовые интерфейсы Utils

### 🔑 **Ключевые точки интеграции:**
- `IFirebaseServiceFacade` - для всех Firebase операций
- `IDatabaseService` - для работы с данными
- `DIContainer` - для регистрации новых сервисов
- `MyLogger` - для системы логирования
- `EmotionService` - для работы с эмоциями

## Взаимосвязи Компонентов
- **EntryPoint → DIContainer**: Инициализирует и конфигурирует контейнер DI
- **DIContainer → Сервисы**: Разрешает зависимости между сервисами с поддержкой циклических ссылок
- **Bootstrap → Сцены**: Управляет загрузкой начальных сцен приложения
- **FirebaseServiceFacade → Специализированные Сервисы**: Унифицированный доступ к Firebase
- **EmotionService → Firebase**: Синхронизация через EmotionDatabaseService
- **UI → Сервисы**: Реактивные обновления через Observer pattern

## Поток Данных
1. Пользователь взаимодействует с элементами UI
2. UI-компоненты вызывают методы соответствующих сервисов через DI
3. Сервисы выполняют бизнес-логику и взаимодействуют с кэшем
4. Данные синхронизируются с Firebase при наличии соединения через батчинг
5. Изменения отображаются в UI через систему событий
6. Игровые механики (очки, опыт) обновляются автоматически

## Обработка Ошибок и Логирование
- **MyLogger**: Система логирования с категориями для детальной диагностики
- **DataValidationService**: Валидация данных перед отправкой в Firebase
- **ConnectivityManager**: Отслеживание состояния сети
- **Graceful Degradation**: Работа в офлайн режиме с последующей синхронизацией

## Производительность и Оптимизация
- **Addressables**: Динамическая загрузка ассетов и конфигураций
- **Object Pooling**: Для часто создаваемых UI элементов
- **Batch Operations**: Пакетные операции для Firebase
- **Lazy Loading**: Отложенная инициализация тяжелых компонентов
- **Caching Strategy**: Многоуровневое кэширование данных

Эта архитектура обеспечивает высокую модульность, тестируемость и удобство сопровождения, предоставляя прочную основу для масштабирования и будущих расширений. Четкая группировка по зависимостям позволяет безопасно работать с большинством модулей независимо от остальной системы. 