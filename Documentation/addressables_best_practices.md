# Правила работы с Unity Addressables

## Основные принципы

*   **Назначение Addressables**: Используйте Addressables для управления загрузкой контента, уменьшения размера билда, управления памятью (своевременная загрузка и выгрузка ассетов) и для возможности обновления контента без необходимости пересборки всего игрового клиента.
*   **Маркировка ассетов**: Чтобы сделать ассет "адресуемым", выделите его в Project окне, поставьте галочку "Addressable" в Inspector и присвойте ему уникальное, понятное имя (ключ/адрес). По этому ключу ассет будет доступен для загрузки.
*   **Инициализация**: Перед первым обращением к Addressables (загрузкой ассетов) необходимо выполнить инициализацию системы: `Addressables.InitializeAsync()`. Рекомендуется отслеживать завершение этой асинхронной операции.

## Группы и Бандлы

*   **Локальные группы**: Ассеты из локальных групп включаются непосредственно в билд игры. Используйте для критически важных ассетов, необходимых для запуска и первоначальной работы игры (например, экран загрузки, основные UI элементы).
*   **Удаленные группы**: Ассеты из удаленных групп загружаются из указанного URL (например, CDN). Это позволяет обновлять контент без выпуска новой версии игры. Настройте пути для билда (`Build Path`) и загрузки (`Load Path`) на удаленный сервер. Снимите галочку "Include in Build" для таких групп.
*   **Режимы упаковки (Build & Load Paths -> Advanced Options -> Bundle Mode)**:
    *   `Pack Together`: Все ассеты из группы упаковываются в один бандл. Подходит для наборов ассетов, которые всегда используются вместе.
    *   `Pack Separately`: Каждый ассет из группы упаковывается в отдельный бандл. Полезно, если ассеты из группы часто используются по отдельности.
    *   `Pack Together by Label`: Ассеты с одинаковыми метками (Labels) внутри группы упаковываются вместе.
*   **Управление зависимостями и дубликатами**:
    *   **Проблема**: Если разные бандлы используют одни и те же общие ассеты (например, шрифты, шейдеры, общие текстуры), эти ассеты могут быть включены в каждый из этих бандлов, что приводит к увеличению общего размера и потребления памяти.
    *   **Решения**:
        1.  **Объединение в одну группу**: Если несколько префабов или ассетов часто используются вместе и имеют много общих зависимостей, поместите их в одну группу с режимом упаковки `Pack Together`.
        2.  **Выделение общих ассетов**: Создайте отдельную группу для всех общих ассетов. Другие группы будут ссылаться на эту группу зависимостей.
        3.  **Инструмент "Analyze"**: Используйте `Window -> Asset Management -> Addressables -> Analyze`. Запустите правило `Check Duplicate Bundle Dependencies`. Инструмент может автоматически создать группу для общих зависимостей и исправить дубликаты. Настройте эту группу (например, режим упаковки `Pack Together`).

## Загрузка и выгрузка ассетов

*   **Загрузка префабов (инстанцирование)**:
    *   `Addressables.InstantiateAsync("ключ_ассета", parentTransform, instantiateInWorldSpace)`
    *   Возвращает `AsyncOperationHandle<GameObject>`. Результатом операции будет инстанцированный `GameObject`.
*   **Загрузка ассетов (без инстанцирования)**:
    *   `Addressables.LoadAssetAsync<T>("ключ_ассета")` (где `T` - тип ассета, например, `Texture2D`, `Material`, `AudioClip`).
    *   Возвращает `AsyncOperationHandle<T>`. Результатом будет загруженный ассет.
*   **Загрузка сцен**:
    *   `Addressables.LoadSceneAsync("ключ_сцены", LoadSceneMode.Additive)` или `LoadSceneMode.Single`.
    *   Возвращает `AsyncOperationHandle<SceneInstance>`.
*   **Выгрузка ассетов**:
    *   Для объектов, созданных через `InstantiateAsync`: `Addressables.ReleaseInstance(gameObjectInstance)` или `Addressables.ReleaseInstance(asyncOperationHandle)`.
    *   Для ассетов/сцен, загруженных через `LoadAssetAsync`/`LoadSceneAsync`: `Addressables.Release(asset)` или `Addressables.Release(asyncOperationHandle)`.
    *   **Важно**: Память освобождается не мгновенно. Addressables используют систему подсчета ссылок; ассет выгружается, когда счетчик ссылок на него достигает нуля.
*   **Асинхронность**: Все операции загрузки и выгрузки асинхронны. Используйте `await`, колбэки (`Completed` событие `AsyncOperationHandle`) или корутины для работы с результатами.

## Работа в редакторе (Play Mode Script)

*   Находится в `Window -> Asset Management -> Addressables -> Settings -> Play Mode Script`.
*   **`Asset Database`**: Наиболее быстрый режим. Загружает ассеты напрямую из папки `Assets` без симуляции бандлов. Не отражает реальную производительность или проблемы с зависимостями.
*   **`Simulate Groups`**: Симулирует загрузку из групп, как они настроены (локальные/удаленные). Не требует сборки бандлов, но настройки групп (пути, режимы упаковки) должны быть корректны. Может выявить проблемы с конфигурацией.
*   **`Use Existing Build`**: Наиболее точная симуляция. Использует предварительно собранные Addressables бандлы. Требует запуска сборки Addressables (`Window -> Asset Management -> Addressables -> Groups -> Build -> New Build -> Default Build Script`).

## Сборка Addressables

*   Для создания билда игры или для работы в режиме `Use Existing Build` в редакторе, необходимо собрать Addressables бандлы.
*   `Window -> Asset Management -> Addressables -> Groups`.
*   Нажмите `Build -> New Build -> Default Build Script`.
*   Собранные бандлы для текущей платформы попадут в `Library/com.unity.addressables` (если пути локальные) или в указанные `Build Path` для удаленных групп.

## Профайлы (Profiles)

*   Позволяют настроить разные конфигурации (например, пути загрузки) для разных стадий разработки (Development, Staging, Production).
*   `Window -> Asset Management -> Addressables -> Settings -> Profile`.
*   Можно создавать новые профили и переключаться между ними. Переменные из активного профиля (например, `[UnityEngine.AddressableAssets.Addressables.RuntimePath]`) используются в путях загрузки групп.

## Структурирование кода

*   Рекомендуется создавать классы-обертки (провайдеры) для загрузки/выгрузки часто используемых или важных Addressable ассетов. Это помогает инкапсулировать ключи ассетов и логику работы с ними, делая код чище и проще в поддержке.
*   Пример:
    ```csharp
    public class AssetProvider<T> where T : UnityEngine.Object
    {
        private string _assetKey;
        private AsyncOperationHandle<T> _handle;
        private T _loadedAsset;

        public AssetProvider(string assetKey)
        {
            _assetKey = assetKey;
        }

        public async Task<T> LoadAssetAsync()
        {
            if (_loadedAsset != null) return _loadedAsset;
            if (!_handle.IsValid()) // Если еще не грузили или была ошибка
            {
                 _handle = Addressables.LoadAssetAsync<T>(_assetKey);
                 _loadedAsset = await _handle.Task;
                 // Добавить обработку ошибок, если _handle.Status == AsyncOperationStatus.Failed
            }
            else if (_handle.IsDone) // Уже загружено
            {
                _loadedAsset = _handle.Result;
            }
            else // Загрузка в процессе
            {
                _loadedAsset = await _handle.Task;
            }
            return _loadedAsset;
        }

        public void ReleaseAsset()
        {
            if (_loadedAsset != null)
            {
                // Для не-GameObject ассетов, если вы хотите управлять моментом освобождения
                // Addressables.Release(_loadedAsset); // или _handle
                // _loadedAsset = null;
            }
            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
                _loadedAsset = null; // Убедиться, что ссылка очищена
            }
        }
    }

    // Использование:
    // var_myMaterialProvider = new AssetProvider<Material>("MyAwesomeMaterial_Key");
    // Material awesomeMaterial = await _myMaterialProvider.LoadAssetAsync();
    // ... использовать материал ...
    // _myMaterialProvider.ReleaseAsset(); // Когда материал больше не нужен глобально
    ```

## Дополнительные советы

*   **Именование ключей**: Используйте консистентную и понятную систему именования ключей для Addressables.
*   **Очистка кеша**: При тестировании удаленной загрузки иногда полезно очищать кеш Addressables. Это можно сделать через скрипт, используя `Caching.ClearCache()`.
*   **Логирование и отладка**: Используйте `Addressables.ResourceManager.ExceptionHandler` для отлова и логирования ошибок загрузки. Окно "Event Viewer" (`Window -> Asset Management -> Addressables -> Event Viewer`) может помочь в отладке.
*   **Версионирование контента**: Для удаленного контента используйте систему контроля версий для каталога и бандлов, чтобы клиенты могли обновляться до нужной версии.
*   **Не выносите все подряд в Addressables**: Ассеты, которые критичны для самого первого запуска и невелики по размеру (например, логотип компании), могут оставаться в `Resources` или быть прямыми ссылками, если это упрощает логику.

Этот набор правил должен помочь в эффективной работе с системой Addressables в Unity. 