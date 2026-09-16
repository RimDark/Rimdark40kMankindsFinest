using System.Collections.Generic;
using Core40k;
using Verse;

namespace Genes40k;

/// <summary>
/// Marks a GeneDef generated at runtime for a player-designed gene-seed and records what it was built from:
/// the line it belongs to, the traits it merged and the grants Gene_CustomChapter executes itself.
/// </summary>
public class DefModExtension_CustomChapterGenerated : DefModExtension
{
    public string chapterDefName;
    public CustomGeneKind kind = CustomGeneKind.Chapter;
    public List<ChapterTraitDef> traits = [];
    public List<DefModExtension_AddRandomGeneByWeight> geneGrants = [];
    public List<DefModExtension_AddRandomTraitByWeight> traitGrants = [];
    public List<DefModExtension_GivesVEFAbility> vefAbilityGrants = [];
    public List<ChapterTraitDef> workerTraits = [];
    public bool twinLinked;
}
