using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface.Utility;
using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace CurrencyWatchdog.Interface.Window.SettingsTabs;

public partial class BurdensTab(ConfigWindow window) {
    private readonly DragDropHelper<Burden> burdenDragDrop = new("BURDEN");

    private int selectedBurdenIndex = -1;

    public void Draw(Config config, ref bool changed) {
        using var table = ImRaii.Table("BurdensLayout", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupColumn("Burdens", ImGuiTableColumnFlags.WidthFixed, 250);
        ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        DrawBurdenPanel(config, ref changed);

        ImGui.TableNextColumn();

        if (selectedBurdenIndex >= 0) {
            if (selectedBurdenIndex < config.Burdens.Count) {
                var burden = config.Burdens[selectedBurdenIndex];
                DrawBurden(burden, ref changed);
            }
        } else {
            const string line1 = "Select or create a burden.";
            const string line2 = "Burdens are used to track currency and items.";

            var size1 = ImGui.CalcTextSize(line1);
            var size2 = ImGui.CalcTextSize(line2);

            var avail = ImGui.GetContentRegionAvail();
            var cursor = ImGui.GetCursorPos();
            var lineSpacing = size1 with { X = 0 };

            ImCursor.Position = cursor;
            ImCursor.ToNestedRect(size1, avail, ImAlign.Center, -lineSpacing);
            ImGui.TextUnformatted(line1);

            ImCursor.Position = cursor;
            ImCursor.ToNestedRect(size2, avail, ImAlign.Center, lineSpacing);
            ImGui.TextUnformatted(line2);
        }
    }

    private void DrawBurdenPanel(Config config, ref bool changed) {
        ImGui.Text("Burdens");
        ImGui.Separator();

        DrawBurdenList(config, ref changed);

        var buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;

        if (ImGui.Button("Create", new Vector2(buttonWidth, 0))) {
            config.Burdens.Add(new Burden());
            selectedBurdenIndex = config.Burdens.Count - 1;
            changed = true;
        }

        ImGui.SameLine();

        if (ImGui.Button("Add Presets", new Vector2(buttonWidth, 0))) {
            var token = window.PresetSelectorSlot.Acquire(Guid.Empty);
            Plugin.WindowManager.PresetSelectorWindow.OpenWithCallback(() => token.Supply(Plugin.WindowManager.PresetSelectorWindow.GetSelection()));
        }

        if (window.PresetSelectorSlot.TryConsume(Guid.Empty) is { } newBurdens) {
            foreach (var burden in newBurdens) {
                config.Burdens.Add(burden);
                changed = true;
            }
        }
    }

    private void DrawBurdenList(Config config, ref bool changed) {
        using var child = ImRaii.Child("burdenList", new Vector2(-1, -30 * ImGuiHelpers.GlobalScale), false);
        if (!child) return;

        for (var i = 0; i < config.Burdens.Count; i++) {
            DrawBurdenListItem(config, i, ref changed);
        }

        burdenDragDrop.EndFrame();
    }

    private void DrawBurdenListItem(Config config, int i, ref bool changed) {
        var burden = config.Burdens[i];

        using var id = ImRaii.PushId($"burdenList:{i}");
        var hoverId = ImGui.GetID("hover");

        var isSelected = selectedBurdenIndex == i;
        var (icon, label) = Utils.GetBurdenDisplay(burden);
        var selectableSize = ImGuiHelpers.ScaledVector2(ImGui.GetContentRegionAvail().X, 24);

        using (ImCursor.Excursion()) {
            if (ImGui.Selectable("##selectable", isSelected, ImGuiSelectableFlags.AllowDoubleClick, selectableSize)) {
                selectedBurdenIndex = i;
            }

            using (var drag = burdenDragDrop.Drag(hoverId, config.Burdens, i)) {
                if (drag) drag.SetSourceName(label);
            }
            using (var drop = burdenDragDrop.Drop(hoverId, config.Burdens, DragMask.Reorder)) {
                if (drop) {
                    config.Burdens.Swap(drop.SourceIndex, i, ref selectedBurdenIndex);
                    changed = true;
                }
            }

            using (var drop = ruleDragDrop.Drop(hoverId, burden.Rules, DragMask.MoveOrCopy)) {
                if (drop.Dropped) {
                    if (drop.Action == DragAction.Copy) {
                        burden.Rules.Add(drop.Target.Clone());
                    } else {
                        burden.Rules.Add(drop.PopTarget());
                    }
                    changed = true;
                }
            }

            using (var drop = jurisdictionDragDrop.Drop(hoverId, burden.Jurisdictions, DragMask.MoveOrCopy)) {
                if (drop.Dropped) {
                    if (drop.Action == DragAction.Copy) {
                        burden.Jurisdictions.Add(drop.Target.Clone());
                    } else {
                        burden.Jurisdictions.Add(drop.PopTarget());
                    }
                    changed = true;
                }
            }
        }

        if (icon.GetTexture() is { } texture) {
            using var wrap = texture.GetWrapOrEmpty();
            var tint = burden.Enabled ? Vector4.One : new Vector4(1, 1, 1, 0.5f);
            ImGui.Image(wrap.Handle, ImGuiHelpers.ScaledVector2(selectableSize.Y, selectableSize.Y), tintCol: tint);
            ImGui.SameLine();
        }

        ImCursor.ToNestedRect(ImGui.CalcTextSize(label), selectableSize, ImAlign.Left);
        if (burden.Enabled) {
            ImGui.Text(label);
        } else {
            ImGui.TextDisabled(label);
        }
    }

    private void DrawBurden(Burden burden, ref bool changed) {
        using var child = ImRaii.Child("burdenDetails");
        if (!child) return;

        var startCursor = ImGui.GetCursorPos();

        var enabled = burden.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled)) {
            burden.Enabled = enabled;
            changed = true;
        }

        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0)))
        using (ImCursor.Excursion()) {
            var deleteButtonWidth = ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.TrashAlt, "Delete");
            var cloneButtonWidth = ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.Clone, "Clone");
            var currentPos = new ImFixedCursor(startCursor + new Vector2(ImGui.GetContentRegionAvail().X, 0));

            currentPos.X -= deleteButtonWidth;
            using (ImRaii.Disabled(!ImGui.GetIO().KeyShift)) {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.TrashAlt, "Delete")) {
                    Plugin.Config.Burdens.Remove(burden);
                    selectedBurdenIndex = -1;
                    changed = true;
                    return;
                }
            }
            ImGuiEx.HoverTooltip("Delete burden\n(hold shift)");

            currentPos.X -= cloneButtonWidth + ImGui.GetStyle().ItemSpacing.X;
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Clone, "Clone")) {
                var copy = burden.Clone();
                Plugin.Config.Burdens.Add(copy);
                selectedBurdenIndex = Plugin.Config.Burdens.Count - 1;
                changed = true;
            }
            ImGuiEx.HoverTooltip("Clone burden");
        }

        var name = burden.Name;
        ImGui.PushItemWidth(250 * ImGuiHelpers.GlobalScale);
        if (ImGuiEx.NullableInputText("Use custom name", Utils.GetBurdenDisplay(burden).Name, ref name)) {
            burden.Name = name;
            changed = true;
        }

        if (ImGui.CollapsingHeader("Advanced")) {
            var panelLimit = burden.PanelLimit;
            if (ImGuiEx.NullableInputInt("Limit displayed overlay panels", 5, ref panelLimit)) {
                burden.PanelLimit = panelLimit is { } limit ? Math.Clamp(limit, 0, int.MaxValue) : panelLimit;
                changed = true;
            }

            var chatLimit = burden.ChatLimit;
            if (ImGuiEx.NullableInputInt("Limit displayed chat alerts", 5, ref chatLimit)) {
                burden.ChatLimit = chatLimit is { } limit ? Math.Clamp(limit, 0, int.MaxValue) : chatLimit;
                changed = true;
            }

            DrawCreateJurisdictionsButton(burden, ref changed);
        }

        DrawSubjectsSection(burden, ref changed);

        DrawRulesSection(burden, ref changed);

        DrawJurisdictionsSection(burden, ref changed);
    }
}
