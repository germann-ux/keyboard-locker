using KeyboardLocker.recursos;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;

namespace KeyboardLocker;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private static string AppName => Strings.AppName;
    private const string TrayIconResourceName = "KeyboardLocker.recursos.KeyBoardLocker_Logo.ico";

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly ToolStripMenuItem _toggleMenuItem;
    private readonly System.Windows.Forms.Timer _leftClickTimer;
    private readonly KeyboardBlocker _keyboardBlocker;
    private readonly Icon _trayIcon;
    private bool _keyboardLocked;
    private bool _disposed;
    private readonly ToolStripMenuItem _languageMenuItem;
    private readonly ToolStripMenuItem _spanishMenuItem;
    private readonly ToolStripMenuItem _englishMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;

    public TrayApplicationContext()
    {
        _trayIcon = LoadTrayIcon();

        _statusMenuItem = new ToolStripMenuItem
        {
            Enabled = false
        };

        _toggleMenuItem = new ToolStripMenuItem();
        _toggleMenuItem.Click += (_, _) => ToggleKeyboardLock();

        _menu = new ContextMenuStrip();
        _menu.Items.Add(_statusMenuItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_toggleMenuItem);
        _menu.Items.Add(new ToolStripSeparator());

        _languageMenuItem = new ToolStripMenuItem(Strings.LanguageMenu);

        _spanishMenuItem = new ToolStripMenuItem("Español");
        _spanishMenuItem.Click += (_, _) => ChangeLanguage("es-MX");

        _englishMenuItem = new ToolStripMenuItem("English");
        _englishMenuItem.Click += (_, _) => ChangeLanguage("en-US");

        _languageMenuItem.DropDownItems.Add(_spanishMenuItem);
        _languageMenuItem.DropDownItems.Add(_englishMenuItem);

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_languageMenuItem);
        _menu.Items.Add(new ToolStripSeparator());
        // traducciones. 
        //_menu.Items.Add("Salir", null, (_, _) => ExitApplication());
        _exitMenuItem = new ToolStripMenuItem(Strings.MenuExit);
        _exitMenuItem.Click += (_, _) => ExitApplication();
        _menu.Items.Add(_exitMenuItem);

        _leftClickTimer = new System.Windows.Forms.Timer
        {
            Interval = SystemInformation.DoubleClickTime
        };
        _leftClickTimer.Tick += LeftClickTimer_Tick;

        _notifyIcon = new NotifyIcon
        {
            Icon = _trayIcon,
            Text = AppName,
            Visible = true,
            ContextMenuStrip = _menu
        };
        _notifyIcon.MouseUp += NotifyIcon_MouseUp;
        _notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;

        _keyboardBlocker = new KeyboardBlocker();

        SetKeyboardLock(true, showNotification: true);
    }

    private void NotifyIcon_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _leftClickTimer.Stop();
        _leftClickTimer.Start();
    }

    private void NotifyIcon_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _leftClickTimer.Stop();
        ToggleKeyboardLock();
    }

    private void LeftClickTimer_Tick(object? sender, EventArgs e)
    {
        _leftClickTimer.Stop();
        _menu.Show(Cursor.Position);
    }

    private void ToggleKeyboardLock()
    {
        SetKeyboardLock(!_keyboardLocked, showNotification: true);
    }

    private void SetKeyboardLock(bool locked, bool showNotification)
    {
        _keyboardLocked = locked;
        _keyboardBlocker.Enabled = locked;

        //_statusMenuItem.Text = locked ? "Estado: teclado bloqueado" : "Estado: teclado desbloqueado";
        _statusMenuItem.Text = locked ? Strings.StatusLocked : Strings.StatusUnlocked;
        //_toggleMenuItem.Text = locked ? "Desbloquear teclado" : "Bloquear teclado";
        _toggleMenuItem.Text = locked ? Strings.ActionUnlock : Strings.ActionLock;
        //_notifyIcon.Text = locked ? $"{AppName} - bloqueado" : $"{AppName} - desbloqueado";
        _notifyIcon.Text = locked
            ? $"{AppName} - {Strings.TrayLocked}"
            : $"{AppName} - {Strings.TrayUnlocked}";

        if (!showNotification)
        {
            return;
        }

        //var title = locked ? "Teclado bloqueado" : "Teclado desbloqueado";
        var title = locked ? Strings.BalloonLockedTitle : Strings.BalloonUnlockedTitle;
        //var message = locked
        //    ? "El teclado quedo desactivado. Usa el icono de la bandeja para reactivarlo."
        //    : "El teclado volvio a estar disponible.";
        var message = locked
            ? Strings.BalloonLockedMessage
            : Strings.BalloonUnlockedMessage;

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = locked ? ToolTipIcon.Warning : ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(1500);
    }

    private void ExitApplication()
    {
        SetKeyboardLock(false, showNotification: false);
        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        DisposeResources();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeResources();
        }

        base.Dispose(disposing);
    }

    private void DisposeResources()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _leftClickTimer.Stop();
        _notifyIcon.Visible = false;
        _keyboardBlocker.Dispose();
        _trayIcon.Dispose();
        _leftClickTimer.Dispose();
        _menu.Dispose();
        _notifyIcon.Dispose();
    }

    private static Icon LoadTrayIcon()
    {
        using var stream = typeof(TrayApplicationContext).Assembly.GetManifestResourceStream(TrayIconResourceName);
        if (stream is null)
        {
            return (Icon)SystemIcons.Shield.Clone();
        }

        return new Icon(stream);
    }
    private void ChangeLanguage(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);

        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;

        Settings.Default.LanguageCode = cultureCode;
        Settings.Default.Save();

        RefreshTexts();
    }

    private void RefreshTexts()
    {
        _languageMenuItem.Text = Strings.LanguageMenu;
        _spanishMenuItem.Checked = CultureInfo.CurrentUICulture.Name.StartsWith("es");
        _englishMenuItem.Checked = CultureInfo.CurrentUICulture.Name.StartsWith("en");

        _statusMenuItem.Text = _keyboardLocked ? Strings.StatusLocked : Strings.StatusUnlocked;
        _toggleMenuItem.Text = _keyboardLocked ? Strings.ActionUnlock : Strings.ActionLock;

        _exitMenuItem.Text = Strings.MenuExit;

        _notifyIcon.Text = _keyboardLocked
            ? $"{Strings.AppName} - {Strings.TrayLocked}"
            : $"{Strings.AppName} - {Strings.TrayUnlocked}";
    }

}

internal sealed class KeyboardBlocker : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int HcAction = 0;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

    private readonly LowLevelKeyboardProc _hookCallback;
    private nint _hookHandle;
    private bool _disposed;

    public KeyboardBlocker()
    {
        _hookCallback = HookCallback;
        InstallHook();
    }

    public bool Enabled { get; set; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    private void InstallHook()
    {
        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;
        var moduleName = currentModule?.ModuleName ?? throw new Win32Exception(Strings.ErrorMainModule);
        var moduleHandle = GetModuleHandle(moduleName);

        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _hookCallback, moduleHandle, 0);

        if (_hookHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), Strings.ErrorInstallHook);
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= HcAction && Enabled)
        {
            var message = wParam.ToInt32();
            if (message is WmKeyDown or WmKeyUp or WmSysKeyDown or WmSysKeyUp)
            {
                return 1;
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern nint GetModuleHandle(string lpModuleName);
}
