/// <summary>Temporarily bypasses the grayscale post effect while 2D cutscene video is playing.</summary>
public static class ENVCinematicScreen
{
    public static int ActiveSequences { get; private set; }
    public static void Begin() { ActiveSequences++; }
    public static void End() { ActiveSequences = System.Math.Max(0, ActiveSequences - 1); }
}
