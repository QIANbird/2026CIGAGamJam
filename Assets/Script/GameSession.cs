public static class GameSession
{
    public static string PlayerId { get; private set; } = string.Empty;
    public static bool RequiresPlayerNameInput { get; private set; } = true;
    public static bool StartScreenCompleted { get; private set; }

    public static void PrepareForNameInput()
    {
        PlayerId = string.Empty;
        RequiresPlayerNameInput = true;
        StartScreenCompleted = false;
    }

    public static void MarkStartScreenCompleted()
    {
        StartScreenCompleted = true;
    }

    public static bool TrySetPlayerId(string playerId)
    {
        string normalizedId = playerId == null ? string.Empty : playerId.Trim();

        if (string.IsNullOrEmpty(normalizedId))
        {
            return false;
        }

        PlayerId = normalizedId;
        RequiresPlayerNameInput = false;
        return true;
    }
}
