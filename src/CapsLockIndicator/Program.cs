namespace CapsLockIndicator;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Allow only one instance per user session.
        using var mutex = new Mutex(true, @"Local\CapsLockIndicator_SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        using var context = new TrayApplicationContext();
        Application.Run(context);
    }
}
