namespace WaFoLo.Services
{
    /// <summary>
    /// Abstraction over modal dialogs (e.g. <c>MessageBox.Show</c>), allowing
    /// unit tests to verify dialog calls without displaying any UI.
    /// </summary>
    public interface IDialogService
    {
        void ShowWarning(string message, string title);
        void ShowError(string message, string title);
        void ShowInfo(string message, string title);
    }
}
