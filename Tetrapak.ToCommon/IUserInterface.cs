namespace Tetrapak.ToCommon
{
    public interface IUserInterface : ILog, IBusyControl
    {
        string Ask(string message, params string[] options);
        char ReadKey();
    }
}