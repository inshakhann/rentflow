using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RentFlow.Client.Services
{
    public class ToastService
    {
        public event Action? OnToastsUpdated;
        public List<Toast> Toasts { get; private set; } = new();

        public void ShowSuccess(string message, string title = "Success") => ShowToast(message, title, "Success");
        public void ShowError(string message, string title = "Error") => ShowToast(message, title, "Error");
        public void ShowWarning(string message, string title = "Warning") => ShowToast(message, title, "Warning");
        public void ShowInfo(string message, string title = "Info") => ShowToast(message, title, "Info");

        private void ShowToast(string message, string title, string type)
        {
            var toast = new Toast { Message = message, Title = title, Type = type };
            Toasts.Add(toast);
            OnToastsUpdated?.Invoke();

            // Auto-dismiss after 5s
            Task.Delay(5000).ContinueWith(_ =>
            {
                if (Toasts.Contains(toast))
                {
                    RemoveToast(toast);
                }
            });
        }

        public void RemoveToast(Toast toast)
        {
            Toasts.Remove(toast);
            OnToastsUpdated?.Invoke();
        }
    }

    public class Toast
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info"; // Success, Error, Warning, Info
    }
}
