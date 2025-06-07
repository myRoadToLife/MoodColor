# 🔒 Панель настроек конфиденциальности - Руководство по реализации

## 📋 Обзор

Реализована система настроек конфиденциальности, которая позволяет пользователю:

1. **🗺️ Выбрать свой район** из списка (23 региона России + международные)
2. **🔒 Управлять согласием** на добавление данных в общую статистику
3. **📍 Контролировать использование геолокации**
4. **🔐 Настраивать анонимизацию данных**

## 🏗️ Архитектура

### Созданные компоненты:

1. **`IPrivacyService`** - интерфейс сервиса конфиденциальности
2. **`PrivacyService`** - реализация сервиса (пока не интегрирована)
3. **`PrivacyPanel`** - UI контроллер панели настроек
4. **Интеграция с `SettingsPanelController`** - добавлена кнопка "Настройки конфиденциальности"

### Структура файлов:

```
Assets/
├── App/Develop/CommonServices/Privacy/
│   ├── IPrivacyService.cs          # Интерфейс сервиса
│   └── PrivacyService.cs           # Реализация сервиса
├── Scripts/
│   └── PrivacyPanel.cs             # UI контроллер панели
└── docs/
    └── privacy-settings-implementation.md  # Эта документация
```

## 🎨 UI Компоненты

### PrivacyPanel требует следующие UI элементы:

#### Основные настройки:
- `Toggle _allowGlobalDataSharingToggle` - разрешить глобальную статистику
- `Toggle _allowLocationTrackingToggle` - разрешить геолокацию  
- `Toggle _anonymizeDataToggle` - анонимизировать данные

#### Выбор региона:
- `Toggle _useManualRegionToggle` - использовать ручной выбор
- `TMP_Dropdown _regionDropdown` - выпадающий список регионов
- `TMP_Text _currentLocationText` - текущее местоположение
- `Button _refreshLocationButton` - обновить местоположение

#### Информация:
- `TMP_Text _privacyInfoText` - информация о конфиденциальности
- `TMP_Text _dataUsageText` - как используются данные

#### Кнопки управления:
- `Button _saveButton` - сохранить настройки
- `Button _resetButton` - сбросить настройки
- `Button _revokeAllButton` - отозвать все согласия
- `Button _closeButton` - закрыть панель

#### Уведомления:
- `GameObject _notificationPanel` - панель уведомлений
- `TMP_Text _notificationText` - текст уведомления
- `Button _notificationOkButton` - кнопка OK

## 🗺️ Доступные регионы

Система поддерживает 35 регионов для Беларуси:

### Автоматический режим:
- 🤖 Автоматически (определение по GPS)

### Минск (столица - 5 зон):
- 🏛️ Минск - Центр
- 🏘️ Минск - Север  
- 🏢 Минск - Юг
- 🌅 Минск - Восток
- 🌇 Минск - Запад

### Брестская область (4):
- 🏰 Брест
- 🌾 Барановичи
- 🌲 Пинск
- 🏞️ Брестская область

### Витебская область (4):
- 🏛️ Витебск
- 🏔️ Полоцк
- 🌲 Орша
- 🍃 Витебская область

### Гомельская область (4):
- 🏭 Гомель
- ⚡ Мозырь
- 🌾 Речица
- 🌻 Гомельская область

### Гродненская область (4):
- 🏰 Гродно
- 🌸 Лида
- 🌳 Слоним
- 🏞️ Гродненская область

### Минская область (4):
- 🏭 Борисов
- ⚙️ Солигорск
- 🌾 Молодечно
- 🌿 Минская область

### Могилевская область (4):
- 🏛️ Могилев
- 🏭 Бобруйск
- 🌾 Кричев
- 🌻 Могилевская область

### Соседние страны и международные варианты (8):
- 🇧🇾 Другой регион Беларуси
- 🇷🇺 Россия
- 🇺🇦 Украина
- 🇱🇹 Литва
- 🇱🇻 Латвия
- 🇪🇪 Эстония
- 🇵🇱 Польша
- 🌍 Другая страна
- 🤐 Предпочитаю не указывать

## 💾 Сохранение настроек

Настройки сохраняются в `PlayerPrefs`:

```csharp
// Ключи для сохранения
"Privacy_AllowGlobalSharing"  // bool (1/0)
"Privacy_AllowLocation"       // bool (1/0) 
"Privacy_AnonymizeData"       // bool (1/0)
"Privacy_UseManualRegion"     // bool (1/0)
"Privacy_ManualRegion"        // string
```

## 🔧 Интеграция с существующей системой

### 1. SettingsPanelController

Добавлены:
- `Button _privacySettingsButton` - кнопка открытия панели
- `GameObject _privacyPanel` - ссылка на панель конфиденциальности
- `ShowPrivacyPanel()` - метод открытия панели

### 2. Public API PrivacyPanel

```csharp
// Получение настроек
bool GetAllowGlobalDataSharing()
bool GetAllowLocationTracking() 
bool GetAnonymizeData()
bool GetUseManualRegionSelection()
string GetManuallySelectedRegion()

// Получение эффективного региона
string GetEffectiveRegionId(string gpsRegionId)
```

## 🚀 Следующие шаги для завершения

### 1. Создание UI префаба

Создать префаб панели конфиденциальности с всеми необходимыми UI элементами:

```
PrivacyPanel (GameObject)
├── Background (Image)
├── Header (TMP_Text) - "🔒 Настройки конфиденциальности"
├── MainSettings (VerticalLayoutGroup)
│   ├── GlobalDataToggle (Toggle + Label)
│   ├── LocationToggle (Toggle + Label)
│   └── AnonymizeToggle (Toggle + Label)
├── RegionSettings (VerticalLayoutGroup)
│   ├── ManualRegionToggle (Toggle + Label)
│   ├── RegionDropdown (TMP_Dropdown)
│   ├── CurrentLocationText (TMP_Text)
│   └── RefreshLocationButton (Button)
├── InfoPanel (VerticalLayoutGroup)
│   ├── PrivacyInfoText (TMP_Text)
│   └── DataUsageText (TMP_Text)
├── ButtonsPanel (HorizontalLayoutGroup)
│   ├── SaveButton (Button)
│   ├── ResetButton (Button)
│   ├── RevokeAllButton (Button)
│   └── CloseButton (Button)
└── NotificationPanel (GameObject)
    ├── NotificationBackground (Image)
    ├── NotificationText (TMP_Text)
    └── NotificationOkButton (Button)
```

### 2. Подключение к SettingsPanel

1. Добавить PrivacyPanel как дочерний объект к SettingsPanel
2. Назначить ссылку `_privacyPanel` в SettingsPanelController
3. Добавить кнопку "Настройки конфиденциальности" в UI
4. Назначить ссылку `_privacySettingsButton` в SettingsPanelController

### 3. Интеграция с EmotionService

Обновить `EmotionService.LogEmotionEvent()` для использования настроек конфиденциальности:

```csharp
// В EmotionService
private PrivacyPanel _privacyPanel; // Получить через DI или FindObjectOfType

public async Task LogEmotionEvent(EmotionType emotionType)
{
    // Проверяем разрешение на сбор данных
    if (_privacyPanel != null && !_privacyPanel.GetAllowGlobalDataSharing())
    {
        MyLogger.Log("🚫 Пользователь запретил сбор данных", MyLogger.LogCategory.Regional);
        return; // Не сохраняем данные
    }

    // Получаем эффективный регион
    string gpsRegionId = await TryGetLocationData();
    string effectiveRegionId = _privacyPanel?.GetEffectiveRegionId(gpsRegionId) ?? "default";
    
    // Создаем запись эмоции с учетом настроек
    var emotionRecord = new EmotionHistoryRecord
    {
        EmotionType = emotionType,
        Timestamp = DateTime.UtcNow,
        RegionId = effectiveRegionId,
        IsAnonymized = _privacyPanel?.GetAnonymizeData() ?? false
    };

    // Сохраняем локально
    await SaveEmotionLocally(emotionRecord);
    
    // Обновляем региональную статистику только если разрешено
    if (_privacyPanel?.GetAllowGlobalDataSharing() ?? true)
    {
        await TryUpdateRegionalStats(effectiveRegionId, emotionType);
    }
}
```

### 4. Регистрация PrivacyService в DI

После создания UI панели, добавить в `EntryPoint.cs`:

```csharp
// В InitializeApplication()
MyLogger.Log("🔒 Регистрация PrivacyService...", MyLogger.LogCategory.Bootstrap);
RegisterPrivacyService(_projectContainer);

// Новый метод
private void RegisterPrivacyService(DIContainer container)
{
    try
    {
        MyLogger.Log("🔒 Регистрация PrivacyService...", MyLogger.LogCategory.Bootstrap);
        
        container.RegisterAsSingle<IPrivacyService>(c => new PrivacyService()).NonLazy();
        
        MyLogger.Log("✅ PrivacyService зарегистрирован успешно", MyLogger.LogCategory.Bootstrap);
    }
    catch (Exception ex)
    {
        MyLogger.LogError($"❌ Ошибка регистрации PrivacyService: {ex.Message}", MyLogger.LogCategory.Bootstrap);
        throw;
    }
}
```

### 5. Тестирование

1. **Функциональное тестирование:**
   - Переключение настроек сохраняется
   - Выбор региона работает корректно
   - Кнопки "Сбросить" и "Отозвать согласия" работают
   - Уведомления отображаются правильно

2. **Интеграционное тестирование:**
   - EmotionService учитывает настройки конфиденциальности
   - Региональная статистика обновляется только при согласии
   - Геолокация используется только при разрешении

3. **UX тестирование:**
   - Интуитивно понятный интерфейс
   - Понятные описания настроек
   - Быстрый доступ к настройкам из главной панели

## 🎯 Результат

После завершения всех шагов пользователь сможет:

✅ **Выбрать свой район** из списка 23 регионов  
✅ **Отказаться от участия** в глобальной статистике  
✅ **Контролировать геолокацию** и анонимизацию данных  
✅ **Легко управлять настройками** через удобный интерфейс  

Система будет полностью соответствовать требованиям конфиденциальности и даст пользователю полный контроль над своими данными.

## 🔗 Связанные файлы

- `Assets/Scripts/PrivacyPanel.cs` - основной контроллер
- `Assets/App/Develop/CommonServices/Privacy/IPrivacyService.cs` - интерфейс сервиса
- `Assets/App/Develop/CommonServices/Privacy/PrivacyService.cs` - реализация сервиса
- `Assets/App/Develop/Scenes/PersonalAreaScene/Panels/SettingsPanel/SettingsPanelController.cs` - интеграция
- `docs/data-collection-system.md` - общая документация системы сбора данных 