# Firebase Service Facade

## Описание
Главный фасад для сервисов Firebase, обеспечивающий единую точку доступа ко всем сервисам Firebase в приложении.

## Структура
Firebase сервисы в приложении разделены на следующие модули:

- **Database** - сервисы для работы с Firebase Realtime Database
- **Auth** - сервисы аутентификации Firebase
- **Analytics** - сервисы Firebase Analytics
- **Messaging** - сервисы Firebase Cloud Messaging
- **RemoteConfig** - сервисы Firebase Remote Config

Каждый модуль имеет свой набор интерфейсов и реализаций.

## Архитектура

```
IFirebaseServiceFacade
        |
        |--- IDatabaseService (Database)
        |--- IAuthService (Auth)
        |--- IAuthStateService (Auth)
        |--- IFirebaseAnalyticsService (Analytics)
        |--- IFirebaseMessagingService (Messaging)
        |--- IFirebaseRemoteConfigService (RemoteConfig)
```

## Использование

### Получение фасада через DI-контейнер

```csharp
// Получение фасада из контейнера зависимостей
var firebaseService = Container.Resolve<IFirebaseServiceFacade>();
```

### Пример работы с аутентификацией

```csharp
// Вход с использованием email и пароля
var result = await firebaseService.Auth.SignInWithEmailPasswordAsync(email, password);

if (result.IsSuccess)
{
    Debug.Log($"Успешный вход: {result.User.Email}");
}
else
{
    Debug.LogError($"Ошибка входа: {result.ErrorMessage}");
}
```

### Пример работы с базой данных

```csharp
// Получение профиля пользователя
var profile = await firebaseService.Database.GetUserProfileAsync();

// Обновление профиля
profile.DisplayName = "Новое имя";
await firebaseService.Database.UpdateUserProfileAsync(profile);
```

### Пример работы с аналитикой

```csharp
// Логирование события
firebaseService.Analytics.LogEvent("screen_view", "screen_name", "MainMenu");

// Установка свойства пользователя
firebaseService.Analytics.SetUserProperty("user_type", "premium");
```

### Пример работы с облачными сообщениями

```csharp
// Получение токена FCM
string token = await firebaseService.Messaging.GetTokenAsync();

// Подписка на тему
await firebaseService.Messaging.SubscribeToTopic("news");

// Подписка на события получения сообщений
firebaseService.Messaging.OnMessageReceived += message =>
{
    Debug.Log($"Получено сообщение: {message.Notification.Title}");
};
```

### Пример работы с удаленной конфигурацией

```csharp
// Загрузка и активация конфигурации
await firebaseService.RemoteConfig.FetchAndActivateAsync();

// Получение значений
bool featureEnabled = firebaseService.RemoteConfig.GetBool("feature_enabled");
int cooldownTime = firebaseService.RemoteConfig.GetInt("cooldown_time");
```

## Примеры использования

Для более подробных примеров использования фасада, смотрите класс `FirebaseServiceExample` в папке `Examples`.

## Миграция со старого кода

Если в вашем коде используются напрямую классы `DatabaseService`, `AuthService` и т.д., рекомендуется постепенно перейти на использование фасада `IFirebaseServiceFacade`.

### Было:
```csharp
private DatabaseService _databaseService;
private AuthService _authService;

// Использование
_databaseService.GetUserProfileAsync();
_authService.SignInWithEmailPasswordAsync(email, password);
```

### Стало:
```csharp
private IFirebaseServiceFacade _firebaseService;

// Использование
_firebaseService.Database.GetUserProfileAsync();
_firebaseService.Auth.SignInWithEmailPasswordAsync(email, password);
```

## Расширение функциональности

Для добавления новых сервисов Firebase:

1. Создайте интерфейс и реализацию нового сервиса
2. Добавьте соответствующее свойство в `IFirebaseServiceFacade`
3. Обновите класс `FirebaseServiceFacade`
4. Обновите `FirebaseServiceInstaller` для регистрации нового сервиса в DI-контейнере 