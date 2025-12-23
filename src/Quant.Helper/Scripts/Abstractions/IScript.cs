using SharpHook.Data;

namespace Quant.Helper.Scripts.Abstractions;

public interface IScript
{
    event Action<bool> OnRunningEvent;
    KeyCode SelectorKey { get; }

    string Name { get; }

    Task ExecuteAsync();

    Task StopAsync();
}
