using CurrencyWatchdog.Interface.Window;
using Dalamud.Interface.Windowing;
using System;

namespace CurrencyWatchdog.Interface;

public sealed class WindowManager : IDisposable {
    public ConfigWindow ConfigWindow { get; }
    public SubjectSelectorWindow SubjectSelectorWindow { get; }
    public PresetSelectorWindow PresetSelectorWindow { get; }
    public ZoneSelectorWindow ZoneSelectorWindow { get; }
    public ContentSelectorWindow ContentSelectorWindow { get; }

    private readonly WindowSystem windowSystem = new("Currency Watchdog");

    public WindowManager() {
        ConfigWindow = new ConfigWindow();
        SubjectSelectorWindow = new SubjectSelectorWindow();
        PresetSelectorWindow = new PresetSelectorWindow();
        ZoneSelectorWindow = new ZoneSelectorWindow();
        ContentSelectorWindow = new ContentSelectorWindow();

        windowSystem.AddWindow(ConfigWindow);
        windowSystem.AddWindow(SubjectSelectorWindow);
        windowSystem.AddWindow(PresetSelectorWindow);
        windowSystem.AddWindow(ZoneSelectorWindow);
        windowSystem.AddWindow(ContentSelectorWindow);

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
