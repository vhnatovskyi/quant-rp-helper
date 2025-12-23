using Quant.Helper.Common;
using Quant.Helper.Scripts.Abstractions;
using SharpHook;
using SharpHook.Data;

namespace Quant.Helper.Scripts;

internal class Dispatcher
{
    internal IScript? ActiveScript;
    private readonly IGlobalHook _hook;
    private readonly ILogger _logger;
    private readonly IEnumerable<IScript> _scripts;
    private bool _ctrlPressed;

    public event Action<IScript?>? ActiveScriptChanged;
    public event Action? OnScriptStopped;
    public Dispatcher(ILogger logger, IEnumerable<IScript> scripts)
    {
        _hook = new SimpleGlobalHook();
        _logger = logger;
        _scripts = scripts;
        _hook.KeyPressed += async (_, e) => await KeyPressed_Handler(_, e);
        _hook.KeyReleased += (_, e) => KeyReleased_Handler(e);
    }

    private async Task KeyPressed_Handler(object obj, KeyboardHookEventArgs e)
    {
        KeyCode key = e.Data.KeyCode;
        IScript? selected;
        if (key == KeyCode.VcLeftControl || key == KeyCode.VcRightControl)
        {
            _ctrlPressed = true;
            selected = null;
        }
        else
        {
            selected = Enumerable.FirstOrDefault(_scripts, s => s.SelectorKey == key);
            if (selected != null && _ctrlPressed)
            {
                if (ActiveScript != null && ActiveScript != selected)
                    await StopScriptAsync();
                ActiveScript = ActiveScript == selected ? null : selected;
                _logger.Log("Обрано: " + (ActiveScript?.Name ?? "Нічого"));
                Action<IScript> activeScriptChanged = ActiveScriptChanged;
                if (activeScriptChanged == null)
                {
                    selected = null;
                }
                else
                {
                    activeScriptChanged(ActiveScript);
                    selected = null;
                }
            }
            else if (key != KeyCode.VcE)
                selected = null;
            else if (ActiveScript == null)
            {
                selected = null;
            }
            else
            {
                await ExecuteScriptAsync();
                selected = null;
            }
        }
    }

    private void KeyReleased_Handler(KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode != KeyCode.VcLeftControl && e.Data.KeyCode != KeyCode.VcRightControl)
            return;
        _ctrlPressed = false;
    }

    internal async Task StopScriptAsync()
    {
        if (ActiveScript == null)
            return;
        await ActiveScript.StopAsync();
        _logger.Log($"[{ActiveScript.Name}]: Зупинено");
    }

    internal async Task ExecuteScriptAsync()
    {
        if (ActiveScript == null)
            return;

        ActiveScript.RunningStateChanged += () =>
        {
            OnScriptStopped?.Invoke();
        };

        await ActiveScript.ExecuteAsync();
        _logger.Log($"[{ActiveScript.Name}]: " + (ActiveScript.IsRunning ? "Запущено" : "Скасовано"));
    }

    public async Task RunAsync() => await _hook.RunAsync();
}
