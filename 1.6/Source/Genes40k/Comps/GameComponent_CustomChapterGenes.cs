using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Genes40k;

public class GameComponent_CustomChapterGenes : GameComponent
{
    private static GameComponent_CustomChapterGenes instance;
    private static Game instanceGame;

    /// <summary>
    /// The current game's component, looked up once per game instead of scanning Game.components per call.
    /// </summary>
    public static GameComponent_CustomChapterGenes Instance
    {
        get
        {
            var game = Current.Game;

            if (game == null)
            {
                return null;
            }

            if (instance == null || instanceGame != game)
            {
                instance = game.GetComponent<GameComponent_CustomChapterGenes>();
                instanceGame = game;
            }

            return instance;
        }
    }

    private List<CustomChapterGene> chapters = [];
    private int nextId = 0;

    public List<CustomChapterGene> Chapters => chapters;

    public GameComponent_CustomChapterGenes(Game game)
    {
    }

    public CustomChapterGene Get(int id)
    {
        if (id < 0)
        {
            return null;
        }

        return chapters.FirstOrDefault(chapter => chapter.id == id);
    }

    public List<CustomChapterGene> DesignsOf(CustomGeneKind kind, bool includeAdHoc = false)
    {
        return chapters.Where(chapter => chapter.Kind == kind && (includeAdHoc || !chapter.adHoc)).ToList();
    }

    public CustomChapterGene Add(CustomChapterGeneTemplateDef template, string name, FlagIconDef flagIconDef, List<ChapterTraitDef> traits, List<CustodesDisciplineLevel> disciplines = null, GeneDef relatedPrimarchGene = null, bool adHoc = false)
    {
        var chapter = new CustomChapterGene
        {
            id = nextId++,
            template = template,
            defName = CustomChapterGeneDefBuilder.NewDefName(template),
            name = name,
            flagIconDef = flagIconDef ?? Genes40kDefOf.BEWH_FlagAquila,
            traits = traits?.ToList() ?? new List<ChapterTraitDef>(),
            disciplines = disciplines?.Where(entry => entry?.def != null && entry.level > 0).Select(entry => new CustodesDisciplineLevel(entry.def, entry.level)).ToList() ?? new List<CustodesDisciplineLevel>(),
            adHoc = adHoc
        };
        chapter.RelatedPrimarchGene = relatedPrimarchGene;
        chapters.Add(chapter);
        chapter.RefreshGeneDef();
        return chapter;
    }

    /// <summary>
    /// The Custodes design with exactly these discipline levels, or a new unnamed one. Reusing the design keeps one
    /// generated GeneDef per level spread so vials gestated from the same spread stack.
    /// </summary>
    public CustomChapterGene GetOrCreateCustodesDesign(List<CustodesDisciplineLevel> levels)
    {
        var existing = chapters.FirstOrDefault(chapter => chapter.Kind == CustomGeneKind.Custodes && chapter.SameDisciplinesAs(levels));

        if (existing != null)
        {
            return existing;
        }

        var template = CustomChapterGeneUtility.TemplateFor(CustomGeneKind.Custodes);
        var name = "BEWH.MankindsFinest.CustomChapter.AdHocPatternName".Translate(CustomChapterGeneUtility.PatternCode(levels));
        return Add(template, name, Genes40kDefOf.BEWH_FlagAquila, null, levels, null, true);
    }

    public CustomChapterGene GetByGeneDef(GeneDef geneDef)
    {
        if (geneDef == null)
        {
            return null;
        }

        return chapters.FirstOrDefault(chapter => chapter.defName == geneDef.defName);
    }

    public bool Remove(CustomChapterGene chapter)
    {
        return chapter != null && !chapter.locked && chapters.Remove(chapter);
    }

    public bool NameTaken(string name, CustomChapterGene except = null)
    {
        return chapters.Any(chapter => chapter != except && !chapter.adHoc && string.Equals(chapter.name, name, System.StringComparison.OrdinalIgnoreCase));
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref chapters, "chapters", LookMode.Deep);
        Scribe_Values.Look(ref nextId, "nextId", 0);

        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        chapters ??= [];
        chapters.RemoveAll(chapter => chapter == null);

        if (chapters.Any() && nextId <= chapters.Max(chapter => chapter.id))
        {
            nextId = chapters.Max(chapter => chapter.id) + 1;
        }

        foreach (var chapter in chapters.Where(chapter => chapter.Kind == CustomGeneKind.Chapter && chapter.RelatedPrimarchGene != null))
        {
            chapter.RefreshGeneDef();
        }
    }
}
