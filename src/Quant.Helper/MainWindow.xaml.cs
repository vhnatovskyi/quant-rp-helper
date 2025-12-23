using Quant.Helper.Common;
using Quant.Helper.Scripts;
using Quant.Helper.Scripts.Abstractions;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

#nullable enable
namespace Quant.Helper;

public partial class MainWindow : Window, IComponentConnector
{
    private readonly List<IScript> _scripts;
    private readonly Dispatcher _dispatcher;
    private readonly ILogger _logger;

    public MainWindow()
    {
        InitializeComponent();
        _logger = new Logger();
        var scriptList = new List<IScript>
        {
         //  new TreeChopScript(),
            new ElectricScript(_logger)
        };
        _scripts = scriptList;
        ScriptList.ItemsSource = _scripts;
        _dispatcher = new Dispatcher(_logger, _scripts);
        _dispatcher.ActiveScriptChanged += OnActiveScriptChanged;
        _dispatcher.OnScriptStopped += OnScriptStopped;
        _logger.Messages.CollectionChanged += (_1, _2) => Dispatcher.Invoke(() =>
        {
            LogList.ItemsSource = new List<string>();
            LogList.ItemsSource = _logger.Messages;
            ListBox logList = LogList;
            ObservableCollection<string> messages = _logger.Messages;
            logList.ScrollIntoView(messages.LastOrDefault());
        });
        Task.Run(_dispatcher.RunAsync);
    }

    private async void Execute_Click(object sender, RoutedEventArgs e)
    {
        if (ScriptList.SelectedItem is IScript)
            await _dispatcher.ExecuteScriptAsync();
        UpdateStartStopButton();
    }

    private void UpdateStartStopButton()
    {
        ExecuteButton.Content = ScriptList.SelectedItem is IScript selectedItem && selectedItem.IsRunning ? "Зупитини" : "Запустити";
        _logger.Log(ExecuteButton.Content?.ToString() ?? string.Empty);
    }

    private void OnScriptStopped()
    {
        Dispatcher.Invoke(UpdateStartStopButton);
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
        ShowOrHideButton(ScriptList.SelectedItem as IScript);
    }

    private void ShowOrHideButton(IScript? script)
    {
        _dispatcher.ActiveScript = _dispatcher.ActiveScript != script ? script : null;
        ActiveScriptLabel.Text = _dispatcher.ActiveScript?.Name ?? "Відсутній";
        ActionPanel.Visibility = _dispatcher.ActiveScript != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
