namespace App.Develop.CommonServices.LoadingScreen
{
    public interface ILoadingScreen
    {
        bool IsShowing { get; }
        void Show();
        void Hide();
    }
}
