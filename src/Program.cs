namespace GeminiForChromeManager;

static class Program
{
    [STAThread]
    static void Main()
    {
        using Mutex singleInstance = new(true, "Ghostwheel.GeminiForChromeTaskManager", out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show(
                "Gemini for Chrome Task Manager is already running.",
                AppInfo.AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
