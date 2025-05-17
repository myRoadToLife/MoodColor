# Список TODO-задач в проекте

## Панели UI
- **SettingsPanelGenerator.cs**: 
  - Добавить контроллер, если нужен для этой панели
  - Создать специфичный контент для SettingsPanel

- **WorkshopPanelGenerator.cs**:
  - Создать специфичный контент для WorkshopPanel

- **LogEmotionPanelGenerator.cs**:
  - Заполнить EmotionSelector реальными элементами

- **FriendsPanelGenerator.cs**:
  - Создать специфичный контент для FriendsPanel

- **HistoryPanelGenerator.cs**:
  - Создать специфичный контент для HistoryPanel

## Логика Personal Area
- **WorkshopPanelController.cs**:
  - Загрузка данных мастерской

- **PersonalAreaManager.cs**:
  - Подгрузить имя пользователя из профиля
  - Передать дефолтный Sprite для эмоции или загрузить из данных
  - Заменить заглушки реальными данными для очков и записей

- **LogEmotionPanelController.cs**:
  - Добавить логику сохранения эмоции

- **HistoryPanelController.cs**:
  - Отобразить сообщение "Ошибка загрузки истории"
  - Отобразить сообщение "История пуста"

- **FriendsPanelController.cs**:
  - Загрузка данных о друзьях
  - Логика добавления друга

## Firebase
- **EmotionSelectionManager.cs**:
  - Добавить RegionId и Location, если нужно
  - Показывать пользователю сообщение об ошибке
  - Заменить на получение цветов из конфигурации 