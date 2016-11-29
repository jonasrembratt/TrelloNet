namespace Tetrapak.ToCommon
{
    public interface IBusyControl
    {
        void ShowBusy(int value = 0, int maxValue = 100, string message = null);
        void UpdateBusy(int value, string message = null);
        void HideBusy();
    }
}