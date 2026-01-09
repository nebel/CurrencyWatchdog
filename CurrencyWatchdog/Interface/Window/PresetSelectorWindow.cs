using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Action = System.Action;

namespace CurrencyWatchdog.Interface.Window;

public class PresetSelectorWindow : Dalamud.Interface.Windowing.Window {
    private record Preset(string Name, Burden Burden);

    private List<Preset> availablePresets = [];
    private List<Preset> shownPresets = [];
    private List<string> selectedPresetNames = [];

    private string searchText = string.Empty;

    private Action? callback;

    private const float PresetRowHeight = 40;

    public PresetSelectorWindow() : base("Preset Selector", ImGuiWindowFlags.NoCollapse) {
        var size = new Vector2(350, 500);
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = size,
            MaximumSize = new Vector2(float.PositiveInfinity),
        };

        AllowPinning = false;
        AllowClickthrough = false;
    }

    public override void OnOpen() {
        base.OnOpen();
        Reset();
        availablePresets = Presets.PresetBurdens
            .Select(x => new Preset(x.Key, x.Value))
            .ToList();
        shownPresets = availablePresets;
    }

    public override void OnClose() {
        base.OnClose();
        Reset();
        callback = null;
    }

    private void Reset() {
        availablePresets = [];
        shownPresets = [];
        selectedPresetNames = [];
        searchText = string.Empty;
    }

    public void OpenWithCallback(Action callbackAction) {
        callback = callbackAction;
        IsOpen = true;
        BringToFront();
    }

    public List<Burden> GetSelection() {
        var list = new List<Burden>();
        foreach (var name in selectedPresetNames) {
            list.Add(Presets.PresetBurdens[name].Clone());
        }
        return list;
    }

    private void UpdateFilter(string s) {
        if (string.IsNullOrWhiteSpace(s))
            shownPresets = availablePresets;
        else
            shownPresets = availablePresets
                .Where(preset => Utils.GetBurdenDisplay(preset.Burden).Name.Contains(s, StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    public override void Draw() {
        using var id = ImRaii.PushId("PresetSelector");

        ImGui.PushItemWidth(-1);
        if (ImGui.InputTextWithHint("##presetSearch", "Search", ref searchText, 100)) {
            searchText = string.IsNullOrWhiteSpace(searchText) ? "" : searchText;
            UpdateFilter(searchText);
        }
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(-1);

        using (var scrollChild = ImRaii.Child("scrollChild", ImGui.GetContentRegionAvail() - ImGuiHelpers.ScaledVector2(0, 40))) {
            if (scrollChild) {
                ImGuiClip.ClippedDraw(shownPresets, DrawPreset, PresetRowHeight * ImGuiHelpers.GlobalScale);
            }
        }

        DrawConfirmCancel();
    }

    private void DrawConfirmCancel() {
        using var confirmationButtons = ImRaii.Child("selectCancel", ImGui.GetContentRegionAvail());
        if (!confirmationButtons) return;

        ImGuiHelpers.ScaledDummy(5.0f);

        var selectionCount = selectedPresetNames.Count;
        using (ImRaii.Disabled(selectionCount == 0)) {
            var label = selectionCount > 1 ? $"Confirm ({selectionCount})" : "Confirm";
            if (ImGui.Button(label, ImGuiHelpers.ScaledVector2(100.0f, 25.0f))) {
                callback?.Invoke();
                IsOpen = false;
            }
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX((ImGui.GetContentRegionMax().X / 2) - (50f * ImGuiHelpers.GlobalScale));
        if (ImGui.Button("Clear", ImGuiHelpers.ScaledVector2(100.0f, 25.0f))) {
            selectedPresetNames.Clear();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - (100.0f * ImGuiHelpers.GlobalScale));
        if (ImGui.Button("Cancel", ImGuiHelpers.ScaledVector2(100.0f, 25.0f))) {
            IsOpen = false;
        }
    }

    private void DrawPreset(Preset preset) {
        var cursorPosition = ImGui.GetCursorPos();
        var selectableSize = new Vector2(ImGui.GetContentRegionAvail().X, PresetRowHeight * ImGuiHelpers.GlobalScale);

        var name = preset.Name;
        using var id = ImRaii.PushId($"subjectName:{name}");

        using (ImRaii.PushColor(ImGuiCol.Border, Vector4.One))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 2)) {
            if (ImGui.Selectable("##selectable", selectedPresetNames.Contains(name), ImGuiSelectableFlags.AllowItemOverlap, selectableSize)) {
                if (!selectedPresetNames.Remove(name)) {
                    selectedPresetNames.Add(name);
                }
            }
        }

        ImGui.SetCursorPos(cursorPosition);
        using (ImRaii.Child("selection_child", selectableSize, false, ImGuiWindowFlags.NoInputs)) {
            DrawPresetContents(preset);
        }
    }

    private void DrawPresetContents(Preset preset) {
        var burdenDisplay = Utils.GetBurdenDisplay(preset.Burden);
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(4, 0))) {
            ImGui.TextDisabled("Preset");
            if (Service.TextureProvider.GetFromGameIcon(new GameIconLookup(burdenDisplay.IconId)) is { } texture) {
                using var wrap = texture.GetWrapOrEmpty();
                ImGui.Image(wrap.Handle, ImGuiHelpers.ScaledVector2(PresetRowHeight / 2, PresetRowHeight / 2));
                ImGui.SameLine();
            }
            ImGui.Text(burdenDisplay.Name);
        }
    }
}
