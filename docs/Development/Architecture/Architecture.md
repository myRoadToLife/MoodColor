# MoodColor Architecture Document

## Introduction / Preamble

Этот документ описывает общую архитектуру проекта MoodColor, включая существующие системы, предлагаемые расширения для поддержки концепции "MoodRoom", а также ключевые технологические выборы и паттерны. Его основная цель — служить руководящим архитектурным планом для дальнейшей разработки, обеспечивая последовательность и соответствие выбранным паттернам и технологиям.

**Взаимосвязь с UI/UX Спецификацией:**
Этот документ дополняет UI/UX спецификацию MoodColor, которая детально описывает фронтенд-специфичный дизайн и пользовательский опыт. Основные технологические выборы, задокументированные здесь, являются определяющими для всего проекта.

## Table of Contents

*   [Introduction / Preamble](#introduction--preamble)
*   [Technical Summary](#technical-summary)
*   [High-Level Overview](#high-level-overview)
*   [Architectural / Design Patterns Adopted](#architectural--design-patterns-adopted)
*   [Component View](#component-view)
    *   [Существующие Компоненты](#существующие-компоненты)
    *   [Новые Компоненты для "MoodRoom"](#новые-компоненты-для-moodroom)
*   [Project Structure](#project-structure)
    *   [Key Directory Descriptions](#key-directory-descriptions)
*   [API Reference](#api-reference)
    *   [External APIs Consumed](#external-apis-consumed)
        *   [Firebase API](#firebase-api)
*   [Data Models](#data-models)
    *   [Core Application Entities / Domain Objects](#core-application-entities--domain-objects)
        *   [MoodEntry](#moodentry)
        *   [User](#user)
        *   [Emotion](#emotion)
        *   [Points & Level](#points--level)
*   [Coding Standards](#coding-standards)
*   [Testing Strategy](#testing-strategy)
*   [Technical Limitations](#technical-limitations)
*   [Performance Considerations](#performance-considerations)
*   [Deployment Considerations](#deployment-considerations)
*   [Local Development & Testing Requirements](#local-development--testing-requirements)
*   [Change Log](#change-log)

## Technical Summary

Архитектура MoodColor основана на Unity Engine с использованием C# и мощного бэкенда на Firebase. Система спроектирована с акцентом на модульность, слабую связанность (через DI-контейнер) и высокую масштабируемость. Для поддержки новой концепции "MoodRoom" архитектура будет расширена новыми модулями, отвечающими за сложную визуализацию, интерактивность и реактивное окружение, без нарушения существующей стабильной основы.

## High-Level Overview

MoodColor представляет собой **монолитное клиентское приложение Unity (Monorepo)**, которое взаимодействует с облачными сервисами Firebase для управления данными и аутентификацией. Основной поток данных включает взаимодействие клиента с Firebase для сохранения и извлечения информации о настроении, профилях пользователей и геймификации. UI и визуальные эффекты полностью обрабатываются на стороне клиента в Unity.

```mermaid
graph TD
    User -->|Взаимодействие| Unity_Client[Unity Client App];
    Unity_Client -->|SDK/API вызовы| Firebase_Auth[Firebase Authentication];
    Unity_Client -->|SDK/API вызовы| Firebase_DB[Firebase Realtime Database];
    Unity_Client -->|SDK/API вызовы| Firebase_Analytics[Firebase Analytics];
    Firebase_Auth -->|Данные аутентификации| Firebase_DB;
    Firebase_DB -->|Сохранение/Чтение данных| Unity_Client;

    subgraph Unity Client App
        UI_Layer[UI Layer (MoodRoom, Jars)];
        Business_Logic[Business Logic (Services, Core)];
        Data_Access[Data Access (Repositories, Caching)];
        DI_Container[DI Container];

        UI_Layer -- Вызовы --> Business_Logic;
        Business_Logic -- Использует --> Data_Access;
        Data_Access -- Зависит от --> DI_Container;
        DI_Container -- Инжектирует --> Business_Logic;
        DI_Container -- Инжектирует --> Data_Access;
    end
```

## Architectural / Design Patterns Adopted

*   **Dependency Injection (Внедрение Зависимостей):** _Rationale:_ Использование собственного легковесного `DIContainer` обеспечивает слабую связанность между компонентами, облегчает тестирование и повышает поддерживаемость кода. Это позволяет легко заменять реализации интерфейсов без изменения клиентского кода.
*   **Repository Pattern (Паттерн Репозиторий):** _Rationale:_ Инкапсулирует логику доступа к данным, предоставляя унифицированный интерфейс для работы с различными источниками данных (локальное хранилище, облачная база данных). Это обеспечивает чистоту бизнес-логики и облегчает миграцию между источниками данных.
*   **Facade Pattern (Паттерн Фасад):** _Rationale:_ `FirebaseServiceFacade` предоставляет упрощенный интерфейс для взаимодействия со сложным набором Firebase SDK, скрывая детали реализации и облегчая интеграцию.
*   **Observer Pattern (Паттерн Наблюдатель):** _Rationale:_ Используется для уведомления множества зависимых объектов об изменениях состояния в одном объекте (например, `HealthChanged` в `Health`). Это критически важно для реактивного окружения "MoodRoom", где изменения настроения должны вызывать визуальные обновления.
*   **Factory Pattern (Паттерн Фабрика):** _Rationale:_ Используется для централизованного создания сложных объектов, скрывая логику инстанцирования и конфигурирования. Полезно для создания префабов UI или персонажей эмоций.
*   **Service Layer Pattern (Сервисный Слой):** _Rationale:_ Бизнес-логика организована в отдельных сервисах (`EmotionService`, `PointsService` и т.д.), что обеспечивает чистоту кода, его переиспользуемость и тестируемость.

## Component View

### Существующие Компоненты

*   **`DIContainer`:** (Core) Собственный контейнер для внедрения зависимостей, управляющий жизненным циклом и связями компонентов.
*   **`PanelManager`:** (UI) Система для управления UI-панелями и их переходами.
*   **`SceneManagementSystem`:** (Core) Кастомная система управления сценами с Bootstrap-процессом для инициализации приложения.
*   **`SecurePlayerPrefs`:** (Data) Компонент для зашифрованного хранения локальных данных.
*   **`EmotionService`:** (Business Logic) Основная логика приложения, связанная с эмоциями, их типами, смешиванием и хранением. Интегрирован с Firebase.
*   **`PointsService`:** (Business Logic) Управляет начислением очков за действия пользователя.
*   **`LevelSystem`:** (Business Logic) Отслеживает прогресс пользователя и управляет уровнями.
*   **`AchievementService`:** (Business Logic) Управляет системой достижений (хотя в MVP это вне Scope).
*   **`FirebaseServiceFacade`:** (Data Access / Integration) Фасад для взаимодействия с различными сервисами Firebase (Auth, Database, Analytics).
*   **`EmotionHistoryCache`:** (Data Access) Кэширование истории эмоций для оптимизации производительности.
*   **`EmotionConfigService`:** (Configuration) Загрузка конфигураций эмоций через Addressables.
*   **`MyLogger`:** (Utilities) Собственная система логирования для диагностики.

### Новые Компоненты для "MoodRoom"

Эти компоненты будут интегрированы в существующую архитектуру, взаимодействуя с существующими сервисами и используя DI.

*   **`VisualOrchestrator`:** (Core / Visual Management)
    *   **Ответственность:** Центральный компонент, который слушает события от `EmotionService` (например, `OnEmotionLogged`) и управляет состоянием визуальных систем "MoodRoom" (освещение, погода, персонажи в банках).
    *   **Взаимодействие:** Подписывается на `EmotionService`. Отдает команды `LightingSystem`, `WeatherSystem`, `TimeSystem` и `EmotionJarController` (для персонифицированных эмоций).
*   **`LiquidSystem`:** (Visual Rendering)
    *   **Ответственность:** Набор компонентов и ресурсов для создания реалистичной жидкости в банках с эмоциями, включая физику и эффекты пузырьков.
    *   **Компоненты:**
        *   **Шейдер Жидкости:** Специализированный шейдер для визуализации жидкостей с динамическими свойствами (вязкость, цвет).
        *   **Система Пузырьков:** Набор Particle Systems, настроенных для каждой эмоции, с уникальным поведением пузырьков.
*   **`EmotionJarController`:** (UI / Interaction / Visual)
    *   **Ответственность:** Компонент, прикрепленный к каждому префабу банки с эмоцией. Управляет:
        *   Анимацией персонажа внутри банки.
        *   Интерактивностью при нажатии (визуальная обратная связь).
        *   Свойствами шейдера жидкости и активацией системы пузырьков в зависимости от состояния.
    *   **Взаимодействие:** Получает события от `InputSystem` (для обработки нажатий), отправляет события в `VisualOrchestrator` или напрямую в `EmotionService` (для логирования).
*   **`WeatherSystem`:** (Visual Rendering / Environment)
    *   **Ответственность:** Управляет визуальными эффектами погоды за окном "MoodRoom" (дождь, солнце, туман, ветер).
    *   **Взаимодействие:** Получает команды от `VisualOrchestrator`.
*   **`LightingSystem`:** (Visual Rendering / Environment)
    *   **Ответственность:** Управляет глобальным освещением в "MoodRoom", его цветом и интенсивностью для создания соответствующей атмосферы.
    *   **Взаимодействие:** Получает команды от `VisualOrchestrator`.
*   **`TimeSystem`:** (Visual Rendering / Environment)
    *   **Ответственность:** Управляет визуальной сменой времени суток за окном "MoodRoom" (день, сумерки, ночь).
    *   **Взаимодействие:** Получает команды от `VisualOrchestrator`.

```mermaid
graph TD
    A[EmotionService] -- "OnEmotionLogged" --> B[VisualOrchestrator];
    B --> C[LightingSystem];
    B --> D[WeatherSystem];
    B --> E[TimeSystem];
    B --> F[EmotionJarController];
    F -- "OnJarSelected" --> G[InputSystem];
    G --> H[UI/InteractionManager];
    H --> A; %% Log emotion
    F --> I[LiquidSystem]; %% Update liquid appearance
```

## Project Structure

```plaintext
{project-root}/
├── Assets/
│   ├── _Project/                 # Основная структура проекта
│   │   ├── Core/                 # Базовые системы (DI, SceneManagement, Logging)
│   │   ├── Services/             # Бизнес-логика (EmotionService, PointsService и т.д.)
│   │   ├── Data/                 # ScriptableObjects, конфигурации (EmotionConfigService)
│   │   ├── UI/                   # UI Components, PanelManager, новые MoodRoom UI
│   │   ├── Gameplay/             # Специфическая игровая логика (если будет)
│   │   ├── Visuals/              # Новые системы для MoodRoom (VisualOrchestrator, LiquidSystem, WeatherSystem, LightingSystem, TimeSystem, EmotionJarController)
│   │   │   ├── Shaders/
│   │   │   ├── Materials/
│   │   │   ├── Textures/
│   │   │   ├── Prefabs/ (банки, эффекты)
│   │   │   └── Scripts/ (VisualOrchestrator, LiquidSystem, WeatherSystem, LightingSystem, TimeSystem, EmotionJarController)
│   │   ├── ThirdParty/           # Интеграции с SDK (Firebase, Social)
│   │   ├── Scenes/               # Сцены (Auth, MoodRoom, History)
│   │   ├── Resources/            # (Используется с осторожностью, предпочтительно Addressables)
│   │   └── Editor/               # Скрипты для редактора
│   ├── AddressableAssets/        # Управляемые ассеты
│   └── Plugins/                  # Внешние библиотеки (Firebase SDK, TextMeshPro)
├── ProjectDocumentation/
│   ├── Development/
│   │   ├── Architecture/         # Этот документ (Architecture.md)
│   │   ├── TaskManagement/       # Документы задач (TASK-XXX.md)
│   │   └── ...
│   ├── Product/
│   │   └── PRD.md                # Product Requirements Document
│   │   └── UI_UX_Specification.md# UI/UX Specification
├── .cursor/
│   └── memory-bank/              # Входные данные, контекст (projectbrief.md, techContext.md, productContext.md)
├── UnityPackageManager/          # Файлы Unity Package Manager
├── Temp/                         # Временные файлы
├── Library/                      # Временные файлы Unity
└── ... (прочие файлы Unity)
```

### Key Directory Descriptions

*   `Assets/_Project/Core/`: Содержит базовые системы, такие как `DIContainer`, `SceneManagementSystem`, `MyLogger`.
*   `Assets/_Project/Services/`: Включает основную бизнес-логику приложения, такую как `EmotionService`, `PointsService`, `LevelSystem`.
*   `Assets/_Project/Data/`: Хранит ScriptableObjects, конфигурационные файлы (например, для эмоций), загружаемые через `Addressables`.
*   `Assets/_Project/UI/`: Содержит менеджер UI-панелей (`PanelManager`), а также все UI-компоненты, специфичные для "MoodRoom" (например, кнопки-таблички, поля ввода-бирки).
*   `Assets/_Project/Visuals/`: **Новая ключевая папка** для всех систем, связанных с визуализацией "MoodRoom". Здесь будут находиться скрипты `VisualOrchestrator`, `LiquidSystem`, `WeatherSystem`, `LightingSystem`, `TimeSystem`, `EmotionJarController`, а также связанные с ними шейдеры, материалы, текстуры и префабы (банки, эффекты частиц).
*   `Assets/_Project/ThirdParty/`: Для интеграций с внешними SDK, такими как Firebase SDK.
*   `Assets/_Project/Scenes/`: Все игровые сцены, включая экран авторизации и основную сцену "MoodRoom".
*   `ProjectDocumentation/Development/Architecture/`: Местоположение данного документа.

## API Reference

### External APIs Consumed

#### Firebase API

*   **Назначение:** Предоставление бэкенд-сервисов для аутентификации пользователей, хранения данных в реальном времени, аналитики, отчетов о сбоях, push-уведомлений и удаленной конфигурации.
*   **Базовый URL(ы):** Не применимо напрямую, взаимодействие через Firebase SDK.
*   **Аутентификация:** `FirebaseAuth` с поддержкой Google Sign-In. Доступ к данным в `FirebaseDatabase` управляется правилами безопасности Firebase, привязанными к аутентифицированным пользователям.
*   **Ключевые используемые сервисы/возможности:**
    *   **Firebase Authentication:** Регистрация, вход, управление сессиями пользователей.
    *   **Firebase Realtime Database:** Хранение и синхронизация пользовательских данных (профили, история настроения, очки, уровни). Используются оптимизированные запросы и пакетные операции.
    *   **Firebase Analytics:** Сбор аналитических данных о поведении пользователей.
    *   **Firebase Crashlytics:** Отчеты о сбоях для мониторинга стабильности.
    *   **Firebase Remote Config:** Динамическая конфигурация приложения.
*   **Ограничения скорости:** Управляется Firebase автоматически; необходимо оптимизировать запросы и подписки.
*   **Ссылка на официальную документацию:** [Firebase Documentation](https://firebase.google.com/docs/)

## Data Models

### Core Application Entities / Domain Objects

#### MoodEntry

*   **Описание:** Представляет собой запись о настроении пользователя в определенный момент времени.
*   **Схема / Определение интерфейса:**

    ```csharp
    public class MoodEntry
    {
        public string Id { get; set; } // Уникальный идентификатор записи (генерируется)
        public string UserId { get; set; } // ID пользователя
        public string EmotionType { get; set; } // Тип эмоции (например, "Joy", "Sadness")
        public DateTime Timestamp { get; set; } // Временная метка записи
        // public string MixedEmotionType { get; set; } // Если реализовано смешивание эмоций
        // public string CustomColorHex { get; set; } // Если реализован кастомный цвет
    }
    ```

#### User

*   **Описание:** Представляет профиль пользователя в приложении.
*   **Схема / Определение интерфейса:**

    ```csharp
    public class UserProfile
    {
        public string Id { get; set; } // ID пользователя (Firebase Auth UID)
        public string DisplayName { get; set; } // Отображаемое имя пользователя
        public string Email { get; set; } // Email пользователя (Firebase Auth)
        public int Points { get; set; } // Общее количество набранных очков
        public int Level { get; set; } // Текущий уровень пользователя
        // public List<string> FriendIds { get; set; } // Для социальных функций
        // public List<string> Achievements { get; set; } // Для достижений
    }
    ```

#### Emotion

*   **Описание:** Конфигурация для каждого типа эмоции (цвет, свойства персонажа, правила смешивания). Загружается через `Addressables`.
*   **Схема / Определение интерфейса:**

    ```csharp
    [CreateAssetMenu(fileName = "EmotionConfig", menuName = "Game/Emotion Config")]
    public class EmotionConfig : ScriptableObject
    {
        public string Id; // Уникальный ID эмоции (Joy, Sadness и т.д.)
        public string DisplayName; // Отображаемое название
        public Color BaseColor; // Базовый цвет для визуала
        public GameObject JarPrefab; // Ссылка на префаб банки с этой эмоцией
        public EmotionProperties Properties; // Свойства для LiquidSystem, BubbleSystem, персонажа
    }

    [System.Serializable]
    public class EmotionProperties
    {
        public float LiquidViscosity; // Вязкость жидкости
        public float BubbleSpeed; // Скорость пузырьков
        // Другие параметры для персонажа (скорость анимации, мимика и т.д.)
    }
    ```

#### Points & Level

*   **Описание:** Данные, связанные с прогрессией пользователя в геймификации.
*   **Схема / Определение интерфейса:**

    ```csharp
    public class PointsData
    {
        public int CurrentPoints { get; set; }
        public Dictionary<string, int> PointSources { get; set; } // Например, "MoodLog": 10
    }

    public class LevelData
    {
        public int CurrentLevel { get; set; }
        public int PointsToNextLevel { get; set; }
        public Dictionary<int, int> LevelThresholds { get; set; } // Уровень -> Очки
    }
    ```

## Coding Standards

Проект следует строгим стандартам кодирования C# в Unity, как это было оговорено в начале нашей работы:

*   **Именование:**
    *   `PascalCase` для публичных членов, классов, методов, свойств, констант.
    *   `_camelCase` для приватных полей (с нижним подчеркиванием).
    *   `camelCase` для локальных переменных и аргументов методов.
*   **Структура класса:** `Fields -> Constructor -> Properties -> Methods`. Внутри блоков: `public -> protected -> private`.
*   **Модификаторы доступа:** Всегда указываются явно.
*   **Отступы и форматирование:** Единообразное форматирование с пустыми строками между блоками кода.
*   **Комментирование:** Комментарии используются там, где логика не очевидна. XML-документация для публичных API.
*   **Принципы SOLID:** Применяются для обеспечения модульности и поддерживаемости.
*   **Избегание "магических чисел":** Использование именованных констант или полей.
*   **Оптимизация:** Оптимизация циклов `Update()`, использование пулинга объектов, `TryGetComponent()`, избегание `GameObject.Find()`.

## Testing Strategy

*   **Юнит-тесты:**
    *   **Расположение:** Юнит-тесты для бизнес-логики будут находиться в отдельной папке `Tests/EditMode/` или `Tests/PlayMode/` в Unity, зеркально отражая структуру папок `Assets/_Project/Services/` и `Assets/_Project/Core/`.
    *   **Соглашение об именовании:** Файлы тестов будут называться по формату `[ComponentName]Tests.cs` или `[ComponentName]Spec.cs`.
    *   **Фреймворк:** NUnit.
*   **Визуальное тестирование:**
    *   Ручное тестирование на различных целевых устройствах для проверки корректности рендеринга шейдеров, систем частиц и анимаций "MoodRoom".
    *   Автоматизированные тесты для визуальных компонентов могут быть реализованы с использованием Unity Test Runner в Play Mode для проверки состояний и переходов.
*   **Производительность:**
    *   Регулярное профилирование в Unity Editor и на целевых устройствах для мониторинга FPS, использования памяти и энергопотребления.
    *   Тестирование производительности ключевых визуальных эффектов (шейдеры жидкости, системы частиц) в экстремальных условиях.
*   **Интеграционное тестирование:**
    *   Ручное тестирование взаимодействия с Firebase (аутентификация, сохранение/чтение данных).
    *   Будет рассмотрена возможность использования автоматизированных интеграционных тестов для Firebase в будущем.

## Technical Limitations

*   **Офлайн-функциональность:** Базовое логирование настроения будет поддерживаться через локальное хранение с последующей синхронизацией. Полная функциональность, зависящая от Firebase (например, социальные функции), будет недоступна в офлайн-режиме.
*   **Безопасность данных:** Хотя используются `SecurePlayerPrefs` и правила безопасности Firebase, 100% гарантии защиты от сложных атак на мобильных устройствах нет.

## Performance Considerations

*   **Целевая частота кадров:** 60 кадров/с.
*   **Оптимизация рендеринга:** Использование Universal Render Pipeline (URP), оптимизация шейдеров для мобильных устройств, атласирование текстур UI-элементов.
*   **Системы частиц:** Оптимизация количества частиц и их логики для минимизации нагрузки на CPU/GPU.
*   **Кэширование:** Продолжать использовать `EmotionHistoryCache` и другие механизмы кэширования для работы с данными.
*   **Пакетные операции:** Для взаимодействия с Firebase (Firebase Batching).
*   **Использование памяти:** Мониторинг потребления памяти, особенно для ассетов и визуальных эффектов.

## Deployment Considerations

*   **CI/CD:** Автоматизированные сборки (APK/IPA) через Unity Cloud Build или GitHub Actions.
*   **Процесс релиза:** Внутреннее тестирование (QA), затем поэтапный выпуск в App Store и Google Play.
*   **Версионирование:** Семантическое версионирование (MAJOR.MINOR.PATCH).

## Local Development & Testing Requirements

*   **Среда разработки:** Unity Editor 2022.3.62f1 LTS, JetBrains Rider 2025.1.
*   **Версионный контроль:** Git с репозиторием GitHub.
*   **Тестирование из командной строки:** Запуск юнит-тестов из командной строки через Unity Test Runner.
*   **Отладка:** Использование детальной системы логирования `MyLogger` с категориями для диагностики проблем в Unity Editor и на устройствах.

## Change Log

| Change | Date | Version | Description | Author |
| ------ | ---- | ------- | ----------- | ------ |
| Инициализация | 2024-05-27 | 1.0 | Создан документ по архитектуре MoodColor на основе PRD, UI/UX спецификации и `techContext.md`. | Architect |
| Добавление MoodRoom | 2024-05-27 | 1.1 | Обновлена архитектура для поддержки концепции "MoodRoom" и связанных визуальных систем. | Architect |
