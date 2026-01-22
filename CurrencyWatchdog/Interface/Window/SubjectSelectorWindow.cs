using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Action = System.Action;

namespace CurrencyWatchdog.Interface.Window;

public class SubjectSelectorWindow : Dalamud.Interface.Windowing.Window {
    private List<Item> availableItems = [];
    private List<Item> shownItems = [];
    private List<uint> selectedItemIds = [];
    private List<Subject> selectedSubjects = [];

    private string searchText = string.Empty;

    private TabType? lastTabType;
    private bool tabChanged;

    private Action? callback;

    private const float SpecialRowHeight = 40;
    private const float ItemRowHeight = 25;

    public SubjectSelectorWindow() : base("Subject Selector", ImGuiWindowFlags.NoCollapse) {
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
    }

    public override void OnClose() {
        base.OnClose();
        Reset();
        callback = null;
    }

    private void Reset() {
        availableItems = [];
        shownItems = [];
        selectedItemIds = [];
        selectedSubjects = [];

        searchText = string.Empty;
        lastTabType = null;
        tabChanged = false;
    }

    public void OpenWithCallback(Action callbackAction) {
        callback = callbackAction;
        IsOpen = true;
        BringToFront();
    }

    public List<Subject> GetSelection() {
        return new List<Subject>(selectedSubjects)
            .Concat(selectedItemIds.Select(id => new Subject { Id = id }))
            .ToList();
    }

    public override void Draw() {
        using var id = ImRaii.PushId("SubjectSelector");

        using var tabBar = ImRaii.TabBar("tabs");
        if (!tabBar) return;

        using (var specialTab = ImRaii.TabItem("Special")) {
            if (specialTab) {
                if (lastTabType is null or not TabType.Special) {
                    lastTabType = TabType.Special;
                    tabChanged = true;
                }
                DrawSpecialTab();
            }
        }

        using (var currencyTab = ImRaii.TabItem("Currency items")) {
            if (currencyTab) {
                if (lastTabType is null or not TabType.CurrencyItems) {
                    lastTabType = TabType.CurrencyItems;
                    tabChanged = true;
                }
                DrawItemTab(TabType.CurrencyItems);
            }
        }

        using (var specialTab = ImRaii.TabItem("All items")) {
            if (specialTab) {
                if (lastTabType is null or not TabType.AllItems) {
                    lastTabType = TabType.AllItems;
                    tabChanged = true;
                }
                DrawItemTab(TabType.AllItems);
            }
        }

        tabChanged = false;
    }

    private record SubjectRenderInfo(Subject Subject, string SubjectName, SubjectDetails? Details);

    private void DrawSpecialTab() {
        var list = new List<SubjectRenderInfo>();
        foreach (var subjectType in Enum.GetValues<SubjectType>()) {
            if (subjectType == SubjectType.Item)
                continue;
            var subject = new Subject { Type = subjectType };
            var subjectName = subjectType.GetDisplayName();
            var details = Plugin.Evaluator.GetDetails(subject);
            list.Add(new SubjectRenderInfo(subject, subjectName, details));
        }

        using (var scrollChild = ImRaii.Child("scrollChild", ImGui.GetContentRegionAvail() - ImGuiHelpers.ScaledVector2(0, 40))) {
            if (scrollChild) {
                ImGuiClip.ClippedDraw(list, DrawSubject, SpecialRowHeight * ImGuiHelpers.GlobalScale);
            }
        }

        DrawConfirmCancel();
    }

    private void DrawItemTab(TabType type) {
        if (tabChanged) {
            availableItems = type == TabType.CurrencyItems ? GetCurrencyItems().ToList() : GetItems().ToList();
            UpdateItemFilter(searchText);
        }

        ImGui.PushItemWidth(-1);
        if (ImGui.InputTextWithHint("##itemSearch", "Search", ref searchText, 100)) {
            searchText = string.IsNullOrWhiteSpace(searchText) ? "" : searchText;
            UpdateItemFilter(searchText);
        }

        if (tabChanged) {
            ImGui.SetKeyboardFocusHere(-1);
        }

        using (var scrollChild = ImRaii.Child("scrollChild", ImGui.GetContentRegionAvail() - ImGuiHelpers.ScaledVector2(0, 40))) {
            if (scrollChild) {
                if (shownItems.Count != 0) {
                    ImGuiClip.ClippedDraw(shownItems, DrawItem, ItemRowHeight * ImGuiHelpers.GlobalScale);
                } else {
                    ImGui.Text("No matches found.");
                }
            }
        }

        DrawConfirmCancel();
    }

    private void DrawConfirmCancel() {
        using var confirmationButtons = ImRaii.Child("selectCancel", ImGui.GetContentRegionAvail());
        if (!confirmationButtons) return;

        ImGuiHelpers.ScaledDummy(5.0f);

        var selectionCount = selectedItemIds.Count + selectedSubjects.Count;
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
            selectedItemIds.Clear();
            selectedSubjects.Clear();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - (100.0f * ImGuiHelpers.GlobalScale));
        if (ImGui.Button("Cancel", ImGuiHelpers.ScaledVector2(100.0f, 25.0f))) {
            IsOpen = false;
        }
    }

    private void UpdateItemFilter(string s) {
        shownItems = string.IsNullOrWhiteSpace(s)
            ? availableItems
            : availableItems.Where(item => item.Name.ToString().Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
    }


    private void DrawSubject(SubjectRenderInfo info) {
        var cursorPosition = ImGui.GetCursorPos();
        var selectableSize = new Vector2(ImGui.GetContentRegionAvail().X, SpecialRowHeight * ImGuiHelpers.GlobalScale);

        var subject = info.Subject;
        using var id = ImRaii.PushId($"subjectId:{(int)subject.Type}:{subject.Id}");

        using (ImRaii.PushColor(ImGuiCol.Border, Vector4.One))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 2)) {
            if (ImGui.Selectable("##selectable", selectedSubjects.Contains(subject), ImGuiSelectableFlags.AllowItemOverlap, selectableSize)) {
                if (!selectedSubjects.Remove(subject)) {
                    selectedSubjects.Add(subject);
                }
            }
        }

        ImGui.SetCursorPos(cursorPosition);
        using (ImRaii.Child("selection_child", selectableSize, false, ImGuiWindowFlags.NoInputs)) {
            DrawSubjectContents(info);
        }
    }

    private void DrawSubjectContents(SubjectRenderInfo info) {
        var availableHeight = SpecialRowHeight * ImGuiHelpers.GlobalScale;
        if (info.Details is { } details && Service.TextureProvider.GetFromGameIcon(new GameIconLookup(details.IconId)) is { } texture) {
            using var wrap = texture.GetWrapOrEmpty();
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(4, 0))) {
                ImGui.TextDisabled(info.SubjectName);
                ImGui.Image(wrap.Handle, ImGuiHelpers.ScaledVector2(SpecialRowHeight / 2, SpecialRowHeight / 2));
                ImGui.SameLine();
                ImGui.Text(details.Name);
            }
        } else {
            var text = info.SubjectName;
            var textSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + (availableHeight - textSize.Y) / 2);
            ImGui.Text(text);
        }
    }

    private void DrawItem(Item item) {
        var cursorPosition = ImGui.GetCursorPos();
        var selectableSize = new Vector2(ImGui.GetContentRegionAvail().X, ItemRowHeight * ImGuiHelpers.GlobalScale);

        using var id = ImRaii.PushId($"itemId:{item.RowId}");

        if (ImGui.Selectable("##selectable", selectedItemIds.Contains(item.RowId), ImGuiSelectableFlags.AllowItemOverlap, selectableSize)) {
            if (!selectedItemIds.Remove(item.RowId)) {
                selectedItemIds.Add(item.RowId);
            }
        }

        ImGui.SetCursorPos(cursorPosition);
        using (ImRaii.Child("selection_child", selectableSize, false, ImGuiWindowFlags.NoInputs)) {
            DrawItemContents(item);
        }
    }

    protected void DrawItemContents(Item option) {
        if (Service.TextureProvider.GetFromGameIcon(new GameIconLookup(option.Icon)) is { } texture) {
            using var wrap = texture.GetWrapOrEmpty();
            ImGui.Image(wrap.Handle, ImGuiHelpers.ScaledVector2(ItemRowHeight, ItemRowHeight));
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + (3 * ImGuiHelpers.GlobalScale));
            ImGui.Text(option.Name.ToString());
        }
    }

    private enum TabType {
        Special,
        CurrencyItems,
        AllItems,
    }

    public static IEnumerable<Item> GetItems() {
        return Service.DataManager.GetExcelSheet<Item>().Where(item => item is { Name.IsEmpty: false });
    }

    public static IEnumerable<Item> GetCurrencyItems() {
        var obsoleteTomes = GetObsoleteTomestones();
        return GetItems()
            .Where(item => item is { ItemUICategory.RowId: 100 } or { RowId: >= 1 and < 100 })
            .Where(item => !obsoleteTomes.Contains(item));
    }

    private static HashSet<Item> GetObsoleteTomestones()
        => Service.DataManager
            .GetExcelSheet<TomestonesItem>()
            .Where(item => item.Tomestones.RowId is 0)
            .Select(item => item.Item.Value)
            .ToHashSet(EqualityComparer<Item>.Create((x, y) => x.RowId == y.RowId, obj => obj.RowId.GetHashCode()));
}
