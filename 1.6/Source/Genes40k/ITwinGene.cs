using Verse;

namespace Genes40k;

/// <summary>
/// A gene that can hold a twin link (Alpharius/Omegon). Implemented by the shipped Gene_TwinConnected and by
/// Gene_CustomChapter when its design carries a twin-linked trait.
/// </summary>
public interface ITwinGene
{
    Pawn Twin { get; }
    bool TwinCapable { get; }
    void SetTwin(Pawn twin);
}
