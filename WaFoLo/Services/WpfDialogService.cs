using System.Windows;

namespace WaFoLo.Services
{
    /// <summary>
    /// WPF production implementation of <see cref="IDialogService"/> that
    /// delegates to <see cref="MessageBox"/>.
    /// </summary>
    internal sealed class WpfDialogService : IDialogService
    {
        public void ShowWarning(string message, string title) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        public void ShowError(string message, string title) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        public void ShowInfo(string message, string title) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
