using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using RefinedGem.Services;
using RefinedGem.UI;

namespace RefinedGem.Patches;

[HarmonyPatch(typeof(NCardLibrary), "_Ready")]
internal static class CardLibraryReadyPatch
{
    private const string RefinedPoolFilterStableId = "refined_pool";
    private const string FilterIconPath = "res://assets/refined_gem_relic.png";
    private const float FilterSize = 64f;
    private const float ImageSize = 56f;

    [HarmonyPostfix]
    private static void Postfix(
        NCardLibrary __instance,
        Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters)
    {
        EnsureRefinedPoolFilter(__instance, ____poolFilters);
        Callable.From(() => CardLibrarySelectionController.Attach(__instance)).CallDeferred();
    }

    private static void EnsureRefinedPoolFilter(
        NCardLibrary library,
        Dictionary<NCardPoolFilter, Func<CardModel, bool>> poolFilters)
    {
        try
        {
            if (poolFilters.Keys.Any(filter =>
                    string.Equals(filter.Name, RefinedPoolFilterStableId, StringComparison.Ordinal)))
                return;

            var colorless = AccessTools.Field(typeof(NCardLibrary), "_colorlessFilter")
                ?.GetValue(library) as NCardPoolFilter;
            if (colorless is null || !GodotObject.IsInstanceValid(colorless))
            {
                RefinedGemEntry.Logger.Warn("[RefinedGem] Colorless Card Library filter missing; refined_pool not added.");
                return;
            }

            var parent = colorless.GetParent();
            if (parent is null)
            {
                RefinedGemEntry.Logger.Warn("[RefinedGem] Card Library filter parent missing; refined_pool not added.");
                return;
            }

            var referenceMat = colorless.GetNodeOrNull<Control>("Image")?.Material as ShaderMaterial;
            var filter = CreateFilter(referenceMat);
            parent.AddChild(filter);
            parent.MoveChild(filter, colorless.GetIndex() + 1);

            var updateMethod = AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter");
            var updateDelegate = AccessTools.MethodDelegate<Action<NCardPoolFilter>>(updateMethod, library);
            filter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From(updateDelegate));
            poolFilters[filter] = RefinedPoolService.ContainsCard;
        }
        catch (Exception ex)
        {
            RefinedGemEntry.Logger.Warn($"[RefinedGem] Failed to create refined_pool filter: {ex.Message}");
        }
    }

    private static NCardPoolFilter CreateFilter(ShaderMaterial? referenceMat)
    {
        var filter = new NCardPoolFilter
        {
            Name = RefinedPoolFilterStableId,
            CustomMinimumSize = new Vector2(FilterSize, FilterSize),
            Size = new Vector2(FilterSize, FilterSize),
            FocusMode = Control.FocusModeEnum.All,
        };

        AddFilterImage(filter, referenceMat, TryLoadTexture(FilterIconPath));
        AddSelectionReticle(filter);
        return filter;
    }

    private static void AddFilterImage(NCardPoolFilter filter, ShaderMaterial? referenceMat, Texture2D? texture)
    {
        var image = new TextureRect
        {
            Name = "Image",
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(ImageSize, ImageSize),
            Size = new Vector2(ImageSize, ImageSize),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            PivotOffset = new Vector2(ImageSize / 2f, ImageSize / 2f),
            Position = new Vector2((FilterSize - ImageSize) / 2f, (FilterSize - ImageSize) / 2f),
        };

        if (referenceMat is not null)
            image.Material = (ShaderMaterial)referenceMat.Duplicate();

        filter.AddChild(image);
    }

    private static void AddSelectionReticle(NCardPoolFilter filter)
    {
        try
        {
            var scenePath = SceneHelper.GetScenePath("ui/selection_reticle");
            var reticle = PreloadManager.Cache.GetScene(scenePath).Instantiate<NSelectionReticle>();
            reticle.Name = "SelectionReticle";
            reticle.UniqueNameInOwner = true;
            reticle.MouseFilter = Control.MouseFilterEnum.Ignore;
            filter.AddChild(reticle);
            reticle.Owner = filter;
        }
        catch (Exception ex)
        {
            RefinedGemEntry.Logger.Warn($"[RefinedGem] Could not attach selection reticle to refined_pool filter: {ex.Message}");
        }
    }

    private static Texture2D? TryLoadTexture(string path)
    {
        if (!ResourceLoader.Exists(path))
            return null;

        return ResourceLoader.Load<Texture2D>(path);
    }
}

[HarmonyPatch(typeof(NCardLibrary), "OnSubmenuOpened")]
internal static class CardLibraryOpenedPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCardLibrary __instance) =>
        Callable.From(() => CardLibrarySelectionController.Attach(__instance)).CallDeferred();
}

[HarmonyPatch(typeof(NCardLibrary), "UpdateCardPoolFilter")]
internal static class CardLibraryPoolFilterPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCardLibrary __instance, NCardPoolFilter filter) =>
        CardLibrarySelectionController.NotifyPoolFilterChanged(__instance, filter);
}

[HarmonyPatch(typeof(NCardLibrary), "ShowCardDetail")]
internal static class CardLibraryShowCardDetailPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NCardHolder holder)
    {
        if (holder.CardModel is not CardModel card)
            return true;

        if (CardLibrarySelectionController.EditModeEnabled)
        {
            var wasInPool = RefinedPoolService.ContainsCard(card);
            if (!CardLibrarySelectionController.TryToggleCard(card))
                return false;

            CardLibraryFeedback.Show(
                wasInPool
                    ? RefinedGemUiText.Get("refined_gem.ui.card_removed")
                    : RefinedGemUiText.Get("refined_gem.ui.card_added"));

            return false;
        }

        if (!CardLibrarySelectionController.IsRefinedPoolViewActive)
            return true;

        if (!CardLibrarySelectionController.TryRemoveCard(card))
            return true;

        CardLibraryFeedback.Show(RefinedGemUiText.Get("refined_gem.ui.card_removed"));
        return false;
    }
}
