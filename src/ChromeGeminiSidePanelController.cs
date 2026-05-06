using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Patterns;
using FlaUI.UIA3;

namespace GeminiForChromeManager;

internal sealed class ChromeGeminiSidePanelController : IDisposable
{
    private readonly UIA3Automation automation = new();

    public bool TryOpenSidePanel()
    {
        if (!TryGetChromeWindow(preferSidePanel: false, out AutomationElement chromeWindow))
        {
            AppLog.Info("Chrome Gemini side panel was not opened because no Chrome window was available to UI Automation.");
            return false;
        }

        try
        {
            AutomationElement[] controls = chromeWindow.FindAllDescendants();

            if (IsSidePanelOpen(controls))
            {
                AppLog.Info("Chrome Gemini side panel is already open.");
                return true;
            }

            AutomationElement? askGeminiButton = controls.FirstOrDefault(control =>
                IsVisibleButtonLike(control) &&
                IsOpenGeminiButtonName(GetName(control)));

            if (!TryInvokeOrClick(askGeminiButton))
            {
                AppLog.Info("Chrome Gemini side panel open button was not found through UI Automation.");
                return false;
            }

            AppLog.Info($"Chrome Gemini side panel opened through \"{GetName(askGeminiButton!)}\" button.");
            return true;
        }
        catch (Exception exception)
        {
            AppLog.Error("Failed while opening Chrome Gemini side panel through UI Automation.", exception);
            return false;
        }
    }

    public bool TryApplyReasoningLevel(GeminiReasoningLevel reasoningLevel)
    {
        if (reasoningLevel == GeminiReasoningLevel.Auto)
        {
            AppLog.Info("Reasoning level is Auto; no side panel reasoning selection needed.");
            return true;
        }

        if (!TryGetChromeWindow(preferSidePanel: true, out AutomationElement chromeWindow))
        {
            AppLog.Info("Reasoning level was not applied because no Chrome window was available to UI Automation.");
            return false;
        }

        string targetName = reasoningLevel.ToString();

        try
        {
            AutomationElement[] controls = chromeWindow.FindAllDescendants();
            AutomationElement? directChoice = FindNamedControl(controls, targetName);

            if (TryInvokeOrClick(directChoice))
            {
                AppLog.Info($"Reasoning level \"{targetName}\" selected directly in Chrome Gemini side panel.");
                return true;
            }

            AutomationElement? reasoningButton = FindReasoningButton(controls);

            if (!TryInvokeOrClick(reasoningButton))
            {
                AppLog.Info($"Reasoning level \"{targetName}\" was not applied because no reasoning control was found.");
                return false;
            }

            Thread.Sleep(500);
            controls = chromeWindow.FindAllDescendants();
            directChoice = FindNamedControl(controls, targetName);

            if (TryInvokeOrClick(directChoice))
            {
                AppLog.Info($"Reasoning level \"{targetName}\" selected after opening reasoning menu.");
                return true;
            }

            AppLog.Info($"Reasoning level \"{targetName}\" was not found after opening reasoning menu.");
            return false;
        }
        catch (Exception exception)
        {
            AppLog.Error($"Failed while applying reasoning level \"{targetName}\" through UI Automation.", exception);
            return false;
        }
    }

    public bool TryFocusPromptBox()
    {
        DateTime deadline = DateTime.Now.AddSeconds(10);

        while (DateTime.Now < deadline)
        {
            if (!TryGetChromeWindow(preferSidePanel: true, out AutomationElement chromeWindow))
            {
                Thread.Sleep(300);
                continue;
            }

            try
            {
                AutomationElement[] controls = chromeWindow.FindAllDescendants();
                AutomationElement? promptBox = FindPromptBox(controls);

                if (promptBox is null)
                {
                    Thread.Sleep(300);
                    continue;
                }

                promptBox.Focus();
                Thread.Sleep(150);
                promptBox.Click();

                AppLog.Info($"Prompt box focused in Chrome Gemini side panel. Bounds={FormatRectangle(promptBox.BoundingRectangle)}.");
                return true;
            }
            catch (Exception exception)
            {
                AppLog.Error("Failed while focusing Chrome Gemini side panel prompt box through UI Automation.", exception);
                return false;
            }
        }

        AppLog.Info("Prompt box was not found in Chrome Gemini side panel before timeout.");
        return false;
    }

    public void Dispose()
    {
        automation.Dispose();
    }

    private bool TryGetChromeWindow(bool preferSidePanel, out AutomationElement chromeWindow)
    {
        chromeWindow = null!;
        AutomationElement? fallback = null;

        foreach (IntPtr chromeWindowHandle in ChromeWindowLocator.GetChromeWindowHandles())
        {
            AutomationElement candidate = automation.FromHandle(chromeWindowHandle);

            if (candidate is null)
            {
                continue;
            }

            AutomationElement[] controls = candidate.FindAllDescendants();

            if (preferSidePanel && IsSidePanelOpen(controls))
            {
                chromeWindow = candidate;
                return true;
            }

            if (!preferSidePanel && ContainsAskGeminiButtonOrSidePanel(controls))
            {
                chromeWindow = candidate;
                return true;
            }

            fallback ??= candidate;
        }

        if (fallback is not null)
        {
            chromeWindow = fallback;
            return true;
        }

        return false;
    }

    private AutomationElement? FindReasoningButton(AutomationElement[] controls)
    {
        return controls.FirstOrDefault(control =>
            IsVisibleButtonLike(control) &&
            NameContainsAny(control, "Open mode picker", "Reasoning", "Thinking", "Fast", "Pro", "Auto"));
    }

    private static bool IsSidePanelOpen(AutomationElement[] controls)
    {
        return controls.Any(control =>
            IsVisible(control) &&
            (GetName(control).Contains("Gemini Chrome", StringComparison.OrdinalIgnoreCase) ||
             GetName(control).Contains("Enter a prompt for Gemini", StringComparison.OrdinalIgnoreCase) ||
             GetName(control).Contains("Close Gemini in Chrome", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool ContainsAskGeminiButtonOrSidePanel(AutomationElement[] controls)
    {
        return IsSidePanelOpen(controls) ||
            controls.Any(control =>
                IsVisibleButtonLike(control) &&
                IsOpenGeminiButtonName(GetName(control)));
    }

    private AutomationElement? FindNamedControl(AutomationElement[] controls, string name)
    {
        return controls.FirstOrDefault(control =>
            IsVisibleButtonLike(control) &&
            NameStartsWithWord(GetName(control), name));
    }

    private AutomationElement? FindPromptBox(AutomationElement[] controls)
    {
        AutomationElement[] candidates = controls
            .Where(control => IsVisible(control) && IsPromptLike(control))
            .OrderByDescending(control => control.BoundingRectangle.Right)
            .ThenByDescending(control => control.BoundingRectangle.Bottom)
            .ToArray();

        return candidates.FirstOrDefault();
    }

    private static bool IsPromptLike(AutomationElement control)
    {
        ControlType controlType = control.ControlType;
        string name = GetName(control);

        return controlType == ControlType.Edit &&
            (name.Contains("Enter a prompt for Gemini", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("Ask Gemini", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("Ask anything", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("Prompt", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsVisibleButtonLike(AutomationElement control)
    {
        ControlType controlType = control.ControlType;
        return IsVisible(control) &&
            (controlType == ControlType.Button ||
             controlType == ControlType.MenuItem ||
             controlType == ControlType.ListItem ||
             controlType == ControlType.Text);
    }

    private static bool IsVisible(AutomationElement control)
    {
        return !control.Properties.IsOffscreen.ValueOrDefault &&
            control.BoundingRectangle is { IsEmpty: false, Width: > 0, Height: > 0 };
    }

    private static bool NameContainsAny(AutomationElement control, params string[] values)
    {
        string name = GetName(control);
        return values.Any(value => name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetName(AutomationElement control)
    {
        return control.Properties.Name.ValueOrDefault ?? string.Empty;
    }

    private static bool NameStartsWithWord(string actualName, string expectedWord)
    {
        if (actualName.Equals(expectedWord, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return actualName.StartsWith(expectedWord + " ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOpenGeminiButtonName(string name)
    {
        return name.Contains("Ask Gemini", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Open Gemini in Chrome", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryInvokeOrClick(AutomationElement? control)
    {
        if (control is null)
        {
            return false;
        }

        if (control.Patterns.Invoke.IsSupported)
        {
            IInvokePattern invokePattern = control.Patterns.Invoke.Pattern;
            invokePattern.Invoke();
            return true;
        }

        control.Click();
        return true;
    }

    private static string FormatRectangle(Rectangle rectangle)
    {
        return $"{rectangle.Left},{rectangle.Top},{rectangle.Right},{rectangle.Bottom}";
    }
}
