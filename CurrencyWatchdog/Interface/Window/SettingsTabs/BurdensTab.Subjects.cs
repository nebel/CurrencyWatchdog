using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface.Utility;
using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Lumina.Excel.Sheets;
using System;
using System.Numerics;

namespace CurrencyWatchdog.Interface.Window.SettingsTabs;

public partial class BurdensTab {
    private const string PopupEditSubject = "currency-watchdog-edit-alias";

    private readonly DragDropHelper<Subject> subjectDragDrop = new("SUBJECT");

    private string editingAlias = "";
    private uint editingOverrideCap;

    private void DrawSubjectsSection(Burden burden, ref bool changed) {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Text("Subjects");

        DrawAddSubjectsButton(burden, ref changed);
        ImGui.Spacing();
        DrawSubjects(burden, ref changed);
    }

    private void DrawAddSubjectsButton(Burden burden, ref bool changed) {
        const FontAwesomeIcon icon = FontAwesomeIcon.SearchPlus;
        const string text = "Add subjects";

        var buttonWidth = ImGuiComponents.GetIconButtonWithTextWidth(icon, text);

        ImGui.SameLine();
        ImCursor.X += ImGui.GetContentRegionAvail().X - buttonWidth;

        if (ImGuiComponents.IconButtonWithText(icon, text)) {
            var token = window.SubjectSelectorSlot.Acquire(burden.Guid);
            Plugin.WindowManager.SubjectSelectorWindow.OpenWithCallback(() => token.Supply(Plugin.WindowManager.SubjectSelectorWindow.GetSelection()));
        }

        if (window.SubjectSelectorSlot.TryConsume(burden.Guid) is { } newSubjects) {
            foreach (var subject in newSubjects) {
                burden.Subjects.Add(subject);
                changed = true;
            }
        }
    }

    private void DrawSubjects(Burden burden, ref bool changed) {
        for (var i = 0; i < burden.Subjects.Count; i++) {
            using var id = ImRaii.PushId($"subject:{i}");
            var subject = burden.Subjects[i];
            DrawSubject(burden, subject, i, ref changed);
            ImGui.Spacing();
        }
        subjectDragDrop.EndFrame();
    }

    private void DrawSubject(Burden burden, Subject subject, int i, ref bool changed) {
        const float rowHeight = 40;

        var hoverId = ImGui.GetID("hover");

        var bgCol = subjectDragDrop.GetDragState(hoverId) switch {
            DragState.None => new Vector4(1, 1, 1, 0.05f),
            DragState.Source => new Vector4(1, 1, 1, 0.15f),
            DragState.Target => new Vector4(1, 1, 0, 0.15f),
            _ => Vector4.Zero,
        };

        var iconTint = subject.Enabled ? Vector4.One : new Vector4(1, 1, 1, 0.5f);
        var fadeMultiplier = subject.Enabled ? 1f : 0.3f;
        var typeColor = ImGuiEx.GetFadedColor(ImGuiCol.TextDisabled, fadeMultiplier);
        var qualityColor = ImGuiEx.GetFadedColor(ImGuiColors.HealerGreen, fadeMultiplier);
        var nameColor = ImGuiEx.GetFadedColor(ImGuiCol.Text, fadeMultiplier);
        var aliasColor = ImGuiEx.GetFadedColor(ImGuiColors.DalamudViolet, fadeMultiplier);
        var overrideCapColor = ImGuiEx.GetFadedColor(ImGuiColors.ParsedGold, fadeMultiplier);

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0));

        ImGuiEx.DrawRoundedBackground(rowHeight, bgCol, new Vector4(1, 1, 1, 0.1f));

        var startCursor = ImGui.GetCursorPos();

        var subjectTypeName = subject.Type.GetDisplayName();
        var subjectDetails = Plugin.Evaluator.GetDetails(subject);
        var subjectName = subjectDetails != null ? subjectDetails.Name : $"ID={subject.Id}";

        ImCursor.X += 4 * ImGuiHelpers.GlobalScale;
        using (ImRaii.Group()) {
            ImGui.TextColored(typeColor, subjectTypeName);

            if (subjectDetails != null) {
                if (Service.TextureProvider.GetFromGameIcon(new GameIconLookup(subjectDetails.IconId, subjectDetails.UseHqIcon)) is { } texture) {
                    using var wrap = texture.GetWrapOrEmpty();
                    ImGui.Image(wrap.Handle, ImGuiHelpers.ScaledVector2(rowHeight / 2, rowHeight / 2), tintCol: iconTint);
                    ImGui.SameLine();
                }
            }

            if (subject.Quality != SubjectQuality.Any) {
                ImGui.TextColored(qualityColor, $"{subject.Quality.GetDisplayName()}");
                ImGui.SameLine();
            }

            ImGui.TextColored(nameColor, subjectName);

            if (subject.Alias is not null) {
                ImGui.SameLine();
                ImGui.TextColored(aliasColor, $"({subject.Alias})");
            }

            if (subjectDetails != null) {
                ImGui.SameLine();
                if (ImGui.GetIO().KeyShift) {
                    ImGui.TextColored(typeColor, $"  {subjectDetails.QuantityHeldPercentage.ToString(Utils.PercentDisplayFormat)}%"
                                                 + $"  |  {subjectDetails.QuantityMissing.ToString(Utils.UintDisplayFormat)} missing");
                } else {
                    ImGui.TextColored(typeColor, $"  {subjectDetails.QuantityHeld.ToString(Utils.UintDisplayFormat)}");
                    ImGui.SameLine();
                    ImGui.TextColored(typeColor, "/");
                    ImGui.SameLine();
                    if (subject.OverrideCap is not null) {
                        ImGui.TextColored(overrideCapColor, $"{subjectDetails.EffectiveCap.ToString(Utils.UintDisplayFormat)} *");
                    } else {
                        ImGui.TextColored(typeColor, $"{subjectDetails.EffectiveCap.ToString(Utils.UintDisplayFormat)}");
                    }
                }
            } else {
                if (subject.OverrideCap is { } overrideCap) {
                    ImGui.SameLine();
                    ImGui.TextColored(overrideCapColor, $"(Cap = {overrideCap.ToString(Utils.UintDisplayFormat)})");
                }
            }
        }

        var currentPos = new ImFixedCursor(startCursor + new Vector2(ImGui.GetContentRegionAvail().X, (rowHeight / 2) - (ImGui.GetFrameHeight() / 2)));
        {
            var buttonIcon = FontAwesomeIcon.TrashAlt;
            var buttonText = "";
            currentPos.X -= ImGuiComponents.GetIconButtonWithTextWidth(buttonIcon, buttonText) + ImGui.GetStyle().ItemSpacing.X;
            using (ImRaii.Disabled(!ImGui.GetIO().KeyShift)) {
                if (ImGuiComponents.IconButton(buttonIcon)) {
                    burden.Subjects.RemoveAt(i);
                    changed = true;
                }
            }
            ImGuiEx.HoverTooltip("Delete subject\n(hold shift)");
        }
        {
            var buttonIcon = FontAwesomeIcon.Feather;
            var buttonText = "";
            currentPos.X -= ImGuiComponents.GetIconButtonWithTextWidth(buttonIcon, buttonText) + ImGui.GetStyle().ItemSpacing.X;
            if (ImGuiComponents.IconButton(buttonIcon)) {
                editingAlias = subject.Alias ?? "";
                editingOverrideCap = subjectDetails?.EffectiveCap ?? 999;
                ImGui.OpenPopup(PopupEditSubject);
            }
            ImGuiEx.HoverTooltip("Customize");

            DrawSubjectCustomizePopup(subject, subjectTypeName, subjectName, ref changed);
        }
        {
            var buttonIcon = subject.Enabled ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
            var buttonText = "";
            currentPos.X -= ImGuiComponents.GetIconButtonWithTextWidth(buttonIcon, buttonText) + ImGui.GetStyle().ItemSpacing.X;
            if (ImGuiComponents.IconButton(buttonIcon)) {
                subject.Enabled = !subject.Enabled;
                changed = true;
            }
            ImGuiEx.HoverTooltip("Toggle");
        }

        // Drag/Drop

        ImGui.SetCursorPos(startCursor);
        ImGui.InvisibleButton("##dragDropFrame", new Vector2(-1, rowHeight * ImGuiHelpers.GlobalScale));

        using (var drag = subjectDragDrop.Drag(hoverId, burden.Subjects, i)) {
            if (drag) drag.SetSourceName(subjectName);
        }

        using (var drop = subjectDragDrop.Drop(hoverId, burden.Subjects, DragMask.Reorder)) {
            if (drop) {
                burden.Subjects.Swap(drop.SourceIndex, i);
                changed = true;
            }
        }
    }

    private static bool CanBeHq(Subject subject) {
        return Service.DataManager.Excel.GetSheet<Item>().GetRowOrDefault(subject.Id) is { CanBeHq: true };
    }

    private void DrawSubjectCustomizePopup(Subject subject, string subjectTypeName, string subjectName, ref bool changed) {
        using var defaultStyle = ImRaii.DefaultStyle();
        using var popup = ImRaii.Popup(PopupEditSubject);
        if (!popup) return;

        ImGui.Text($"Customize {subjectTypeName}: {subjectName}");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var canBeHq = subject.Type == SubjectType.Item && CanBeHq(subject);
        var canChangeQuality = canBeHq || subject.Quality != SubjectQuality.Any;
        if (canBeHq || canChangeQuality) {
            ImGui.Text("Quality");
            if (canChangeQuality) {
                ImGui.SetNextItemWidth(160 * ImGuiHelpers.GlobalScale);
                var quality = subject.Quality;
                if (ImGuiEx.EnumCombo("Track items of quality...", ref quality)) {
                    subject.Quality = quality;
                    changed = true;
                }
            }
            if (canBeHq) {
                ImGui.SetNextItemWidth(160 * ImGuiHelpers.GlobalScale);
                var useHqIcon = subject.UseHqIcon;
                if (ImGui.Checkbox("Use HQ icon", ref useHqIcon)) {
                    subject.UseHqIcon = useHqIcon;
                    changed = true;
                }
            }
        }

        ImGui.SetNextItemWidth(160 * ImGuiHelpers.GlobalScale);
        ImGui.Text("Alias");
        var aliasEnter = ImGui.InputText("##alias", ref editingAlias, 200, ImGuiInputTextFlags.EnterReturnsTrue);
        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere(-1);
        ImGui.SameLine();
        if (ImGui.Button("Save alias") || aliasEnter) {
            subject.Alias = string.IsNullOrEmpty(editingAlias) ? null : editingAlias;
            changed = true;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear alias")) {
            subject.Alias = null;
            changed = true;
            ImGui.CloseCurrentPopup();
        }

        ImGui.Text("Custom Cap");
        ImGui.SetNextItemWidth(160 * ImGuiHelpers.GlobalScale);
        var localEditingOverrideCap = (int)editingOverrideCap;
        if (ImGui.InputInt("##overrideCap", ref localEditingOverrideCap, 1, 100)) {
            editingOverrideCap = (uint)Math.Clamp(localEditingOverrideCap, 1, 999_999_999);
        }
        ImGui.SameLine();
        if (ImGui.Button("Save cap")) {
            subject.OverrideCap = editingOverrideCap;
            changed = true;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear cap")) {
            subject.OverrideCap = null;
            changed = true;
            ImGui.CloseCurrentPopup();
        }
    }
}
