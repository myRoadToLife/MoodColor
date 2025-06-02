using System;

namespace App.Develop.Utils.Events
{
    /// <summary>
    /// Типизированные события для личного кабинета
    /// Заменяют простые Action на более информативные EventHandler
    /// </summary>
    public static class PersonalAreaEvents
    {
        /// <summary>
        /// Аргументы события навигации
        /// </summary>
        public class NavigationEventArgs : EventArgs
        {
            public string PanelName { get; }
            public DateTime Timestamp { get; }

            public NavigationEventArgs(string panelName)
            {
                PanelName = panelName;
                Timestamp = DateTime.Now;
            }
        }

        /// <summary>
        /// Аргументы события выхода из приложения
        /// </summary>
        public class QuitRequestEventArgs : EventArgs
        {
            public bool RequiresConfirmation { get; }
            public DateTime Timestamp { get; }

            public QuitRequestEventArgs(bool requiresConfirmation = true)
            {
                RequiresConfirmation = requiresConfirmation;
                Timestamp = DateTime.Now;
            }
        }

        /// <summary>
        /// Аргументы события изменения профиля
        /// </summary>
        public class ProfileChangedEventArgs : EventArgs
        {
            public string Username { get; }
            public DateTime Timestamp { get; }

            public ProfileChangedEventArgs(string username)
            {
                Username = username;
                Timestamp = DateTime.Now;
            }
        }

        /// <summary>
        /// Аргументы события изменения статистики
        /// </summary>
        public class StatisticsChangedEventArgs : EventArgs
        {
            public int Points { get; }
            public int Entries { get; }
            public DateTime Timestamp { get; }

            public StatisticsChangedEventArgs(int points, int entries)
            {
                Points = points;
                Entries = entries;
                Timestamp = DateTime.Now;
            }
        }
    }
} 