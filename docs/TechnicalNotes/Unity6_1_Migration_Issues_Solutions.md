# 🔧 Unity 6.1 Migration Issues & Solutions

## 📋 Обзор проблем

После обновления проекта MoodColor на Unity 6.1 выявлены следующие проблемы, требующие решения:

1. **UniversalRenderPipelineGlobalSettings** - отсутствующие типы рендеринга
2. **Render Graph API** - предупреждение о совместимости  
3. **EmotionJarView** - не назначен bubble prefab
4. **Firebase Database** - ошибки доступа

---

## 🚨 Проблема 1: URP Global Settings - Missing Types

### Симптомы
```
Missing types referenced from component UniversalRenderPipelineGlobalSettings:
UnityEngine.Rendering.RenderingDebuggerRuntimeResources, Unity.RenderPipelines.Core.Runtime (1 object)
```

### Причина
После обновления на Unity 6.1 с URP 17.2.0 некоторые ресурсы рендеринга не были корректно мигрированы.

### Решение

#### Шаг 1: Восстановить URP Global Settings
1. В Unity Editor перейти в **Edit > Project Settings > Graphics**
2. В разделе **Scriptable Render Pipeline Settings** найти **Universal Render Pipeline Global Settings**
3. Если поле пустое или показывает ошибки - создать новые настройки:

```
Assets > Create > Rendering > URP Global Settings (Renderer Data)
```

#### Шаг 2: Обновить зависимости URP
В Package Manager проверить установку:
- ✅ Universal RP: 17.2.0
- ✅ Core RP Library: 17.1.0  
- ✅ Shader Graph: актуальная версия

#### Шаг 3: Перезагрузить проект
После восстановления настроек:
1. Сохранить проект
2. Закрыть Unity Editor
3. Удалить папку `Library/`
4. Открыть проект заново

---

## ⚡ Проблема 2: Render Graph API Compatibility Mode

### Симптомы
```
The project currently uses the compatibility mode where the Render Graph API is disabled. 
Support for this mode will be removed in future Unity versions.
```

### Решение

#### Включить Render Graph API
1. **Edit > Project Settings > Graphics**
2. Найти секцию **Render Graph**
3. Установить **Enable Render Graph** в `true`

#### Обновить URP Global Settings файл
В файле `Assets/UniversalRenderPipelineGlobalSettings.asset` изменить:

```yaml
# Было:
m_EnableRenderGraph: 0

# Должно быть:
m_EnableRenderGraph: 1
```

#### Проверить совместимость
После включения Render Graph API проверить:
- ✅ Нет ошибок компиляции
- ✅ Рендеринг работает корректно
- ✅ Post-processing эффекты функционируют

---

## 🫧 Проблема 3: EmotionJarView - Missing Bubble Prefab

### Симптомы
```
[EmotionJarView] Bubble prefab is not assigned in the inspector
```

### Анализ кода
В `EmotionJarView.cs` строка 64 проверяет назначение `_bubblePrefab`:

```csharp
if (_bubblePrefab == null) 
{
    LogWarning("Bubble prefab is not assigned in the inspector");
}
```

### Решение

#### Вариант 1: Создать Bubble Prefab
1. Создать новый GameObject с названием "BubblePrefab"
2. Добавить компоненты:
   - `Image` - для визуального отображения пузыря
   - `RectTransform` - для позиционирования в UI
3. Настроить внешний вид пузыря
4. Сохранить как prefab в `Assets/App/Addressables/UI/Components/`

#### Вариант 2: Отключить функцию пузырей (временно)
В `EmotionJarView.cs` закомментировать проверку:

```csharp
// if (_bubblePrefab == null) 
// {
//     LogWarning("Bubble prefab is not assigned in the inspector");
// }
```

#### Вариант 3: Найти существующий prefab
Возможно, prefab уже существует. Проверить:
- `Assets/App/Addressables/UI/Components/`
- `Assets/App/Addressables/UI/Panels/`

#### Назначение prefab в Inspector
1. Выбрать объект с компонентом `EmotionJarView`
2. В Inspector найти поле **Bubble Prefab**
3. Перетащить созданный/найденный prefab в это поле

---

## 🔥 Проблема 4: Firebase Database - Permission Denied

### Симптомы
```
Listen at emotions failed: Permission denied
Listen at jars failed: Permission denied  
Listen at users failed: Permission denied
```

### Причины
1. **Неправильные правила безопасности** Firebase Database
2. **Неверная аутентификация** пользователя
3. **Истекшие токены** доступа
4. **Изменения в Firebase SDK** после обновления Unity

### Решение

#### Шаг 1: Проверить правила Firebase Database
В Firebase Console > Database > Rules проверить:

```json
{
  "rules": {
    "emotions": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "jars": {
      "$uid": {
        ".read": "$uid === auth.uid", 
        ".write": "$uid === auth.uid"
      }
    },
    "users": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    }
  }
}
```

#### Шаг 2: Проверить аутентификацию
В коде проверить статус аутентификации перед обращением к базе:

```csharp
if (FirebaseAuth.DefaultInstance.CurrentUser != null)
{
    // Пользователь аутентифицирован - можно обращаться к базе
}
else
{
    // Необходима аутентификация
}
```

#### Шаг 3: Обновить Firebase конфигурацию
1. Скачать новый `google-services.json` из Firebase Console
2. Заменить файл в `Assets/google-services.json`
3. Перезапустить Unity Editor

#### Шаг 4: Проверить версии Firebase SDK
В Package Manager убедиться в совместимости версий Firebase пакетов с Unity 6.1.

---

## ✅ Общий План Исправления

### Последовательность действий:

1. **Исправить URP Settings** (5-10 мин)
   - Создать новые URP Global Settings
   - Обновить зависимости

2. **Включить Render Graph** (2 мин)
   - Project Settings > Graphics > Enable Render Graph

3. **Решить проблему с Bubble Prefab** (10-15 мин)
   - Создать или найти prefab
   - Назначить в Inspector

4. **Настроить Firebase** (15-20 мин)
   - Проверить правила безопасности
   - Обновить конфигурацию
   - Проверить аутентификацию

### Проверка результата:
После выполнения всех шагов в Console должны исчезнуть все ошибки и предупреждения.

---

## 📚 Дополнительные Ресурсы

### Официальная документация:
- [Unity 6.0 Upgrade Guide](https://docs.unity3d.com/6000.0/Documentation/Manual/UpgradeGuideUnity6.html)
- [URP Documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html)
- [Render Graph API](https://docs.unity3d.com/6000.0/Documentation/Manual/render-graph.html)

### Связанные документы:
- `docs/TechnicalNotes/Unity6_1_TextMeshPro_Status.md`
- `docs/Architecture/ARCHITECTURE_DOCUMENTATION.md`

---

**Дата создания:** 6 января 2025  
**Автор:** Unity Developer  
**Статус:** Готово к исполнению 