using System.Diagnostics;
using System.Runtime.InteropServices;

// ReSharper disable UnusedMethodReturnValue.Local

namespace NugetManager;

internal static partial class Program
{
    private const string MutexName = "Global\\NugetManager_SingleInstance_Mutex";

    // Win32 API functions for window manipulation
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // Create a mutex to ensure only one instance runs
        using var mutex = new Mutex(true, MutexName, out var createdNew);

        if (!createdNew)
        {
            // Another instance is already running, try to bring it to foreground
            BringExistingInstanceToForeground();
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    /// <summary>
    /// Try to bring the existing instance window to foreground
    /// </summary>
    private static void BringExistingInstanceToForeground()
    {
        try
        {
            // Find the existing NugetManager process
            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(currentProcess.ProcessName);

            foreach (var process in processes)
            {
                // Skip the current process
                if (process.Id == currentProcess.Id)
                    continue;

                // Try to bring the window to foreground
                var mainWindowHandle = process.MainWindowHandle;
                if (mainWindowHandle == IntPtr.Zero) continue;
                // If the window is minimized, restore it
                if (IsIconic(mainWindowHandle))
                {
                    ShowWindow(mainWindowHandle, SW_RESTORE);
                }

                // Bring the window to foreground
                SetForegroundWindow(mainWindowHandle);
                return;
            }

            // If we couldn't find a window to bring to foreground, show a message
            MessageBox.Show(
                "NugetManager is already running. Please check the system tray or taskbar.",
                "Application Already Running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            // If anything goes wrong, just show the basic message
            MessageBox.Show(
                $"NugetManager is already running. Only one instance is allowed.\n\nError details: {ex.Message}",
                "Application Already Running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}