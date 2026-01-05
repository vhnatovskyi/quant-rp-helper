using Quant.Helper.Common;
using Quant.Helper.Scripts;
using Quant.Helper.Scripts.Abstractions;
using SharpHook;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Threading;
using WindowsInput;
using Dispatcher = Quant.Helper.Scripts.Dispatcher;

#nullable enable
namespace Quant.Helper;

public partial class MainWindow : Window, IComponentConnector
{
    private readonly List<IScript> _scripts;
    private readonly Task _dispatcherTask;
    private readonly Dispatcher _dispatcher;
    private readonly ILogger _logger;
    private readonly IGlobalHook _hook;
    private readonly InputSimulator _inputSimulator;
    private bool _isClickThrough = false;
    private bool _ctrlPressed = false;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;

    public MainWindow()
    {
        InitializeComponent();
        _logger = new Logger();

        _hook = new SimpleGlobalHook();
        _inputSimulator = new InputSimulator();
        var scriptList = new List<IScript>
        {
            new TreeChopScript(_inputSimulator),
            new ElectricScript(_logger, _inputSimulator),
            new MineScript(_logger, _inputSimulator),
            new SnowBallScript(_inputSimulator)
        };
        _scripts = scriptList;
        ScriptList.ItemsSource = _scripts;
        _dispatcher = new Dispatcher(_logger, _hook, _scripts);
        _dispatcher.ActiveScriptChanged += OnActiveScriptChanged;
        _dispatcher.OnScriptStopped += OnScriptStopped;
        _dispatcher.OnScriptRunning += OnScriptRunning;

        _hook.KeyPressed += (_, e) =>
        {
            if (e.Data.KeyCode == SharpHook.Data.KeyCode.VcLeftControl || e.Data.KeyCode == SharpHook.Data.KeyCode.VcRightControl)
                _ctrlPressed = true;

            if (_ctrlPressed && e.Data.KeyCode == SharpHook.Data.KeyCode.VcH)
            {
                Dispatcher.BeginInvoke(new Action(ToggleTransparency));
            }
        };

        _hook.KeyReleased += (_, e) =>
        {
            if (e.Data.KeyCode == SharpHook.Data.KeyCode.VcLeftControl || e.Data.KeyCode == SharpHook.Data.KeyCode.VcRightControl)
                _ctrlPressed = false;
        };

        LogList.ItemsSource = _logger.Messages;
        _logger.Messages.CollectionChanged += (_, __) =>
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var last = _logger.Messages.LastOrDefault();
                if (last != null) LogList.ScrollIntoView(last);
            }), DispatcherPriority.Background);
        _dispatcherTask = _dispatcher.RunAsync();
    }

    private async void Execute_Click(object sender, RoutedEventArgs e)
    {
        if (ScriptList.SelectedItem is IScript)
            await _dispatcher.ExecuteScriptAsync();
    }

    private void OnScriptStopped()
    {
        Dispatcher.Invoke(() =>
        {
            if (ScriptList.SelectedItem is IScript selectedItem)
                ExecuteButton.Content = "Запустити";
        });
    }

    private void OnScriptRunning()
    {
        Dispatcher.Invoke(() =>
        {
            if (ScriptList.SelectedItem is IScript selectedItem)
                ExecuteButton.Content = "Зупитини";
        });
    }

    private void OnActiveScriptChanged(IScript? script)
    {
        Dispatcher.Invoke(() =>
        {
            ScriptList.SelectedItem = script;
            ShowOrHideButton(script);
        });
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
            return;
        DragMove();
    }

    private void ScriptList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var script = ScriptList.SelectedItem as IScript;
        _dispatcher.SetActiveScript(script);
        ShowOrHideButton(script);
    }

    private void ShowOrHideButton(IScript? script)
    {
        ActiveScriptLabel.Text = script?.Name ?? "Відсутній";
        ActionPanel.Visibility = script != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ToggleTransparency()
    {
        _isClickThrough = !_isClickThrough;

        var hwnd = new WindowInteropHelper(this).Handle;

        if (_isClickThrough)
        {
            Opacity = 0.0;

            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);

            _logger.Log("Вікно стало прозорим і click-through (Ctrl+H для відновлення)");
        }
        else
        {
            Opacity = 1;

            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
            _logger.Log("Прозорість і click-through скасовано");
        }
    }

    private async void Exit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _dispatcher.StopScriptAsync();
            _dispatcherTask?.Dispose();
        }
        finally
        {
            Application.Current.Shutdown();
            Environment.Exit(0);
        }
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
}
