using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface.Utility;
using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using System;
using System.Numerics;
using System.Text;

namespace CurrencyWatchdog.Interface.Window.SettingsTabs;

public partial class BurdensTab {
    private readonly ListDragDrop<Jurisdiction> jurisdictionDragDrop = new("JURIS");
    private readonly ListDragDrop<Activity> activityDragDrop = new("ACTIVITY");

    private void DrawCreateJurisdictionsButton(Burden burden, ref bool changed) {
        if (burden.Jurisdictions.Count != 0)
            return;

        const FontAwesomeIcon icon = FontAwesomeIcon.MapSigns;
        const string text = "Create jurisdictions";

        if (ImGuiComponents.IconButtonWithText(icon, text)) {
            burden.Jurisdictions.Add(new Jurisdiction());
            changed = true;
        }
        ImGuiComponents.HelpMarker("Creating jurisdictions will cause the \"Jurisdictions\" section to appear below the \"Rules\" section for this "
                                   + "burden. Jurisdictions allow you to configure the in-game locations in which a burden's alert panels are shown.");
    }

    private void DrawJurisdictionsSection(Burden burden, ref bool changed) {
        if (burden.Jurisdictions.Count != 0) {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Text("Jurisdictions");

            DrawAddJurisdictionButton(burden, ref changed);
            ImGui.Spacing();
            DrawJurisdictions(burden, ref changed);
        }

        activityDragDrop.EndFrame();
        jurisdictionDragDrop.EndFrame();
    }

    private void DrawAddJurisdictionButton(Burden burden, ref bool changed) {
        const FontAwesomeIcon icon = FontAwesomeIcon.MapSigns;
        const string text = "Add jurisdiction";

        var contentWidth = ImGuiComponents.GetIconButtonWithTextWidth(icon, text);

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - contentWidth);

        if (ImGuiComponents.IconButtonWithText(icon, text)) {
            burden.Jurisdictions.Add(new Jurisdiction());
            changed = true;
        }
    }

    private void DrawAddActivityButtons(Jurisdiction juris, ref bool changed) {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0));

        const FontAwesomeIcon contentIcon = FontAwesomeIcon.Dungeon;
        const string contentText = "Add content";
        var contentWidth = ImGuiComponents.GetIconButtonWithTextWidth(contentIcon, contentText);

        var currentPos = new ImFixedCursor();
        currentPos.X += ImGui.GetContentRegionAvail().X;
        currentPos.X -= contentWidth;

        if (ImGuiComponents.IconButtonWithText(contentIcon, contentText)) {
            var token = window.ContentSelectorSlot.Acquire(juris.Guid);
            Plugin.WindowManager.ContentSelectorWindow.OpenWithCallback(() => token.Supply(Plugin.WindowManager.ContentSelectorWindow.GetSelection()));
        }

        if (window.ContentSelectorSlot.TryConsume(juris.Guid) is { } newConditions) {
            foreach (var condition in newConditions) {
                juris.Activities.Add(new Activity.ContentActivity { RowId = condition.RowId });
                changed = true;
            }
        }

        const FontAwesomeIcon zoneIcon = FontAwesomeIcon.Map;
        const string zoneText = "Add zone";
        var zoneWidth = ImGuiComponents.GetIconButtonWithTextWidth(zoneIcon, zoneText);

        currentPos.X -= zoneWidth + ImGui.GetStyle().ItemSpacing.X;

        if (ImGuiComponents.IconButtonWithText(zoneIcon, zoneText)) {
            var token = window.ZoneSelectorSlot.Acquire(juris.Guid);
            Plugin.WindowManager.ZoneSelectorWindow.OpenWithCallback(() => token.Supply(Plugin.WindowManager.ZoneSelectorWindow.GetSelection()));
        }

        if (window.ZoneSelectorSlot.TryConsume(juris.Guid) is { } newTypes) {
            foreach (var type in newTypes) {
                juris.Activities.Add(new Activity.ZoneActivity { RowId = type.RowId });
                changed = true;
            }
        }
    }

    private void DrawJurisdictions(Burden burden, ref bool changed) {
        var hasMatch = false;
        for (var i = 0; i < burden.Jurisdictions.Count; i++) {
            DrawJurisdiction(burden, i, ref changed, ref hasMatch);
        }
    }

    private void DrawJurisdiction(Burden burden, int i, ref bool changed, ref bool hasMatch) {
        var juris = burden.Jurisdictions[i];
        using var id = ImRaii.PushId($"jurisdiction:{i}");
        var hoverId = ImGui.GetID("hover");

        void RenderCloneButton(Vector2 currentPos, ref bool changed) {
            ImGui.SetCursorPos(currentPos);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Clone)) {
                var copy = juris.Clone();
                burden.Jurisdictions.Insert(i + 1, copy);
                changed = true;
            }
            ImGuiEx.HoverTooltip("Clone jurisdiction");
        }

        void RenderDeleteButton(Vector2 currentPos, ref bool changed) {
            ImGui.SetCursorPos(currentPos);
            using (ImRaii.Disabled(!ImGui.GetIO().KeyShift)) {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.TrashAlt)) {
                    burden.Jurisdictions.RemoveAt(i);
                    changed = true;
                }
            }
            ImGuiEx.HoverTooltip("Delete jurisdiction\n(hold shift)");
        }

        const float headerExtraPadding = 6f;
        var headerStartCursor = ImGui.GetCursorPos();
        var headerStartAvail = ImGui.GetContentRegionAvail();
        var headerFramePadding = ImGui.GetStyle().FramePadding + ImGuiHelpers.ScaledVector2(0, headerExtraPadding);

        Vector2 deletePos;
        Vector2 clonePos;
        using (ImRaii.PushId($"jurisdictionButtonsA"))
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0)))
        using (ImCursor.Excursion()) {
            var buttonWidth = ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.TrashAlt, "");
            var currentPos = headerStartCursor + new Vector2(headerStartAvail.X - buttonWidth - ImGui.GetStyle().ItemSpacing.X, headerExtraPadding * ImGuiHelpers.GlobalScale);
            deletePos = currentPos;
            RenderDeleteButton(deletePos, ref changed);
            currentPos.X -= buttonWidth + ImGui.GetStyle().ItemSpacing.X;
            clonePos = currentPos;
            RenderCloneButton(clonePos, ref changed);
        }

        var colorHeader = false;
        var headerColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        if (!juris.Enabled) {
            colorHeader = true;
        } else if (OverlayUpdater.GetJurisdictionVisibility(juris) is not null) {
            colorHeader = true;
            headerColor = hasMatch ? ImGui.GetColorU32(ImGuiColors.AttentionForeground) : ImGui.GetColorU32(ImGuiColors.SuccessForeground);
            hasMatch = true;
        }

        ImGui.SetCursorPos(headerStartCursor);
        bool header;
        var headerLabel = juris.Name ?? GetJurisdictionName(juris);
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, headerFramePadding))
        using (ImRaii.PushColor(ImGuiCol.Text, headerColor, colorHeader)) {
            header = ImGui.CollapsingHeader(headerLabel + $"###jurisdictionHeader:{i}");
        }

        using (var drag = jurisdictionDragDrop.Drag(hoverId, burden.Jurisdictions, i)) {
            if (drag) jurisdictionDragDrop.SourceName = headerLabel;
        }
        using (var drop = jurisdictionDragDrop.Drop(hoverId, burden.Jurisdictions, DragMask.Reorder)) {
            if (drop.Dropped && jurisdictionDragDrop.Element is { } el) {
                el.Swap(i);
                changed = true;
            }
        }

        using (ImRaii.PushId($"jurisdictionButtonsB"))
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0)))
        using (ImCursor.Excursion()) {
            RenderDeleteButton(deletePos, ref changed);
            RenderCloneButton(clonePos, ref changed);
        }

        if (header) {
            ImGui.Spacing();
            DrawAddActivityButtons(juris, ref changed);
            ImGui.SameLine();

            using var indent = ImRaii.PushIndent();
            var enabled = juris.Enabled;
            if (ImGui.Checkbox("Enabled", ref enabled)) {
                juris.Enabled = enabled;
                changed = true;
            }

            var name = juris.Name;
            if (ImGuiEx.NullableInputText("Use custom name", GetJurisdictionName(juris), ref name)) {
                juris.Name = name;
                changed = true;
            }

            var visDefault = juris.Visibility.Default;
            if (ImGuiEx.NullableEnumCombo("When not in a Duty...", PanelVisibilityKind.Show, ref visDefault)) {
                juris.Visibility = juris.Visibility with { Default = visDefault };
                changed = true;
            }

            var visInDuty = juris.Visibility.InDuty;
            var defaultVisInDuty = Plugin.Config.OverlayConfig.HideInDuty ? PanelVisibilityKind.Hide : PanelVisibilityKind.Show;
            if (ImGuiEx.NullableEnumCombo("When in a Duty...", defaultVisInDuty, ref visInDuty)) {
                juris.Visibility = juris.Visibility with { InDuty = visInDuty };
                changed = true;
            }

            ImGui.Spacing();

            DrawActivities(juris, ref changed);
        }
    }

    private void DrawActivities(Jurisdiction juris, ref bool changed) {
        var zoneTextSize = ImGui.CalcTextSize("Zone");
        var contentTextSize = ImGui.CalcTextSize("Content");
        var typeTextSize = zoneTextSize with { X = Math.Max(zoneTextSize.X, contentTextSize.X) };

        for (var i = 0; i < juris.Activities.Count; i++) {
            using var id = ImRaii.PushId($"activity:{i}");
            var activity = juris.Activities[i];
            DrawActivity(juris, activity, i, typeTextSize, ref changed);
            ImGui.Spacing();
        }
    }

    private static (string Name, string Subtitle, ActivityType type) GetDisplay(Activity activity) {
        if (activity is Activity.ContentActivity contentActivity) {
            var content = Service.DataManager.Excel.GetSheet<ContentFinderCondition>().GetRow(contentActivity.RowId);
            return (ZoneUtils.GetName(content), ZoneUtils.GetTypeAndCategory(content), ActivityType.Content);
        }

        if (activity is Activity.ZoneActivity zoneActivity) {
            var zone = Service.DataManager.Excel.GetSheet<TerritoryType>().GetRow(zoneActivity.RowId);
            var sub = ZoneUtils.GetDetails(zone) is { } details ? ZoneUtils.GetTypeAndCategory(details.ContentFinderConditions) : "";
            return (ZoneUtils.GetName(zone), sub, ActivityType.Zone);
        }

        throw new Exception($"Unknown activity type: {activity.GetType().Name}");
    }


    private void DrawActivity(Jurisdiction juris, Activity activity, int i, Vector2 typeBounds, ref bool changed) {
        const float rowHeight = 36;

        var hoverId = ImGui.GetID("hover");

        var bgCol = activityDragDrop.GetRole(hoverId) switch {
            DragDropRole.None => new Vector4(1, 1, 1, 0.05f),
            DragDropRole.Source => new Vector4(1, 1, 1, 0.15f),
            DragDropRole.Target => new Vector4(1, 1, 0, 0.15f),
            _ => Vector4.Zero,
        };

        var fadeMultiplier = activity.Enabled ? 1f : 0.3f;
        var typeColor = ImGuiEx.GetFadedColor(ImGuiCol.TextDisabled, fadeMultiplier);
        var nameColor = ImGuiEx.GetFadedColor(ImGuiCol.Text, fadeMultiplier);

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0));

        var xSpacing = ImGui.GetStyle().ItemSpacing.X;
        var startCursor = ImGui.GetCursorPos();
        var (name, sub, type) = GetDisplay(activity);
        var typeTextSize = ImGui.CalcTextSize(type.ToString());
        var dividerOffset = typeBounds.X + xSpacing * 4;

        ImGuiEx.DrawRoundedBackground(rowHeight, bgCol, new Vector4(1, 1, 1, 0.1f), dividerOffset);
        ImCursor.ToNestedRect(typeTextSize, new Vector2(dividerOffset, rowHeight));
        ImGui.TextColored(typeColor, type.ToString());

        ImCursor.Position = startCursor + new Vector2(dividerOffset + (xSpacing * 2), 0);
        if (sub.Length == 0) {
            ImCursor.ToNestedRect(typeBounds, new Vector2(0, rowHeight), ImAlign.Left);
            ImGui.TextColored(nameColor, name);
        } else {
            using (ImRaii.Group()) {
                ImGui.TextColored(typeColor, sub);
                ImGui.TextColored(nameColor, name);
            }
        }

        var currentPos = new ImFixedCursor(startCursor + new Vector2(ImGui.GetContentRegionAvail().X, (rowHeight / 2) - (ImGui.GetFrameHeight() / 2)));
        {
            var buttonIcon = FontAwesomeIcon.TrashAlt;
            var buttonText = "";
            currentPos.X -= ImGuiComponents.GetIconButtonWithTextWidth(buttonIcon, buttonText) + ImGui.GetStyle().ItemSpacing.X;
            using (ImRaii.Disabled(!ImGui.GetIO().KeyShift)) {
                if (ImGuiComponents.IconButton(buttonIcon)) {
                    juris.Activities.RemoveAt(i);
                    changed = true;
                }
            }
            ImGuiEx.HoverTooltip($"Delete {type.ToString().ToLower()}\n(hold shift)");
        }
        {
            var buttonIcon = activity.Enabled ? FontAwesomeIcon.ToggleOn : FontAwesomeIcon.ToggleOff;
            var buttonText = "";
            currentPos.X -= ImGuiComponents.GetIconButtonWithTextWidth(buttonIcon, buttonText) + ImGui.GetStyle().ItemSpacing.X;
            if (ImGuiComponents.IconButton(buttonIcon)) {
                activity.Enabled = !activity.Enabled;
                changed = true;
            }
            ImGuiEx.HoverTooltip("Toggle");
        }

        // Drag/Drop

        ImGui.SetCursorPos(startCursor);
        ImGui.InvisibleButton("##dragDropFrame", new Vector2(-1, rowHeight * ImGuiHelpers.GlobalScale));

        using (var drag = activityDragDrop.Drag(hoverId, juris.Activities, i)) {
            if (drag) activityDragDrop.SourceName = name;
        }
        using (var drop = activityDragDrop.Drop(hoverId, juris.Activities, DragMask.Any)) {
            if (drop.Dropped && activityDragDrop.Element is { } el) {
                if (activityDragDrop.DragAction == DragAction.Reorder) {
                    el.Swap(i);
                } else if (activityDragDrop.DragAction == DragAction.Copy) {
                    juris.Activities.Insert(i, el.Get() with { });
                } else {
                    juris.Activities.Insert(i, el.Pop());
                }
                changed = true;
            }
        }
    }

    private static string GetJurisdictionName(Jurisdiction juris) {
        if (juris.Activities.Count == 0)
            return "(Everywhere)";

        var sb = new StringBuilder();
        var activity = juris.Activities[0];
        if (activity is Activity.ContentActivity contentActivity) {
            var cfc = Service.DataManager.Excel.GetSheet<ContentFinderCondition>()[contentActivity.RowId];
            sb.Append(cfc.Name.ToString().FirstCharToUpper());
        } else if (activity is Activity.ZoneActivity zoneActivity) {
            var territoryType = Service.DataManager.Excel.GetSheet<TerritoryType>()[zoneActivity.RowId];
            sb.Append(territoryType.PlaceName.ValueNullable?.Name.ToString() ?? "(Unknown)");
        }

        if (juris.Activities.Count > 1) {
            sb.Append(" (+");
            sb.Append(juris.Activities.Count - 1);
            sb.Append(')');
        }

        return sb.ToString();
    }

    private enum ActivityType {
        Content,
        Zone,
    }
}
