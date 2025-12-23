using Quant.Helper.Common;
using Quant.Helper.Scripts;
using Quant.Helper.Scripts.Abstractions;
using SharpHook;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    public MainWindow()
    {
        InitializeComponent();
        _logger = new Logger();

        var globalHook = new SimpleGlobalHook();
        var inputSimulator = new InputSimulator();
        var scriptList = new List<IScript>
        {
            new TreeChopScript(inputSimulator),
            new ElectricScript(_logger, inputSimulator),
            new MineScript(_logger, inputSimulator)
        };
        _scripts = scriptList;
        ScriptList.ItemsSource = _scripts;
        _dispatcher = new Dispatcher(_logger, globalHook, _scripts);
        _dispatcher.ActiveScriptChanged += OnActiveScriptChanged;
        _dispatcher.OnScriptStopped += OnScriptStopped;
        _dispatcher.OnScriptRunning += OnScriptRunning;

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
}
