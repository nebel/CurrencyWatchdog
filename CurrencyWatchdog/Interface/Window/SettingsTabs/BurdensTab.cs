using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Expressions;
using CurrencyWatchdog.Interface.Utility;
using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using static CurrencyWatchdog.Expressions.SubjectExpression;

namespace CurrencyWatchdog.Interface.Window.SettingsTabs;

public class BurdensTab(ConfigWindow window) {
    private const string PopupEditSubject = "currency-watchdog-edit-alias";
    private const string PopupEditExpression = "currency-watchdog-edit-expression";
    private const string PopupEditOperator = "currency-watchdog-edit-operator";

    private readonly DragDropHelper burdenDragDrop = new("BURDEN");
    private readonly DragDropHelper subjectDragDrop = new("SUBJECT");
    private readonly DragDropHelper ruleDragDrop = new("RULE");

    private int selectedBurdenIndex = -1;
    private string editingAlias = "";
    private string editingConstant = "";
    private uint editingOverrideCap;

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
            var centerY = cursor.Y + (avail.Y * 0.5f) - (40 * ImGuiHelpers.GlobalScale);
            var spacing = ImGui.GetStyle().ItemSpacing.Y * 4;

            ImGui.SetCursorPos(new Vector2(cursor.X + ((avail.X - size1.X) * 0.5f), centerY - size1.Y - (spacing * 0.5f)));
            ImGui.TextUnformatted(line1);

            ImGui.SetCursorPos(new Vector2(cursor.X + ((avail.X - size2.X) * 0.5f), centerY + (spacing * 0.5f)));
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
        using var child = ImRaii.Child("BurdenList", new Vector2(-1, -30 * ImGuiHelpers.GlobalScale), false);
        if (!child) return;

        for (var i = 0; i < config.Burdens.Count; i++) {
            DrawBurdenListItem(config, i, ref changed);
        }

        burdenDragDrop.EndFrame();
    }

    private void DrawBurdenListItem(Config config, int i, ref bool changed) {
        var burden = config.Burdens[i];


        using var id = ImRaii.PushId($"burdenList:{i}");

        var isSelected = selectedBurdenIndex == i;
        var cursorPosition = ImGui.GetCursorPos();
        var selectableSize = ImGuiHelpers.ScaledVector2(ImGui.GetContentRegionAvail().X, 24);

        if (ImGui.Selectable("##selectable", isSelected, ImGuiSelectableFlags.AllowDoubleClick, selectableSize)) {
            using var selectChild = ImRaii.Child($"burdenListChild:{i}", new Vector2(-1, 0), true);
            if (selectChild) {
                selectedBurdenIndex = i;
            }
        }

        var (iconId, label) = Utils.GetBurdenDisplay(burden);

        using (var drag = burdenDragDrop.Drag(i)) {
            if (drag) ImGui.Text($"Reorder: {label}");
        }
        using (var drop = burdenDragDrop.Drop(i)) {
            if (drop) {
                config.Burdens.Move(drop.SourceIndex, i, ref selectedBurdenIndex);
                changed = true;
            }
        }

        ImGui.SetCursorPos(cursorPosition);

        using (ImRaii.Child("selectableChild", selectableSize, false, ImGuiWindowFlags.NoInputs)) {
            if (Service.TextureProvider.GetFromGameIcon(new GameIconLookup(iconId)) is { } texture) {
                using var wrap = texture.GetWrapOrEmpty();
                var tint = burden.Enabled ? Vector4.One : new Vector4(1, 1, 1, 0.5f);
                ImGui.Image(wrap.Handle, ImGuiHelpers.ScaledVector2(selectableSize.Y, selectableSize.Y), tintCol: tint);
                ImGui.SameLine();
            }

            var labelSize = ImGui.CalcTextSize(label);
            ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + ((selectableSize.Y - labelSize.Y) / 2));
            if (burden.Enabled) {
                ImGui.Text(label);
            } else {
                ImGui.TextDisabled(label);
            }
        }

        if (!burden.Enabled) {
            ImGui.SameLine();
            ImGui.TextDisabled("(disabled)");
        }
    }

    private void DrawBurden(Burden burden, ref bool changed) {
        using var child = ImRaii.Child("BurdenDetails");
        if (!child) return;

        var startCursor = ImGui.GetCursorPos();

        var enabled = burden.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled)) {
            burden.Enabled = enabled;
            changed = true;
        }

        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0)))
        using (ImGuiEx.CursorExcursion()) {
            var deleteButtonWidth = ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.TrashAlt, "Delete");
            var cloneButtonWidth = ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.Clone, "Clone");

            var currentPos = startCursor + new Vector2(ImGui.GetContentRegionAvail().X - deleteButtonWidth, 0);
            ImGui.SetCursorPos(currentPos);

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
            ImGui.SetCursorPos(currentPos);

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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Text("Subjects");

        DrawAddSubjectsButton(burden, ref changed);
        ImGui.Spacing();
        DrawSubjects(burden, ref changed);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Text("Rules");

        DrawAddRuleButton(burden, ref changed);
        ImGui.Spacing();
        DrawRules(burden, ref changed);
    }

    private void DrawAddSubjectsButton(Burden burden, ref bool changed) {
        const FontAwesomeIcon icon = FontAwesomeIcon.SearchPlus;
        const string text = "Add subjects";

        var buttonWidth = ImGuiComponents.GetIconButtonWithTextWidth(icon, text);

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - buttonWidth);

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

    private void DrawAddRuleButton(Burden burden, ref bool changed) {
        const FontAwesomeIcon icon = FontAwesomeIcon.BalanceScaleRight;
        const string text = "Add rule";

        var buttonWidth = ImGuiComponents.GetIconButtonWithTextWidth(icon, text);

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - buttonWidth);

        if (ImGuiComponents.IconButtonWithText(icon, text)) {
            burden.Rules.Add(new Rule {
                Conds = [new Cond(new Metric(MetricType.QuantityHeld), Operator.GreaterThanOrEqualTo, new Constant(0))],
            });
            changed = true;
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

        var bgCol = subjectDragDrop.GetDragState(i) switch {
            DragDropHelper.DragState.None => new Vector4(1, 1, 1, 0.05f),
            DragDropHelper.DragState.Source => new Vector4(1, 1, 1, 0.15f),
            DragDropHelper.DragState.Target => new Vector4(1, 1, 0, 0.15f),
            _ => Vector4.Zero,
        };

        var iconTint = subject.Enabled ? Vector4.One : new Vector4(1, 1, 1, 0.5f);
        var fadeMultiplier = subject.Enabled ? 1f : 0.3f;
        var typeColor = ImGuiEx.GetFadedColor(ImGuiCol.TextDisabled, fadeMultiplier);
        var nameColor = ImGuiEx.GetFadedColor(ImGuiCol.Text, fadeMultiplier);
        var aliasColor = ImGuiEx.GetFadedColor(ImGuiColors.DalamudViolet, fadeMultiplier);
        var overrideCapColor = ImGuiEx.GetFadedColor(ImGuiColors.ParsedGold, fadeMultiplier);

        using var color = new ImRaii.Color()
            .Push(ImGuiCol.Border, new Vector4(1, 1, 1, 0.1f))
            .Push(ImGuiCol.ChildBg, bgCol);
        using var style = new ImRaii.Style()
            .Push(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0))
            .Push(ImGuiStyleVar.WindowPadding, ImGuiHelpers.ScaledVector2(4, 0))
            .Push(ImGuiStyleVar.ChildRounding, 3);
        using var child = ImRaii.Child($"subjectChild", ImGuiHelpers.ScaledVector2(-1, rowHeight), true, ImGuiWindowFlags.NoMove);
        if (!child) return;

        var startCursor = ImGui.GetCursorPos();

        var subjectTypeName = subject.Type.GetDisplayName();
        ImGui.TextColored(typeColor, subjectTypeName);

        var subjectDetails = Plugin.Evaluator.GetDetails(subject);

        string subjectName;
        if (subjectDetails != null) {
            if (Service.TextureProvider.GetFromGameIcon(new GameIconLookup(subjectDetails.IconId)) is { } texture) {
                using var wrap = texture.GetWrapOrEmpty();
                ImGui.Image(wrap.Handle, ImGuiHelpers.ScaledVector2(rowHeight / 2, rowHeight / 2), tintCol: iconTint);
                ImGui.SameLine();
            }
            subjectName = subjectDetails.Name;
        } else {
            subjectName = $"ID={subject.Id}";
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

        Vector2 currentPos;
        {
            var buttonIcon = FontAwesomeIcon.TrashAlt;
            var buttonText = "";
            currentPos =
                startCursor + new Vector2(
                    ImGui.GetContentRegionAvail().X - ImGuiComponents.GetIconButtonWithTextWidth(buttonIcon, buttonText),
                    (ImGui.GetFrameHeight() / 2) - ImGui.GetStyle().FramePadding.Y
                );

            ImGui.SetCursorPos(currentPos);
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
            ImGui.SetCursorPos(currentPos);
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
            ImGui.SetCursorPos(currentPos);
            if (ImGuiComponents.IconButton(buttonIcon)) {
                subject.Enabled = !subject.Enabled;
                changed = true;
            }
            ImGuiEx.HoverTooltip("Toggle");
        }

        // Drag/Drop

        ImGui.SetCursorPos(startCursor);
        ImGui.InvisibleButton("##dragDropFrame", new Vector2(-1, rowHeight));

        using (var drag = subjectDragDrop.Drag(i)) {
            if (drag) ImGui.Text($"Reorder: {subjectName}");
        }

        using (var drop = subjectDragDrop.Drop(i)) {
            if (drop) {
                burden.Subjects.Move(drop.SourceIndex, i);
                changed = true;
            }
        }
    }

    private void DrawSubjectCustomizePopup(Subject subject, string subjectTypeName, string subjectName, ref bool changed) {
        using var defaultStyle = ImRaii.DefaultStyle();
        using var popup = ImRaii.Popup(PopupEditSubject);
        if (!popup) return;

        ImGui.Text($"Customize {subjectTypeName}: {subjectName}");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

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

    private void DrawRules(Burden burden, ref bool changed) {
        for (var i = 0; i < burden.Rules.Count; i++) {
            if (DrawRule(burden, i, ref changed)) break;
        }
        ruleDragDrop.EndFrame();
    }

    private bool DrawRule(Burden burden, int i, ref bool changed) {
        var rule = burden.Rules[i];
        using var id = ImRaii.PushId($"rule:{i}");

        void CloneRuleButton(Vector2 currentPos, ref bool changed) {
            ImGui.SetCursorPos(currentPos);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Clone)) {
                var copy = rule.Clone();
                burden.Rules.Insert(i + 1, copy);
                changed = true;
            }
            ImGuiEx.HoverTooltip("Clone rule");
        }

        void RenderDeleteButton(Vector2 currentPos, ref bool changed) {
            ImGui.SetCursorPos(currentPos);
            using (ImRaii.Disabled(!ImGui.GetIO().KeyShift)) {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.TrashAlt)) {
                    burden.Rules.RemoveAt(i);
                    changed = true;
                }
            }
            ImGuiEx.HoverTooltip("Delete rule\n(hold shift)");
        }

        const float headerExtraPadding = 6f;
        var headerStartCursor = ImGui.GetCursorPos();
        var headerStartAvail = ImGui.GetContentRegionAvail();
        var headerFramePadding = ImGui.GetStyle().FramePadding + ImGuiHelpers.ScaledVector2(0, headerExtraPadding);

        Vector2 deletePos;
        Vector2 clonePos;
        using (ImRaii.PushId($"ruleButtonsA"))
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0)))
        using (ImGuiEx.CursorExcursion()) {
            var buttonWidth = ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.TrashAlt, "");
            var currentPos = headerStartCursor + new Vector2(headerStartAvail.X - buttonWidth, headerExtraPadding * ImGuiHelpers.GlobalScale);
            deletePos = currentPos;
            RenderDeleteButton(deletePos, ref changed);
            currentPos.X -= buttonWidth + ImGui.GetStyle().ItemSpacing.X;
            clonePos = currentPos;
            CloneRuleButton(clonePos, ref changed);
        }

        ImGui.SetCursorPos(headerStartCursor);
        bool header;
        var headerLabel = GetCondDisplayName(rule.Conds);
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, headerFramePadding))
        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), !rule.Enabled)) {
            header = ImGui.CollapsingHeader(headerLabel + $"###ruleHeader:{i}");
        }

        using (var drag = burdenDragDrop.Drag(i)) {
            if (drag) ImGui.Text($"Reorder: {headerLabel}");
        }
        using (var drop = burdenDragDrop.Drop(i)) {
            if (drop) {
                burden.Rules.Move(drop.SourceIndex, i);
                changed = true;
            }
        }

        using (ImRaii.PushId($"ruleButtonsB"))
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0)))
        using (ImGuiEx.CursorExcursion()) {
            RenderDeleteButton(deletePos, ref changed);
            CloneRuleButton(clonePos, ref changed);
        }

        if (header) {
            using var indent = ImRaii.PushIndent();
            var enabled = rule.Enabled;
            if (ImGui.Checkbox("Enabled", ref enabled)) {
                rule.Enabled = enabled;
                changed = true;
            }

            ImGui.Spacing();
            DrawRuleConditions(rule, ref changed);

            ImGui.Spacing();
            DrawRuleOutputs(rule, ref changed);
        }
        return false;
    }

    private void DrawRuleConditions(Rule rule, ref bool changed) {
        ImGui.Text("Conditions");

        for (var i = 0; i < rule.Conds.Count; i++) {
            DrawCondition(rule, i, ref changed);
        }

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.PlusSquare, "Add condition")) {
            rule.Conds.Add(new Cond(new Metric(MetricType.QuantityHeld), Operator.GreaterThanOrEqualTo, new Constant(0)));
            changed = true;
        }
    }

    private void DrawCondition(Rule rule, int i, ref bool changed) {
        using var id = ImRaii.PushId($"condition:{i}");

        var cond = rule.Conds[i];

        var expressionSize = ImGuiHelpers.ScaledVector2(140, 0);
        var negateSize = ImGuiHelpers.ScaledVector2(50, 0);
        var operatorSize = ImGuiHelpers.ScaledVector2(50, 0);

        using (ImRaii.PushId($"left")) {
            var value = cond.Left;
            if (DrawExpr(ref value, expressionSize)) {
                rule.Conds[i] = cond with { Left = value };
                changed = true;
            }
        }

        ImGui.SameLine();
        using (ImRaii.PushId($"negate")) {
            var negate = cond.Negate;
            if (DrawNegate(ref negate, negateSize)) {
                rule.Conds[i] = cond with { Negate = negate };
                changed = true;
            }
        }

        ImGui.SameLine();
        using (ImRaii.PushId($"op")) {
            var op = cond.Operator;
            if (DrawOperator(ref op, operatorSize)) {
                rule.Conds[i] = cond with { Operator = op };
                changed = true;
            }
        }

        ImGui.SameLine();
        using (ImRaii.PushId($"right")) {
            var value = cond.Right;
            if (DrawExpr(ref value, expressionSize)) {
                rule.Conds[i] = cond with { Right = value };
                changed = true;
            }
        }

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Clone)) {
            rule.Conds.Insert(i + 1, cond with { });
            changed = true;
        }

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.TrashAlt)) {
            rule.Conds.RemoveAt(i);
            changed = true;
        }
    }

    private bool DrawExpr(ref SubjectExpression expr, Vector2 size) {
        if (ImGui.Button(expr.GetDisplayName(), size)) {
            editingConstant = (expr is Constant { Value: var constant } ? constant : 0).ToString(Utils.DecimalDisplayFormat);
            ImGui.OpenPopup(PopupEditExpression);
        }

        using var defaultStyle = ImRaii.DefaultStyle();
        using var popup = ImRaii.Popup(PopupEditExpression);
        if (!popup) return false;

        var constantValue = editingConstant;
        ImGui.PushItemWidth(size.X);
        if (ImGui.InputText("##customValue", ref constantValue, 20)) {
            editingConstant = constantValue;
        }

        decimal? constantValueParsed = null;
        try {
            var cleanedConstantValue = constantValue.Trim().Replace(",", "");
            constantValueParsed = Math.Clamp(decimal.Parse(cleanedConstantValue, NumberStyles.Any, CultureInfo.InvariantCulture), 0, Utils.CustomConstantMax);
        } catch {
            // Failed to parse, leave constantValueParsed as null
        }

        using (ImRaii.Disabled(!constantValueParsed.HasValue)) {
            if (ImGui.Button("Set custom value", size)) {
                if (constantValueParsed is { } customValue) {
                    expr = new Constant(customValue);
                    ImGui.CloseCurrentPopup();
                    return true;
                }
            }
        }

        ImGuiEx.SpacedSeparator();

        foreach (var item in Enum.GetValues<MetricType>()) {
            if (item == MetricType.LimitedCap)
                ImGuiEx.SpacedSeparator();

            if (ImGui.Button(item.GetDisplayName(), size)) {
                expr = new Metric(item);
                ImGui.CloseCurrentPopup();
                return true;
            }
        }

        return false;
    }

    private bool DrawNegate(ref bool negate, Vector2 size) {
        var text = negate ? "IS NOT" : "IS";
        if (ImGui.Button(text, size)) {
            negate = !negate;
            return true;
        }

        return false;
    }

    private bool DrawOperator(ref Operator op, Vector2 size) {
        if (ImGui.Button(op.GetDisplayName(), size)) {
            ImGui.OpenPopup(PopupEditOperator);
        }

        using var defaultStyle = ImRaii.DefaultStyle();
        using var popup = ImRaii.Popup(PopupEditOperator);
        if (!popup) return false;

        foreach (var item in Enum.GetValues<Operator>()) {
            if (ImGui.Button(item.GetDisplayName(), size)) {
                op = item;
                ImGui.CloseCurrentPopup();
                return true;
            }
        }

        return false;
    }

    private void DrawRuleOutputs(Rule rule, ref bool changed) {
        ImGui.Spacing();

        var showPanel = rule.ShowPanel;
        if (ImGui.Checkbox("Show Overlay Panel", ref showPanel)) {
            rule.ShowPanel = showPanel;
            changed = true;
        }

        var showPanelConfig = rule.PanelConfig is not null;
        if (ImGui.Checkbox("Customize Overlay Panel", ref showPanelConfig)) {
            if (showPanelConfig) {
                rule.PanelConfig ??= new RulePanelConfig();
            } else {
                rule.PanelConfig = null;
            }
            changed = true;
        }

        if (rule.PanelConfig is not null) {
            DrawRulePanelConfig(rule.PanelConfig, ref changed);
        }

        var showChat = rule.ShowChat;
        if (ImGui.Checkbox("Show Chat Alert", ref showChat)) {
            rule.ShowChat = showChat;
            changed = true;
        }

        var showChatConfig = rule.ChatConfig is not null;
        if (ImGui.Checkbox("Customize Chat Alert", ref showChatConfig)) {
            if (showChatConfig) {
                rule.ChatConfig ??= new RuleChatConfig();
            } else {
                rule.ChatConfig = null;
            }
            changed = true;
        }

        if (rule.ChatConfig is not null) {
            DrawRuleChatConfig(rule.ChatConfig, ref changed);
        }
    }

    private void DrawRulePanelConfig(RulePanelConfig panel, ref bool changed) {
        using var indent = ImRaii.PushIndent();
        var width = 250 * ImGuiHelpers.GlobalScale;

        var quantityTemplate = panel.QuantityTemplate;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableInputText("Quantity Template", Plugin.Config.PanelConfig.QuantityTemplate, ref quantityTemplate)) {
            panel.QuantityTemplate = quantityTemplate;
            changed = true;
        }
        ImGuiEx.TemplateHelp();

        var quantityColor = panel.QuantityColor;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableColorEdit4("Quantity Color", Plugin.Config.PanelConfig.QuantityColor, ref quantityColor)) {
            panel.QuantityColor = quantityColor;
            changed = true;
        }

        var quantityOutlineColor = panel.QuantityOutlineColor;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableColorEdit4("Quantity Outline", Plugin.Config.PanelConfig.QuantityOutlineColor, ref quantityOutlineColor)) {
            panel.QuantityOutlineColor = quantityOutlineColor;
            changed = true;
        }

        var labelTemplate = panel.LabelTemplate;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableInputText("Label Template", Plugin.Config.PanelConfig.LabelTemplate, ref labelTemplate)) {
            panel.LabelTemplate = labelTemplate;
            changed = true;
        }
        ImGuiEx.TemplateHelp();

        var labelColor = panel.LabelColor;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableColorEdit4("Label Color", Plugin.Config.PanelConfig.LabelColor, ref labelColor)) {
            panel.LabelColor = labelColor;
            changed = true;
        }

        var labelOutlineColor = panel.LabelOutlineColor;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableColorEdit4("Label Outline", Plugin.Config.PanelConfig.LabelOutlineColor, ref labelOutlineColor)) {
            panel.LabelOutlineColor = labelOutlineColor;
            changed = true;
        }

        var backdropColor = panel.BackdropColor;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableColorEdit4("Backdrop Color", Plugin.Config.PanelConfig.BackdropColor, ref backdropColor)) {
            panel.BackdropColor = backdropColor;
            changed = true;
        }
    }

    private void DrawRuleChatConfig(RuleChatConfig chat, ref bool changed) {
        using var indent = ImRaii.PushIndent();
        var width = 250 * ImGuiHelpers.GlobalScale;

        var message = chat.Message;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableInputText("Message", Plugin.Config.ChatConfig.MessageTemplate, ref message)) {
            chat.Message = message;
            changed = true;
        }

        var messageColor = chat.MessageColor;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableColorEdit4("Message Color", Plugin.Config.ChatConfig.MessageColor, ref messageColor)) {
            chat.MessageColor = messageColor;
            changed = true;
        }

        var messageOutlineColor = chat.MessageOutlineColor;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableColorEdit4("Message Outline", Plugin.Config.ChatConfig.MessageOutlineColor, ref messageOutlineColor)) {
            chat.MessageOutlineColor = messageOutlineColor;
            changed = true;
        }

        var suffix = chat.Suffix;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableInputText("Suffix", Plugin.Config.ChatConfig.SuffixTemplate, ref suffix)) {
            chat.Suffix = suffix;
            changed = true;
        }

        var suffixColor = chat.SuffixColor;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableColorEdit4("Suffix Color", Plugin.Config.ChatConfig.SuffixColor, ref suffixColor)) {
            chat.SuffixColor = suffixColor;
            changed = true;
        }

        var suffixOutlineColor = chat.SuffixOutlineColor;
        ImGui.PushItemWidth(width);
        if (ImGuiEx.NullableColorEdit4("Suffix Outline", Plugin.Config.ChatConfig.SuffixOutlineColor, ref suffixOutlineColor)) {
            chat.SuffixOutlineColor = suffixOutlineColor;
            changed = true;
        }
    }

    private static string GetCondDisplayName(List<Cond> conditions) {
        return conditions.Count == 0 ? "(None)" : string.Join("  OR  ", conditions.Select(GetCondDisplayName));
    }

    private static string GetCondDisplayName(Cond cond) {
        return $"{cond.Left.GetDisplayName()} {(cond.Negate ? " IS NOT " : " IS ")} {cond.Operator.GetDisplayName()} {cond.Right.GetDisplayName()}";
    }
}
