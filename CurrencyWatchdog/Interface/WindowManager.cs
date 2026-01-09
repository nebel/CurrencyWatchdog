using CurrencyWatchdog.Interface.Window;
using Dalamud.Interface.Windowing;
using System;

namespace CurrencyWatchdog.Interface;

public sealed class WindowManager : IDisposable {
    public ConfigWindow ConfigWindow { get; }
    public SubjectSelectorWindow SubjectSelectorWindow { get; }
    public PresetSelectorWindow PresetSelectorWindow { get; }

    private readonly WindowSystem windowSystem = new("Currency Watchdog");

    public WindowManager() {
        ConfigWindow = new ConfigWindow();
        SubjectSelectorWindow = new SubjectSelectorWindow();
        PresetSelectorWindow = new PresetSelectorWindow();

        windowSystem.AddWindow(ConfigWindow);
        windowSystem.AddWindow(SubjectSelectorWindow);
        windowSystem.AddWindow(PresetSelectorWindow);

        Service.PluginInterface.UiBuilder.Draw += Draw;
        Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
    }

    public void Dispose() {
        windowSystem.RemoveAllWindows();
    }

    private void Draw() {
        windowSystem.Draw();
    }

    private void ToggleConfigUi() {
        ConfigWindow.Toggle();
    }
}
