# 📋 Контрольная Документация Проекта MoodColor

## 🎯 Обзор Проекта

**MoodColor** - Unity приложение для отслеживания и анализа эмоций пользователя с облачной синхронизацией через Firebase.

### Технические Требования
- **Unity версия:** Unity 6.1 (Unity 6000.1)
- **Платформы:** Android, iOS
- **Минимальная версия Android:** API 24 (Android 7.0)
- **Минимальная версия iOS:** iOS 12.0

### Основные Возможности
- ✅ Отслеживание эмоций с цветовой визуализацией
- ✅ Система очков и достижений
- ✅ Облачная синхронизация данных (Firebase)
- ✅ Аутентификация пользователей
- ✅ Социальные функции (друзья, поиск)
- ✅ Персональная область с настройками
- ✅ Система уведомлений

---

## 🏗️ Архитектурный Обзор

### Принципы Архитектуры
- **SOLID принципы** - четкое разделение ответственности
- **Dependency Injection** - собственная реализация DI-контейнера
- **Модульность** - разделение на независимые сервисы
- **Facade Pattern** - единая точка доступа к Firebase сервисам
- **Observer Pattern** - реактивные обновления UI

### Структура Проекта
```
Assets/App/Develop/
├── EntryPoint/           # Точка входа приложения
├── DI/                   # Система внедрения зависимостей
├── CommonServices/       # Основные сервисы
│   ├── Firebase/         # Firebase интеграция
│   ├── Emotion/          # Система эмоций
│   ├── GameSystem/       # Игровая механика
│   ├── UI/               # UI компоненты
│   ├── DataManagement/   # Управление данными
│   └── ...
├── Scenes/               # Сцены приложения
├── Configs/              # Конфигурационные файлы
└── Utils/                # Утилиты
```

---

## 🔧 Система Внедрения Зависимостей

### DIContainer
**Расположение:** `Assets/App/Develop/DI/DIContainer.cs`

**Особенности:**
- ✅ Собственная реализация DI-контейнера
- ✅ Поддержка Singleton регистрации
- ✅ Lazy/NonLazy инициализация
- ✅ Обнаружение циклических зависимостей
- ✅ Иерархические контейнеры (parent/child)

**Основные интерфейсы:**
- `IInjectable` - для компонентов, требующих внедрения зависимостей
- `IInitializable` - для компонентов с инициализацией
- `IServiceRegistrator` - для группировки регистрации сервисов

### Паттерны Регистрации
```csharp
// Singleton с фабричным методом
container.RegisterAsSingle<IService>(c => 
    new ServiceImpl(c.Resolve<IDependency>())
).NonLazy();

// Компонент Unity с DI
container.RegisterAsSingle<MonoBehaviourService>(c => {
    var go = new GameObject("ServiceName");
    var service = go.AddComponent<MonoBehaviourService>();
    if (service is IInjectable injectable)
        injectable.Inject(c);
    return service;
}).NonLazy();
```

---

## 🔥 Firebase Архитектура

### Модульная Структура
```
Firebase/
├── Auth/                 # Аутентификация
├── Database/             # Realtime Database
├── Analytics/            # Аналитика
├── Messaging/            # Push уведомления
├── RemoteConfig/         # Удаленная конфигурация
└── Common/               # Общие компоненты
```

### FirebaseServiceFacade
**Паттерн:** Facade
**Цель:** Единая точка доступа ко всем Firebase сервисам

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

### Database Services Hierarchy
```
IDatabaseService (Facade)
├── IUserProfileDatabaseService
├── IJarDatabaseService  
├── IGameDataDatabaseService
├── ISessionManagementService
├── IBackupDatabaseService
└── IEmotionDatabaseService
```

---

## 😊 Система Эмоций

### Основные Компоненты
- **EmotionService** - основной сервис управления эмоциями
- **EmotionConfigService** - конфигурация эмоций
- **EmotionSyncService** - синхронизация с Firebase
- **EmotionHistoryCache** - кэширование истории

### Типы Эмоций
```csharp
public enum EmotionTypes
{
    Joy, Sadness, Anger, Fear, 
    Surprise, Disgust, Trust, 
    Anticipation, Love
}
```

### Workflow Эмоций
1. Пользователь выбирает эмоцию
2. EmotionService создает EmotionHistoryRecord
3. Данные сохраняются локально
4. При наличии сети - синхронизация с Firebase
5. Обновление UI через события

---

## 🎮 Игровая Система

### Компоненты
- **IPointsService** - управление очками
- **ILevelSystem** - система уровней
- **IAchievementService** - достижения

### Источники Очков
```csharp
public enum PointsSource
{
    EmotionTracking,    // За отслеживание эмоций
    JarInteraction,     // За взаимодействие с банкой
    Achievement,        // За достижения
    DailyBonus,         // Ежедневный бонус
    SocialActivity      // Социальная активность
}
```

---

## 🔐 Система Аутентификации

### Компоненты
- **AuthService** - основной сервис аутентификации
- **AuthStateService** - отслеживание состояния
- **UserProfileService** - управление профилем
- **ValidationService** - валидация данных
- **CredentialStorage** - безопасное хранение

### Поддерживаемые Методы
- ✅ Email/Password
- ✅ Google Sign-In
- ✅ Автоматический вход
- ✅ Верификация email
- ✅ Восстановление пароля

---

## 📱 UI Архитектура

### Структура
```
UI/
├── Base/                 # Базовые компоненты
├── Components/           # Переиспользуемые компоненты
├── Panels/               # Панели интерфейса
└── Factories/            # Фабрики создания UI
```

### UI Технологии
- **Unity uGUI** - основная UI система
- **TextMeshPro** - продвинутый рендеринг текста (встроен в Unity 6.1)
- **Addressable Assets** - динамическая загрузка UI ресурсов

### TextMeshPro Интеграция
```csharp
// Основные компоненты в проекте
TextMeshProUGUI     // UI текст
TMP_Dropdown        // Выпадающие списки в настройках
TMP_InputField      // Поля ввода данных
TMP_FontAsset       // Кастомные шрифты (BrushyFont)
```

### Основные Компоненты
- **UIFactory** - создание UI элементов
- **PanelManager** - управление панелями
- **MonoFactory** - создание MonoBehaviour с DI

---

## 🚀 Инициализация Приложения

### EntryPoint Workflow
```
1. Addressables.InitializeAsync()
2. SetupAppSettings()
3. RegisterCoreServices()
4. InitFirebaseAsync()
5. RegisterFirebaseServices()
6. InitializeContainerAndLoadData()
7. StartBootstrapProcess()
```

### Порядок Регистрации Сервисов
1. **Core Services** - базовые сервисы (AssetLoader, CoroutinePerformer, etc.)
2. **Firebase Services** - через FirebaseServiceInstaller
3. **Game Services** - игровая логика
4. **UI Services** - пользовательский интерфейс

---

## ✅ Реализованный Функционал

### 🔥 Firebase Интеграция
- ✅ Realtime Database с кэшированием
- ✅ Authentication (Email, Google)
- ✅ Analytics события
- ✅ Push уведомления
- ✅ Remote Config

### 😊 Система Эмоций
- ✅ 9 типов эмоций с конфигурацией
- ✅ История эмоций с временными метками
- ✅ Цветовая визуализация
- ✅ Статистика и аналитика
- ✅ Облачная синхронизация

### 🎮 Игровая Механика
- ✅ Система очков с источниками
- ✅ Уровни и прогрессия
- ✅ Достижения
- ✅ Ежедневные бонусы
(Оставшаяся часть документа...) 