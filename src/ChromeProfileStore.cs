using System.Text.Json;

namespace GeminiForChromeManager;

internal sealed record ChromeProfileInfo(string DirectoryName, string DisplayName)
{
    public string MenuLabel =>
        DirectoryName.Equals(AppSettingsDefaults.ChromeProfileDirectory, StringComparison.OrdinalIgnoreCase)
            ? AppSettingsDefaults.ChromeProfileDirectory
            :
        DirectoryName.Equals(DisplayName, StringComparison.OrdinalIgnoreCase)
            ? DisplayName
            : $"{DisplayName} ({DirectoryName})";
}

internal static class ChromeProfileStore
{
    private static string ChromeUserDataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Google",
            "Chrome",
            "User Data");

    private static string LocalStatePath => Path.Combine(ChromeUserDataDirectory, "Local State");

    public static IReadOnlyList<ChromeProfileInfo> DetectProfiles()
    {
        List<ChromeProfileInfo> profiles = [];

        try
        {
            if (File.Exists(LocalStatePath))
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(LocalStatePath));

                if (document.RootElement.TryGetProperty("profile", out JsonElement profileElement) &&
                    profileElement.TryGetProperty("info_cache", out JsonElement infoCacheElement) &&
                    infoCacheElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty profileProperty in infoCacheElement.EnumerateObject())
                    {
                        string directoryName = profileProperty.Name;
                        string displayName = directoryName;

                        if (profileProperty.Value.TryGetProperty("name", out JsonElement nameElement) &&
                            nameElement.ValueKind == JsonValueKind.String &&
                            !string.IsNullOrWhiteSpace(nameElement.GetString()))
                        {
                            displayName = nameElement.GetString()!;
                        }

                        profiles.Add(new ChromeProfileInfo(directoryName, displayName));
                    }
                }
            }
        }
        catch (Exception exception)
        {
            AppLog.Error("Failed to auto-detect Chrome profiles from Local State.", exception);
        }

        if (profiles.All(profile => !profile.DirectoryName.Equals(AppSettingsDefaults.ChromeProfileDirectory, StringComparison.OrdinalIgnoreCase)))
        {
            profiles.Add(new ChromeProfileInfo(AppSettingsDefaults.ChromeProfileDirectory, AppSettingsDefaults.ChromeProfileDirectory));
        }

        return profiles
            .OrderBy(profile => profile.DirectoryName.Equals(AppSettingsDefaults.ChromeProfileDirectory, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(profile => profile.DisplayName)
            .ToList();
    }

    public static string NormalizeSelectedProfile(string? selectedProfile)
    {
        if (string.IsNullOrWhiteSpace(selectedProfile))
        {
            return AppSettingsDefaults.ChromeProfileDirectory;
        }

        return selectedProfile.Trim();
    }
}
