using CurrencyWatchdog.Interface.Utility;
using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Action = System.Action;

namespace CurrencyWatchdog.Interface.Window;

public class ContentSelectorWindow : Dalamud.Interface.Windowing.Window {
    private List<ContentFinderCondition> available = [];
    private List<ContentFinderCondition> shown = [];
    private List<uint> selectedIds = [];

    private string searchText = string.Empty;

    private Action? callback;

    private const float RowHeight = 40;

    public ContentSelectorWindow() : base("Content Selector", ImGuiWindowFlags.NoCollapse) {
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
        available = Service.DataManager.GetExcelSheet<ContentFinderCondition>()
            .Where(ShouldDisplayContent)
            .ToList();
        shown = available;
    }

    private static bool ShouldDisplayContent(ContentFinderCondition cfc) {
        if (cfc.RowId is 0 || cfc.Name.IsEmpty)
            return false;

        if (cfc.Image is 0)
            return false;

        return true;
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

    public List<ContentFinderCondition> GetSelection() {
        return selectedIds
            .Select(id => available.FirstOrDefault(t => t.RowId == id))
            .ToList();
    }

    private void UpdateFilter(string s) {
        var search = Normalize(s);
        if (string.IsNullOrWhiteSpace(search)) {
            shown = available;
        } else {
            shown = available
                .Where(t =>
                    Utils.NormalizeForSearch(ZoneUtils.GetName(t)).Contains(search)
                    || Utils.NormalizeForSearch(ZoneUtils.GetTypeAndCategory(t)).Contains(search))
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
                ImGuiClip.ClippedDraw(shown, DrawContentDetails, RowHeight * ImGuiHelpers.GlobalScale);
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

    private void DrawContentDetails(ContentFinderCondition cfc) {
        using (ImCursor.Excursion()) {
            var selectableSize = new Vector2(ImGui.GetContentRegionAvail().X, RowHeight * ImGuiHelpers.GlobalScale);

            var rowId = cfc.RowId;
            using var pushId = ImRaii.PushId($"cfcId:{rowId}");
            using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

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
            DrawContentDetailsContents(cfc);
        }
        ImCursor.Y += RowHeight * ImGuiHelpers.GlobalScale;
    }

    private void DrawContentDetailsContents(ContentFinderCondition cfc) {
        const float width = RowHeight * 376 / 120;
        const float height = RowHeight;

        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(4, 0))) {
            if (Service.TextureProvider.GetFromGameIcon(new GameIconLookup { IconId = cfc.Image }).GetWrapOrEmpty() is { } wrap) {
                ImGui.Image(wrap.Handle, ImGuiHelpers.ScaledVector2(width, height), new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f));
            } else {
                ImGuiHelpers.ScaledDummy(width, height);
            }

            ImGui.SameLine();

            var basePos = ImGui.GetCursorPos() + ImGuiHelpers.ScaledVector2(3, 0);
            var lineHeight = ImGui.GetTextLineHeight();
            var spacing = ImGui.GetStyle().ItemSpacing.Y;

            var typeLine = ZoneUtils.GetTypeAndCategory(cfc);
            var contentLine = ZoneUtils.GetName(cfc);
            var hasTypeLine = typeLine.Length != 0;

            var totalHeight = hasTypeLine
                ? lineHeight * 2 + spacing
                : lineHeight;
            var startY = basePos.Y + (height - totalHeight) * 0.5f;

            var currentLineNum = 0;
            if (hasTypeLine) {
                ImGui.SetCursorPos(basePos with { Y = startY + lineHeight * currentLineNum + spacing * currentLineNum });
                ImGui.TextDisabled(typeLine);
                currentLineNum++;
            }

            ImGui.SetCursorPos(basePos with { Y = startY + lineHeight * currentLineNum + spacing * currentLineNum });
            ImGui.Text(contentLine);

            if (ImGui.GetIO().KeyShift) {
                var textSize = ImGui.CalcTextSize(contentLine);
                using var green = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudViolet);
                ImGui.SetCursorPos(basePos with {
                    Y = startY + lineHeight * currentLineNum + spacing * currentLineNum,
                    X = basePos.X + textSize.X + (ImGui.GetStyle().ItemSpacing.X * 2),
                });
                ImGui.Text($"{cfc.RowId}");
            }
        }
    }
}
