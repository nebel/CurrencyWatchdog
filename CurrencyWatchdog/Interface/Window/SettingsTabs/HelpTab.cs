using CurrencyWatchdog.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

namespace CurrencyWatchdog.Interface.Window.SettingsTabs;

public class HelpTab {
    public void Draw() {
        using var child = ImRaii.Child("helpTabScrollChild");
        if (!child) return;

        ImGuiEx.ConfigTopHeader("Help topics");

        if (ImGui.CollapsingHeader("Burdens, subjects, rules and conditions")) {
            ImGuiEx.ConfigHeader("Burdens");

            ImGui.TextWrapped("A burden is a group of tracked items with a shared configuration. It consists of subjects, which are the items " +
                              "monitored, and rules which are used to determine whether or not an alert is shown.");

            ImGuiEx.ConfigHeader("Subjects");

            ImGui.TextWrapped("A subject is a currency or non-currency item that is tracked by a burden.");

            ImGui.TextWrapped("In addition to the usual currency and non-currency subjects, there are special subjects (added from the \"Special\" tab of " +
                              "the Subject Selector window) which have additional properties:\n" +
                              " -  Grand Company Seals use the correct seal type based on your character's Grand Company and calculate your seal cap based " +
                              "on your character's Grand Company Rank.\n" +
                              " -  Tomestones take into account the currently available Tomestone types which change over time.\n" +
                              " -  Scrips take into account the currently available scrip types which change with each expansion.\n");

            ImGuiEx.ConfigHeader("Rules");

            ImGui.TextWrapped("Rules are used to determine whether overlay panels and/or chat alerts will be shown for items in a burden. Only one rule " +
                              "can be matched at a time. Rules are evaluated from the top down, and the first rule that matches will be used to determine " +
                              "the alert state for items in that burden.");

            ImGuiEx.ConfigHeader("Conditions");

            ImGui.TextWrapped("A rule may have multiple conditions. If any condition in a rule matches, the rule will match.");
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Template strings")) {
            ImGui.TextWrapped("Template strings are text labels with support for special placeholder values.");

            ImGuiEx.ConfigHeader("Placeholders");

            ImGui.TextWrapped("Template strings accept special placeholders which are automatically replaced by certain values related to the subject being " +
                              "tracked.");

            ImGui.TextWrapped("The following text-based placeholders are available:\n" +
                              " -  {n} = Name\n" +
                              " -  {a} = Alias (or name if none is set)");

            ImGui.TextWrapped("The following numeric placeholders are available:\n" +
                              " -  {c} = The currency cap (or stack size for non-currency items)\n" +
                              " -  {h} = The quantity of items currently held\n" +
                              " -  {p} = The quantity of items currently held as a percentage of the cap\n" +
                              " -  {m} = The quantity of items currently missing (will be 0 if over the stack size for non-currency items)");

            ImGui.TextWrapped("The following additional numeric placeholders are available for use with limited tomestones, which have a standard cap and " +
                              "a limited (weekly) cap:\n" +
                              " -  {C} = Same as {c} but for the limited cap\n" +
                              " -  {H} = Same as {h} but for the limited cap\n" +
                              " -  {P} = Same as {p} but for the limited cap\n" +
                              " -  {M} = Same as {m} but for the limited cap\n");

            ImGuiEx.ConfigHeader("Format strings");

            ImGui.TextWrapped("Numeric placeholders (c, h, p and m) support an optional format string which changes how the number is displayed. The " +
                              "format string must be placed after a \":\" (colon) in the curly brackets, such that \"{h:N0}\" will print the \"{h}\" " +
                              "placeholder using the format string \"N0\".");

            ImGui.TextWrapped("For full details, see the following pages:");
            using (ImRaii.PushIndent()) {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Globe, "Standard numeric format strings")) {
                    Util.OpenLink("https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings");
                }
                ImGui.SameLine();
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Globe, "Custom numeric format strings")) {
                    Util.OpenLink("https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-numeric-format-strings");
                }
            }

            ImGui.TextWrapped("In addition to the above standards, the following non-standard additions are available:\n" +
                              " -  If a format string starts with ^, the number will be rounded up to the nearest integer before formatting.\n" +
                              " -  If a format string starts with _, the number will be rounded down to the nearest integer before formatting.\n" +
                              " -  A special format specifier Z (or z) is available which will print the number as an abbreviated decimal for numbers over " +
                              "1,000. A number can also be provided to choose how many digits are shown, e.g. Z1 or z4. The default is 3 digits.");

            ImGui.TextWrapped("Examples:\n" +
                              " -  {h:N0} will print the held number with commas and no decimal places (e.g. 5,263)\n" +
                              " -  {h:Z} will print the held number as an abbreviated decimal for numbers over 1,000 (e.g. 5.26K)\n" +
                              " -  {p:0.000}% will print the percentage held up to 3 decimal places (e.g. 8.315%)");

            ImGui.TextWrapped("If no format string is provided, numbers are displayed rounded down with no decimals and no commas.");
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Definition of \"cap\"")) {
            ImGui.TextWrapped("For currency items, the cap is the maximum number of such items that can be held at once on a single character (not " +
                              "including retainers). Generally speaking it should not be possible for \"Held\" to be greater than \"Cap\" for currency " +
                              "items.");

            ImGui.TextWrapped("For non-currency items which can be split and stacked in inventory slots, the concept of an actual cap makes less sense, " +
                              "and so the maximum stack size for a single inventory slot is considered to be the \"Cap\" for such items. Therefore it is " +
                              "possible for \"Held\" to be greater than \"Cap\" for non-currency items.");

            ImGui.TextWrapped("If you want to use a different \"Cap\" for non-currency items, either enter a custom value in the rule or click the " +
                              "\"Customize\" quill icon next to a subject to set a custom cap.");
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Inputting exact values")) {
            ImGui.TextWrapped("When modifying settings, if a numeric input control is a slider and getting the exact value you desire is difficult, " +
                              "double-clicking or holding Control and clicking will often allow you to input an exact value manually (and values outside" +
                              "the default slider constraints may be allowed to a limited extent).");
        }
    }
}
