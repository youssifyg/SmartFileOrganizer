using System;

namespace SmartFileOrganizer.Core.Events
{
    public static class GlobalEvents
    {
        public static event Action OnHistoryCleared;
        public static void NotifyHistoryCleared() => OnHistoryCleared?.Invoke();

        public static Action OnLanguageChanged;
        public static void NotifyLanguageChanged() => OnLanguageChanged?.Invoke();
    }
}
