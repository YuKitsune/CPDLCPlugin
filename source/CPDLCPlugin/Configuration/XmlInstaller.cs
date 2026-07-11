using System.IO;

namespace CPDLCPlugin.Configuration;

public static class XmlInstaller
{
    const string BackupSuffix = ".cpdlcplugin.bak";
    const string InstalledMarker = "CPDLCPLUGIN_CPDLCSTATUS";

    // Replace the CPDLC/WAKETURBCAT/FIELD18RMK block because the new layout inserts CPDLCSTATUS
    // items before WAKETURBCAT and appends TEXTSTATUS items after FIELD18RMK.
    const string LabelsOldBlock =
        """
              <Item Type="LABEL_ITEM_CPDLC" Colour="" BackgroundColour="CPDLCDownlink" LeftClick="Label_CPDLC_Menu" MiddleClick="Label_CPDLC_Message_Toggle" RightClick="Label_CPDLC_Editor" />
              <Item Type="LABEL_ITEM_WAKETURBCAT" Colour="" LeftClick="" MiddleClick="" RightClick="" />
              <Item Type="LABEL_ITEM_FIELD18RMK" Colour="" LeftClick="Label_Field18_Popup" MiddleClick="" RightClick="Label_Toggle_1" />
        """;

    const string LabelsNewBlock =
        """
              <!-- CPDLC Plugin: CPDLC Status -->
              <Item Type="CPDLCPLUGIN_CPDLCSTATUS" />
              <Item Type="CPDLCPLUGIN_CPDLCSTATUS_BG" BackgroundColour="Custom" />

              <Item Type="LABEL_ITEM_WAKETURBCAT" Colour="" LeftClick="" MiddleClick="" RightClick="" />
              <Item Type="LABEL_ITEM_FIELD18RMK" Colour="" LeftClick="Label_Field18_Popup" MiddleClick="" RightClick="Label_Toggle_1" />

              <!-- CPDLC Plugin: Text Status -->
              <Item Type="CPDLCPLUGIN_TEXTSTATUS" />
              <Item Type="CPDLCPLUGIN_TEXTSTATUS_BG" BackgroundColour="Custom" />
        """;

    const string StripsOldItem =
        """
        <StripItem Type="CPDLCStatus" LeftClick="Label_CPDLC_Editor" MinLength="1" />
        """;

    const string StripsNewItem =
        """
                                  <!-- CPDLC Plugin: CPDLC Status -->
                                  <StripItem Type="CPDLCPLUGIN_CPDLCSTATUS" MinLength="1" />

                                  <!-- CPDLC Plugin: Text Status -->
                                  <StripItem Type="CPDLCPLUGIN_TEXTSTATUS" />
        """;

    public static InstallationStatus GetStatus()
    {
        if (!TryFindConfigFiles(out var labels, out var strips, out var error))
            return InstallationStatus.Error(error!);

        var labelsInstalled = File.Exists(labels) && File.ReadAllText(labels).Contains(InstalledMarker);
        var stripsInstalled = File.Exists(strips) && File.ReadAllText(strips).Contains(InstalledMarker);

        if (labelsInstalled && stripsInstalled)
            return InstallationStatus.Installed();

        return InstallationStatus.NotInstalled();
    }

    public static InstallResult Install()
    {
        if (!TryFindConfigFiles(out var labels, out var strips, out var error))
            return InstallResult.Failed(error!);

        try
        {
            PatchFile(labels, LabelsOldBlock, LabelsNewBlock);
            PatchFile(strips, StripsOldItem, StripsNewItem);
            return InstallResult.Success();
        }
        catch (InstallException ex)
        {
            return InstallResult.Failed(ex.Message);
        }
    }

    public static InstallResult Uninstall()
    {
        if (!TryFindConfigFiles(out var labels, out var strips, out var error))
            return InstallResult.Failed(error!);

        try
        {
            RestoreFile(labels);
            RestoreFile(strips);
            return InstallResult.Success();
        }
        catch (InstallException ex)
        {
            return InstallResult.Failed(ex.Message);
        }
    }

    static void PatchFile(string filePath, string oldText, string newText)
    {
        if (!File.Exists(filePath))
            throw new InstallException($"File not found: {filePath}");

        var backupPath = filePath + BackupSuffix;

        // If a backup exists, restore from it as the base to avoid stacking patches.
        // Otherwise, create a backup from the current file.
        string content;
        if (File.Exists(backupPath))
        {
            content = File.ReadAllText(backupPath);
        }
        else
        {
            content = File.ReadAllText(filePath);
            File.WriteAllText(backupPath, content);
        }

        if (!content.Contains(oldText))
        {
            throw new InstallException(
                $"Could not find the expected content in {Path.GetFileName(filePath)}. " +
                "The file may have been customised. Please apply the changes manually.");
        }

        // Replace all occurrences (Labels.xml has both a Normal and Extended label block).
        var patched = content.Replace(oldText, newText);

        File.WriteAllText(filePath, patched);
    }

    static void RestoreFile(string filePath)
    {
        var backupPath = filePath + BackupSuffix;
        if (!File.Exists(backupPath))
        {
            throw new InstallException(
                $"No backup found for {Path.GetFileName(filePath)}. " +
                "The plugin's label & strip items must be removed manually.");
        }

        File.Copy(backupPath, filePath, overwrite: true);
        File.Delete(backupPath);
    }

    static bool TryFindConfigFiles(out string labelsPath, out string stripsPath, out string? error)
    {
        labelsPath = string.Empty;
        stripsPath = string.Empty;
        error = null;

        if (!ProfileDirectoryResolver.TryGetProfileDirectory(out var profileDir) || profileDir is null)
        {
            error = "Profile directory could not be resolved.";
            return false;
        }

        labelsPath = Path.Combine(profileDir.FullName, "Labels.xml");
        stripsPath = Path.Combine(profileDir.FullName, "Strips.xml");
        return true;
    }

    class InstallException(string message) : Exception(message);
}

public readonly struct InstallationStatus
{
    public bool IsInstalled { get; }
    public string? ErrorMessage { get; }

    InstallationStatus(bool isInstalled, string? errorMessage)
    {
        IsInstalled = isInstalled;
        ErrorMessage = errorMessage;
    }

    public static InstallationStatus Installed() => new(true, null);
    public static InstallationStatus NotInstalled() => new(false, null);
    public static InstallationStatus Error(string errorMessage) => new(false, errorMessage);
}

public readonly struct InstallResult
{
    public bool Succeeded { get; }
    public string? ErrorMessage { get; }

    InstallResult(bool succeeded, string? errorMessage)
    {
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
    }

    public static InstallResult Success() => new(true, null);
    public static InstallResult Failed(string errorMessage) => new(false, errorMessage);
}
