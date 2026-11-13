namespace SF.App.Services.Updates;

/// <summary>
/// Where an update is: downloading (with the percentage done) or replacing the files (too late to cancel).
/// </summary>
internal sealed class UpdateProgress
{
    public UpdateStage Stage { get; set; }

    /// <summary>
    /// While downloading: 0 to 100, or -1 when the download's size is not known.
    /// </summary>
    public int Percent { get; set; }
}

internal enum UpdateStage
{
    Downloading,
    Replacing,
}
