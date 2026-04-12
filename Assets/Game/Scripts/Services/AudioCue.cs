public enum SfxCue
{
    ButtonClick,
    BottleUp,
    BottleDown,
    BottlePull,
    BottleClose,
    Star,
    Equip,
    Purchase,
    Undo,
    AddTube,
    Shuffle,
    ComboHigh,
    Win
}

public enum MusicCue
{
    MainTheme,
}

public static class AudioCueCatalog
{
    public static string GetClipName(SfxCue cue)
    {
        return cue switch
        {
            SfxCue.ButtonClick => "btn_click",
            SfxCue.BottleUp => "Bottle_Up",
            SfxCue.BottleDown => "Bottle_Down",
            // Legacy asset name is Bottle_Full, while gameplay uses this as pull/fill feedback.
            SfxCue.BottlePull => "Bottle_Full",
            SfxCue.BottleClose => "Bottle_Close",
            SfxCue.Star => "size_up",
            SfxCue.Equip => "btn_click",
            SfxCue.Purchase => "btn_click",
            SfxCue.Undo => "size_up5",
            SfxCue.AddTube => "size_up5",
            SfxCue.Shuffle => "size_up5",
            SfxCue.ComboHigh => "size_up",
            SfxCue.Win => "end_win",
            _ => cue.ToString(),
        };
    }

    public static string GetClipName(MusicCue cue)
    {
        return cue switch
        {
            MusicCue.MainTheme => "Sicilian sun",
            _ => cue.ToString(),
        };
    }
}
