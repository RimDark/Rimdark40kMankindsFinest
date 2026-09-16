namespace Genes40k;

/// <summary>
/// Which line a custom gene design or a trait option belongs to. Any is only meaningful on ChapterTraitDef and
/// makes the trait selectable in every editor.
/// </summary>
public enum CustomGeneKind
{
    Chapter,
    Primarch,
    Custodes,
    Any
}
