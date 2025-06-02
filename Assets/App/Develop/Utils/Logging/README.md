# MyLogger - Простая система логирования

Простая и понятная система логирования с поддержкой категорий.

## Быстрый старт

```csharp
// Основное использование
MyLogger.Log("Сообщение", MyLogger.LogCategory.UI);
MyLogger.LogWarning("Предупреждение", MyLogger.LogCategory.Network);
MyLogger.LogError("Ошибка", MyLogger.LogCategory.Firebase);

// С контекстом объекта
MyLogger.Log("Что-то происходит", this, MyLogger.LogCategory.Gameplay);
```

## LoggerController - Управление через Inspector

**LoggerController** - это MonoBehaviour компонент для визуального управления логированием прямо на сцене.

### Как использовать:

1. **Добавьте LoggerController на любой GameObject на сцене**:
   - Создайте пустой GameObject
   - Добавьте компонент `LoggerController`
   - Настройте категории логов через Inspector

2. **Функциональность LoggerController**:
   - ✅ **Визуальное управление** всеми категориями логов
   - ✅ **Быстрые профили** (Production, Development, Debug)
   - ✅ **Автосинхронизация** с MyLogger
   - ✅ **Кнопки тестирования** в Inspector
   - ✅ **Runtime UI** поддержка (опционально)
   - ✅ **Отображение статуса** в реальном времени

3. **Кнопки в Inspector**:
   - `Production Mode` - только критические логи
   - `Development Mode` - основные категории
   - `Debug Mode (All)` - все категории включены
   - `Firebase Debug` - только Firebase и Sync
   - `UI Debug` - только UI логи
   - `Session Debug` - только логи сессий
   - `Test All Categories` - тестирует все категории
   - `Sync from MyLogger` - синхронизирует с MyLogger

### Автоматические функции:

- **OnValidate**: изменения в Inspector автоматически применяются к MyLogger
- **Auto-sync**: отслеживает изменения MyLogger извне
- **Context Menu**: дополнительные команды через правый клик

## Управление категориями

```csharp
// Включить/отключить категорию
MyLogger.SetCategoryEnabled(MyLogger.LogCategory.UI, true);
MyLogger.SetCategoryEnabled(MyLogger.LogCategory.Network, false);

// Проверить состояние
if (MyLogger.IsCategoryEnabled(MyLogger.LogCategory.Firebase))
{
    // категория включена
}

// Массовые операции
MyLogger.EnableAllCategories();   // Включить все
MyLogger.DisableAllCategories();  // Отключить все
```

## Быстрые профили

```csharp
// Программно
MyLogger.SetProductionMode();      // Минимум логов
MyLogger.SetDevelopmentMode();     // Основные логи
MyLogger.SetDebugMode();          // Все логи
MyLogger.EnableFirebaseDebugMode(); // Firebase + Sync + Bootstrap
MyLogger.EnableUIDebugMode();     // UI + Bootstrap
MyLogger.EnableSessionDebugMode();  // Сессии + Firebase + Bootstrap

// Через LoggerController
// Просто нажмите кнопки в Inspector или используйте Context Menu
```

## Категории логов

| Категория | Описание | По умолчанию |
|-----------|----------|--------------|
| `Default` | Обычные логи | ✅ Включено |
| `Sync` | Синхронизация данных | ❌ Отключено |
| `UI` | Пользовательский интерфейс | ❌ Отключено |
| `Network` | Сетевые операции | ❌ Отключено |
| `Firebase` | Firebase операции | ✅ Включено |
| `Editor` | Editor скрипты | ✅ Включено |
| `Gameplay` | Игровая логика | ❌ Отключено |
| `Bootstrap` | Инициализация | ✅ Включено |
| `Emotion` | Система эмоций | ❌ Отключено |
| `ClearHistory` | Очистка истории | ❌ Отключено |
| `Regional` | Региональные настройки | ✅ Включено |
| `Session` | Управление сессиями пользователя | ❌ Отключено |

## Runtime UI (опционально)

LoggerController поддерживает runtime UI для управления логами во время игры:

```csharp
// Назначьте UI элементы в Inspector:
// - _categoryToggles: массив Toggle для каждой категории
// - _productionButton, _developmentButton, _debugButton
// - _statusText: TextMeshProUGUI для отображения статуса
```

## Особенности

- **Автоматическая настройка**: профиль выбирается при запуске в зависимости от билда
- **Editor-only методы**: `EditorLog`, `EditorLogWarning`, `EditorLogError` работают только в редакторе
- **Performance**: логи автоматически отключаются в Production билдах
- **Безопасность**: проверки на null, graceful degradation

## Примеры использования

```csharp
// В скрипте игры
public class GameManager : MonoBehaviour
{
    private void Start()
    {
        MyLogger.Log("Игра запущена", MyLogger.LogCategory.Bootstrap);
        
        // Включаем отладку UI на время разработки
        MyLogger.EnableUIDebugMode();
        
        MyLogger.Log("UI отладка включена", MyLogger.LogCategory.UI);
    }
}

// Использование LoggerController
public class DebugManager : MonoBehaviour
{
    [SerializeField] private LoggerController _loggerController;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _loggerController.SetDebugMode();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            _loggerController.SetProductionMode();
        }
    }
}
```

## Интеграция с существующим кодом

LoggerController полностью совместим с существующим MyLogger кодом. Просто добавьте его на сцену для удобного управления.

## Глобальные настройки

```csharp
// Управление типами логов
MyLogger.IsDebugLoggingEnabled = false;   // Отключить Debug.Log
MyLogger.IsWarningLoggingEnabled = true;  // Включить Debug.LogWarning
MyLogger.IsErrorLoggingEnabled = true;    // Включить Debug.LogError
```

## Доступные категории

- `Default` - Общие сообщения
- `UI` - Интерфейс пользователя  
- `Firebase` - Firebase и сетевые операции
- `Bootstrap` - Инициализация приложения
- `Gameplay` - Игровая логика
- `Sync` - Синхронизация данных
- `Network` - Сетевые операции
- `Editor` - Только в редакторе
- `Emotion` - Система эмоций
- `ClearHistory` - Очистка истории
- `Regional` - Региональная статистика
- `Session` - Управление сессиями пользователя

## Автоматическая настройка

Система автоматически выбирает профиль при запуске:
- **Editor** → Development режим
- **Development Build** → Development режим  
- **Release Build** → Production режим

## Производительность

- Логи автоматически отключаются в релизных сборках (кроме ошибок)
- Проверка категорий происходит только если общее логирование включено
- Минимальные накладные расходы на производительность 