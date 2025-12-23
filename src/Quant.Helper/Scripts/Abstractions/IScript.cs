using SharpHook.Data;

namespace Quant.Helper.Scripts.Abstractions;

public interface IScript
{
    bool IsRunning { get; }
    event Action? RunningStateChanged;
    KeyCode SelectorKey { get; }

    string Name { get; }

    Task ExecuteAsync();

    Task StopAsync();
}
