using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System.Diagnostics;
using System.IO;

namespace HospitalityPOS.WPF.Tests.UITests;

/// <summary>
/// Test fixture for launching and managing the HospitalityPOS application for UI tests.
/// </summary>
public class AppTestFixture : IDisposable
{
    private Application? _application;
    private UIA3Automation? _automation;
    private bool _disposed;

    /// <summary>
    /// Path to the application executable.
    /// </summary>
    public static string AppPath => Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "src", "HospitalityPOS.WPF", "bin", "Debug", "net10.0-windows", "HospitalityPOS.exe");

    /// <summary>
    /// The FlaUI automation instance.
    /// </summary>
    public UIA3Automation Automation => _automation ?? throw new InvalidOperationException("Automation not initialized. Call LaunchApp first.");

    /// <summary>
    /// The running application instance.
    /// </summary>
    public Application Application => _application ?? throw new InvalidOperationException("Application not launched. Call LaunchApp first.");

    /// <summary>
    /// The main window of the application.
    /// </summary>
    public Window MainWindow => Application.GetMainWindow(Automation) ?? throw new InvalidOperationException("Main window not found.");

    /// <summary>
    /// Launches the application for testing.
    /// </summary>
    /// <param name="timeout">Timeout for waiting for the main window to appear.</param>
    public void LaunchApp(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);

        var absolutePath = Path.GetFullPath(AppPath);
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException(
                $"Application executable not found at: {absolutePath}. Please build the application first.");
        }

        _automation = new UIA3Automation();
        _application = Application.Launch(absolutePath);

        // Wait for main window to be ready
        var mainWindow = Application.GetMainWindow(Automation, timeout.Value);
        if (mainWindow == null)
        {
            throw new TimeoutException("Main window did not appear within the specified timeout.");
        }

        // Give the app a moment to fully initialize
        Thread.Sleep(1000);
    }

    /// <summary>
    /// Attaches to an already running instance of the application.
    /// </summary>
    /// <param name="processName">Name of the process to attach to.</param>
    public void AttachToApp(string processName = "HospitalityPOS")
    {
        var processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0)
        {
            throw new InvalidOperationException($"No running process found with name: {processName}");
        }

        _automation = new UIA3Automation();
        _application = Application.Attach(processes[0]);
    }

    /// <summary>
    /// Finds an element by its automation ID within the main window.
    /// </summary>
    public AutomationElement? FindById(string automationId)
    {
        return MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
    }

    /// <summary>
    /// Finds an element by its name within the main window.
    /// </summary>
    public AutomationElement? FindByName(string name)
    {
        return MainWindow.FindFirstDescendant(cf => cf.ByName(name));
    }

    /// <summary>
    /// Finds an element by its text content.
    /// </summary>
    public AutomationElement? FindByText(string text)
    {
        return MainWindow.FindFirstDescendant(cf => cf.ByText(text));
    }

    /// <summary>
    /// Waits for an element to appear by automation ID.
    /// </summary>
    public AutomationElement? WaitForElement(string automationId, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(10);
        var endTime = DateTime.Now + timeout.Value;

        while (DateTime.Now < endTime)
        {
            var element = FindById(automationId);
            if (element != null)
                return element;

            Thread.Sleep(100);
        }

        return null;
    }

    /// <summary>
    /// Waits for an element by name to appear.
    /// </summary>
    public AutomationElement? WaitForElementByName(string name, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(10);
        var endTime = DateTime.Now + timeout.Value;

        while (DateTime.Now < endTime)
        {
            var element = FindByName(name);
            if (element != null)
                return element;

            Thread.Sleep(100);
        }

        return null;
    }

    /// <summary>
    /// Takes a screenshot of the current state.
    /// </summary>
    public void TakeScreenshot(string filePath)
    {
        var capture = FlaUI.Core.Capturing.Capture.Screen();
        capture.ToFile(filePath);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _application?.Close();
            _application?.Dispose();
            _automation?.Dispose();
        }

        _disposed = true;
    }
}
