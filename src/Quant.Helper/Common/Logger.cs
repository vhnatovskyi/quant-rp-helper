using System.Collections.ObjectModel;
using System.Windows;

namespace Quant.Helper.Common;

public sealed class Logger : ILogger
{
    public ObservableCollection<string> Messages { get; private set; } = new ObservableCollection<string>();

    public void Log(string message)
    {
        Application.Current.Dispatcher.Invoke(() => Messages.Add($"[{DateTime.Now:T}] {message}"));
    }
}