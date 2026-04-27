using CurrencyWatchdog.Interface.Utility;
using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Action = System.Action;

namespace CurrencyWatchdog.Interface.Window;

public class ZoneSelectorWindow : Dalamud.Interface.Windowing.Window {

    private List<ZoneDetails> available = [];
    private List<ZoneDetails> shown = [];
    private List<uint> selectedIds = [];

    private string searchText = string.Empty;

    private Action? callback;

    private const float RowHeight = 40;

    public ZoneSelectorWindow() : base("Zone Selector", ImGuiWindowFlags.NoCollapse) {
        var size = new Vector2(350, 520);
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
        available = Service.DataManager.GetExcelSheet<TerritoryType>()
            .Select(ZoneUtils.GetDetails)
            .OfType<ZoneDetails>()
            .ToList();
        shown = available;
    }

    public override void OnClose() {
        base.OnClose();
        Reset();
        callback = null;
    }

    private void Reset() {
        available = [];
        shown = [];
        selectedIds = [];
        searchText = string.Empty;
    }

    public void OpenWithCallback(Action callbackAction) {
        callback = callbackAction;
        IsOpen = true;
        BringToFront();
    }

    public List<TerritoryType> GetSelection() {
        return selectedIds
            .Select(id => available.FirstOrDefault(t => t.TerritoryType.RowId == id))
            .OfType<ZoneDetails>()
            .Select(d => d.TerritoryType)
            .ToList();
    }

    private void UpdateFilter(string s) {
        var search = Normalize(s);
        if (string.IsNullOrWhiteSpace(search)) {
            shown = available;
        } else {
            shown = available
                .Where(t => t.ZoneSearch.Contains(search) || t.ContentSearch.Contains(search))
                .ToList();
        }
    }

    private static string Normalize(string s) {
        return s.Replace("'", "").Replace("-", "").Replace(" ", "").Trim().ToLowerInvariant();
    }

    public override void Draw() {
        using var id = ImRaii.PushId("ZoneSelector");

        ImGui.PushItemWidth(-1);
        if (ImGui.InputTextWithHint("##zoneSearch", "Search", ref searchText, 100)) {
            searchText = string.IsNullOrWhiteSpace(searchText) ? "" : searchText;
            UpdateFilter(searchText);
        }
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(-1);

        using (var scrollChild = ImRaii.Child("scrollChild", ImGui.GetContentRegionAvail() - ImGuiHelpers.ScaledVector2(0, 40))) {
            if (scrollChild) {
                ImGuiClip.ClippedDraw(shown, DrawZoneDetails, RowHeight * ImGuiHelpers.GlobalScale);
            }
        }

        DrawConfirmCancel();
    }

    private void DrawConfirmCancel() {
        ImGuiHelpers.ScaledDummy(5.0f);

        var selectionCount = selectedIds.Count;
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
            selectedIds.Clear();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - (100.0f * ImGuiHelpers.GlobalScale));
        if (ImGui.Button("Cancel", ImGuiHelpers.ScaledVector2(100.0f, 25.0f))) {
            IsOpen = false;
        }
    }

    private void DrawZoneDetails(ZoneDetails details) {
        using (ImCursor.Excursion()) {
            var selectableSize = new Vector2(ImGui.GetContentRegionAvail().X, RowHeight * ImGuiHelpers.GlobalScale);

            var rowId = details.TerritoryType.RowId;
            using var pushId = ImRaii.PushId($"territoryTypeId:{rowId}");

            using (ImRaii.PushColor(ImGuiCol.Border, Vector4.One))
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                if (ImGui.Selectable("##selectable", selectedIds.Contains(rowId), ImGuiSelectableFlags.AllowItemOverlap, selectableSize)) {
                    if (!selectedIds.Remove(rowId)) {
                        selectedIds.Add(rowId);
                    }
                }
            }
        }

        using (ImCursor.Excursion()) {
            DrawZoneDetailsContents(details);
        }
        ImCursor.Y += RowHeight * ImGuiHelpers.GlobalScale;
    }

    private void DrawZoneDetailsContents(ZoneDetails details) {
        const float width = RowHeight * 16 / 9;
        const float height = RowHeight;

        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(4, 0))) {
            if (Service.TextureProvider.GetFromGame($"ui/loadingimage/{details.TerritoryType.LoadingImage.Value.FileName}_hr1.tex").GetWrapOrDefault() is { } wrap) {
                ImGui.Image(wrap.Handle, ImGuiHelpers.ScaledVector2(width, height), new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f));
            } else {
                ImGuiHelpers.ScaledDummy(width, height);
            }

            ImGui.SameLine();

            var basePos = ImGui.GetCursorPos() + ImGuiHelpers.ScaledVector2(3, 0);
            var lineHeight = ImGui.GetTextLineHeight();
            var spacing = ImGui.GetStyle().ItemSpacing.Y;
            var hasContentLine = details.ContentFinderConditions.Count > 0;

            var totalHeight = hasContentLine
                ? lineHeight * 2 + spacing
                : lineHeight;
            var startY = basePos.Y + (height - totalHeight) * 0.5f;

            var currentLineNum = 0;
            ImGui.SetCursorPos(basePos with { Y = startY + lineHeight * currentLineNum + spacing * currentLineNum });

            var zoneText = ZoneUtils.GetName(details.TerritoryType);
            ImGui.Text(zoneText);

            if (ImGui.GetIO().KeyShift) {
                var textSize = ImGui.CalcTextSize(zoneText);
                using var green = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudViolet);
                ImGui.SetCursorPos(basePos with { Y = startY + lineHeight * currentLineNum + spacing * currentLineNum, X = basePos.X + textSize.X + (ImGui.GetStyle().ItemSpacing.X * 2) });
                ImGui.Text($"{ZoneUtils.GetInternalName(details.TerritoryType)}");
            }

            currentLineNum++;

            if (hasContentLine) {
                ImGui.SetCursorPos(basePos with { Y = startY + lineHeight * currentLineNum + spacing * currentLineNum });
                ImGui.TextDisabled(ZoneUtils.GetTypeAndCategory(details.ContentFinderConditions));
            }
        }
    }

}
