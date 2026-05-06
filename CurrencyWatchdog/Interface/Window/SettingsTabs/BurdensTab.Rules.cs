using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Expressions;
using CurrencyWatchdog.Interface.Utility;
using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace CurrencyWatchdog.Interface.Window.SettingsTabs;

public partial class BurdensTab {
    private const string PopupEditExpression = "currency-watchdog-edit-expression";
    private const string PopupEditOperator = "currency-watchdog-edit-operator";

    private readonly DragDropHelper<Rule> ruleDragDrop = new("RULE");

    private string editingConstant = "";

    private void DrawRulesSection(Burden burden, ref bool changed) {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Text("Rules");

        DrawAddRuleButton(burden, ref changed);
        ImGui.Spacing();
        DrawRules(burden, ref changed);
    }

    private void DrawAddRuleButton(Burden burden, ref bool changed) {
        const FontAwesomeIcon icon = FontAwesomeIcon.BalanceScaleRight;
        const string text = "Add rule";

        var buttonWidth = ImGuiComponents.GetIconButtonWithTextWidth(icon, text);

        ImGui.SameLine();
        ImCursor.X += ImGui.GetContentRegionAvail().X - buttonWidth;

        if (ImGuiComponents.IconButtonWithText(icon, text)) {
            burden.Rules.Add(new Rule {
                Conds = [new SubjectExpression.Cond(new SubjectExpression.Metric(SubjectExpression.MetricType.QuantityHeld), SubjectExpression.Operator.GreaterThanOrEqualTo, new SubjectExpression.Constant(0))],
            });
            changed = true;
        }
    }

    private void DrawRules(Burden burden, ref bool changed) {
        for (var i = 0; i < burden.Rules.Count; i++) {
            DrawRule(burden, i, ref changed);
        }
        ruleDragDrop.EndFrame();
    }

    private void DrawRule(Burden burden, int i, ref bool changed) {
        var rule = burden.Rules[i];
        using var id = ImRaii.PushId($"rule:{i}");
        var hoverId = ImGui.GetID("hover");

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
        using (ImCursor.Excursion()) {
            var buttonWidth = ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.TrashAlt, "");
            var currentPos = headerStartCursor + new Vector2(headerStartAvail.X - buttonWidth - ImGui.GetStyle().ItemSpacing.X, headerExtraPadding * ImGuiHelpers.GlobalScale);
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

        using (var drag = ruleDragDrop.Drag(hoverId, burden.Rules, i)) {
            if (drag) drag.SetSourceName(headerLabel);
        }
        using (var drop = ruleDragDrop.Drop(hoverId, burden.Rules, DragMask.Reorder)) {
            if (drop) {
                burden.Rules.Swap(drop.SourceIndex, i);
                changed = true;
            }
        }

        using (ImRaii.PushId($"ruleButtonsB"))
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0)))
        using (ImCursor.Excursion()) {
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
    }

    private void DrawRuleConditions(Rule rule, ref bool changed) {
        ImGui.Text("Conditions");

        for (var i = 0; i < rule.Conds.Count; i++) {
            DrawCondition(rule, i, ref changed);
        }

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.PlusSquare, "Add condition")) {
            rule.Conds.Add(new SubjectExpression.Cond(new SubjectExpression.Metric(SubjectExpression.MetricType.QuantityHeld), SubjectExpression.Operator.GreaterThanOrEqualTo, new SubjectExpression.Constant(0)));
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
            editingConstant = (expr is SubjectExpression.Constant { Value: var constant } ? constant : 0).ToString(Utils.DecimalDisplayFormat);
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
                    expr = new SubjectExpression.Constant(customValue);
                    ImGui.CloseCurrentPopup();
                    return true;
                }
            }
        }

        ImGuiEx.SpacedSeparator();

        foreach (var item in Enum.GetValues<SubjectExpression.MetricType>()) {
            if (item == SubjectExpression.MetricType.LimitedCap)
                ImGuiEx.SpacedSeparator();

            if (ImGui.Button(item.GetDisplayName(), size)) {
                expr = new SubjectExpression.Metric(item);
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

    private bool DrawOperator(ref SubjectExpression.Operator op, Vector2 size) {
        if (ImGui.Button(op.GetDisplayName(), size)) {
            ImGui.OpenPopup(PopupEditOperator);
        }

        using var defaultStyle = ImRaii.DefaultStyle();
        using var popup = ImRaii.Popup(PopupEditOperator);
        if (!popup) return false;

        foreach (var item in Enum.GetValues<SubjectExpression.Operator>()) {
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

    private static string GetCondDisplayName(List<SubjectExpression.Cond> conditions) {
        return conditions.Count == 0 ? "(None)" : string.Join("  OR  ", conditions.Select(GetCondDisplayName));
    }

    private static string GetCondDisplayName(SubjectExpression.Cond cond) {
        return $"{cond.Left.GetDisplayName()} {(cond.Negate ? " IS NOT " : " IS ")} {cond.Operator.GetDisplayName()} {cond.Right.GetDisplayName()}";
    }
}
