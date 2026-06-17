namespace ChessOnlinePersistence;

public sealed class OnlineStoreOptions
{
    public string Provider { get; set; } = "json";
    public string StorePath { get; set; } = "";
    public bool AutoCreate { get; set; } = true;
    public bool RestoreRoomsOnStartup { get; set; } = false;

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Provider))
        {
            Provider = "json";
        }
        if (string.IsNullOrWhiteSpace(StorePath))
        {
            StorePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Chess3D",
                "online-dev",
                "chess3d-online-store.json");
        }
    }
}

