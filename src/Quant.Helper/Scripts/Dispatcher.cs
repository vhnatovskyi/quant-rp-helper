using Quant.Helper.Common;
using Quant.Helper.Scripts.Abstractions;
using SharpHook;
using SharpHook.Data;

namespace Quant.Helper.Scripts;

internal class Dispatcher
{
    private IScript? _activeScript;
    private readonly IGlobalHook _hook;
    private readonly ILogger _logger;
    private readonly IEnumerable<IScript> _scripts;
    private bool _ctrlPressed;

    public event Action<IScript?>? ActiveScriptChanged;
    public event Action? OnScriptStopped;
    public event Action? OnScriptRunning;


    internal IScript? ActiveScript
    {
        get => _activeScript;
        set
        {
            SetActiveScript(value, true);
        }
    }
    public Dispatcher(ILogger logger, IGlobalHook globalHook, IEnumerable<IScript> scripts)
    {
        _hook = globalHook;
        _logger = logger;
        _scripts = scripts;
        _hook.KeyPressed += async (_, e) => await KeyPressed_Handler(_, e);
        _hook.KeyReleased += (_, e) => KeyReleased_Handler(e);
    }

    internal void SetActiveScript(IScript? script, bool isChanged = false)
    {
        if (_activeScript != script)
            _activeScript?.OnRunningEvent -= OnScriptStateChanged;

        _activeScript = script;

        if (_activeScript != null)
            _activeScript.OnRunningEvent += OnScriptStateChanged;

        if (isChanged)
        {
            ActiveScriptChanged?.Invoke(_activeScript);
            _logger.Log("Обрано: " + (_activeScript?.Name ?? "Нічого"));
        }
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
            }
            else if (key != KeyCode.VcF5 || ActiveScript == null)
                selected = null;
            else
            {
                await ExecuteScriptAsync();
                selected = null;
            }
        }
    }

    private void KeyReleased_Handler(KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode == KeyCode.VcLeftControl || e.Data.KeyCode == KeyCode.VcRightControl)
            _ctrlPressed = false;
    }

    private void OnScriptStateChanged(bool isRunning)
    {
        if (isRunning)
        {
            _logger.Log($"[{ActiveScript?.Name}]: Запущено");
            OnScriptRunning?.Invoke();
        }
        else
        {
            OnScriptStopped?.Invoke();
            _logger.Log($"[{ActiveScript?.Name}]: Зупинено");
        }
    }

    internal async Task StopScriptAsync()
    {
        if (ActiveScript == null)
            return;
        await ActiveScript.StopAsync();
    }

    internal async Task ExecuteScriptAsync()
    {
        if (ActiveScript == null)
            return;

        await ActiveScript.ExecuteAsync();
    }

    public async Task RunAsync() => await _hook.RunAsync();
}
