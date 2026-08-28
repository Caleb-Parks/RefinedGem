using System.Collections;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using HarmonyLib;
using RefinedGem.Services;

namespace RefinedGem.UI;

public static class CardLibrarySelectionController
{
    private const string RefinedPoolFilterStableId = "refined_pool";

    private static readonly string[] VanillaPoolFilterFields =
    [
        "_ironcladFilter",
        "_silentFilter",
        "_defectFilter",
        "_regentFilter",
        "_necrobinderFilter",
        "_colorlessFilter",
        "_ancientsFilter",
        "_miscPoolFilter",
    ];

    private static NCardLibrary? _library;
    private static NLibraryStatTickbox? _editModeToggle;
    private static NCardPoolFilter? _refinedPoolFilter;

    public static bool EditModeEnabled =>
        _editModeToggle is not null
        && GodotObject.IsInstanceValid(_editModeToggle)
        && _editModeToggle.IsTicked;

    public static bool IsRefinedPoolViewActive =>
        _refinedPoolFilter is not null
        && GodotObject.IsInstanceValid(_refinedPoolFilter)
        && _refinedPoolFilter.IsSelected;

    public static void Attach(NCardLibrary library)
    {
        try
        {
            if (_library is not null
                && GodotObject.IsInstanceValid(_library)
                && _editModeToggle is not null
                && GodotObject.IsInstanceValid(_editModeToggle))
                return;

            Detach();

            var template = ResolveTickboxTemplate(library);
            if (template is null)
            {
                RefinedGemEntry.Logger.Warn("[RefinedGem] Could not find a tickbox template; Edit Refined Pool toggle not added.");
                return;
            }

            var anchor = ResolveAnchor(library, template);
            if (anchor is null)
            {
                RefinedGemEntry.Logger.Warn("[RefinedGem] Could not find a Card Library anchor; Edit Refined Pool toggle not added.");
                return;
            }

            var parent = anchor.GetParent();
            if (parent is null)
            {
                RefinedGemEntry.Logger.Warn("[RefinedGem] Anchor has no parent; Edit Refined Pool toggle not added.");
                return;
            }

            _library = library;
            _editModeToggle = (NLibraryStatTickbox)template.Duplicate();
            _editModeToggle.Name = "RefinedGemEditPoolToggle";
            _editModeToggle.Visible = true;

            parent.AddChild(_editModeToggle);
            parent.MoveChild(_editModeToggle, anchor.GetIndex() + 1);

            Callable.From(FinalizeToggle).CallDeferred();
            Callable.From(() => NotifyPoolFilterChanged(library)).CallDeferred();
        }
        catch (Exception ex)
        {
            RefinedGemEntry.Logger.Warn($"[RefinedGem] Failed to add Edit Refined Pool toggle: {ex.Message}");
            Detach();
        }
    }

    public static void NotifyPoolFilterChanged(NCardLibrary library, NCardPoolFilter? filter = null)
    {
        if (filter is not null && IsRefinedPoolFilter(library, filter))
            _refinedPoolFilter = filter;
        else if (_refinedPoolFilter is null || !GodotObject.IsInstanceValid(_refinedPoolFilter))
            _refinedPoolFilter = ResolveRefinedPoolFilter(library);

        if (IsRefinedPoolViewActive)
            RebindRefinedPoolFilterPredicate();
    }

    public static bool TryToggleCard(CardModel card)
    {
        if (!EditModeEnabled)
            return false;

        RefinedPoolService.ToggleCard(card);
        RefreshAfterPoolChange();
        return true;
    }

    public static bool TryRemoveCard(CardModel card)
    {
        if (!RefinedPoolService.ContainsCard(card))
            return false;

        RefinedPoolService.ToggleCard(card);
        RefreshAfterPoolChange();
        return true;
    }

    private static NLibraryStatTickbox? ResolveTickboxTemplate(NCardLibrary library)
    {
        foreach (var fieldName in new[] { "_viewUpgrades", "_viewStats", "_viewMultiplayerCards" })
        {
            if (AccessTools.Field(typeof(NCardLibrary), fieldName)?.GetValue(library) is NLibraryStatTickbox tickbox)
                return tickbox;
        }

        return null;
    }

    private static Node? ResolveAnchor(NCardLibrary library, NLibraryStatTickbox template)
    {
        if (AccessTools.Field(typeof(NCardLibrary), "_searchBar")?.GetValue(library) is Node searchBar
            && searchBar.GetParent() is not null)
            return searchBar;

        return template;
    }

    private static void FinalizeToggle()
    {
        if (_editModeToggle is null || !GodotObject.IsInstanceValid(_editModeToggle))
            return;

        try
        {
            _editModeToggle.SetLabel(RefinedGemUiText.Get("refined_gem.ui.edit_mode_label"));
            _editModeToggle.IsTicked = false;
        }
        catch (Exception ex)
        {
            RefinedGemEntry.Logger.Warn($"[RefinedGem] Failed to finalize Edit Refined Pool toggle: {ex.Message}");
            Detach();
        }
    }

    private static NCardPoolFilter? ResolveRefinedPoolFilter(NCardLibrary library)
    {
        var poolFilters = AccessTools.Field(typeof(NCardLibrary), "_poolFilters")?.GetValue(library);
        if (poolFilters is not IDictionary dictionary)
            return null;

        NCardPoolFilter? fallback = null;

        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is not NCardPoolFilter filter)
                continue;

            if (string.Equals(filter.Name, RefinedPoolFilterStableId, StringComparison.Ordinal))
                return filter;

            if (fallback is null && IsRefinedPoolFilter(library, filter))
                fallback = filter;
        }

        return fallback;
    }

    private static bool IsRefinedPoolFilter(NCardLibrary library, NCardPoolFilter filter)
    {
        if (!GodotObject.IsInstanceValid(filter))
            return false;

        if (string.Equals(filter.Name, RefinedPoolFilterStableId, StringComparison.Ordinal))
            return true;

        if (IsVanillaPoolFilter(library, filter))
            return false;

        var poolFilters = AccessTools.Field(typeof(NCardLibrary), "_poolFilters")?.GetValue(library);
        return poolFilters is IDictionary dictionary && dictionary.Contains(filter);
    }

    private static bool IsVanillaPoolFilter(NCardLibrary library, NCardPoolFilter filter)
    {
        foreach (var fieldName in VanillaPoolFilterFields)
        {
            if (AccessTools.Field(typeof(NCardLibrary), fieldName)?.GetValue(library) is NCardPoolFilter vanilla
                && ReferenceEquals(vanilla, filter))
                return true;
        }

        return false;
    }

    private static void RebindRefinedPoolFilterPredicate()
    {
        if (_library is null
            || !GodotObject.IsInstanceValid(_library)
            || _refinedPoolFilter is null
            || !GodotObject.IsInstanceValid(_refinedPoolFilter))
            return;

        var poolFilters = AccessTools.Field(typeof(NCardLibrary), "_poolFilters")?.GetValue(_library);
        if (poolFilters is not IDictionary dictionary || !dictionary.Contains(_refinedPoolFilter))
            return;

        dictionary[_refinedPoolFilter] = (Func<CardModel, bool>)RefinedPoolService.ContainsCard;
    }

    private static void Detach()
    {
        if (_editModeToggle is not null && GodotObject.IsInstanceValid(_editModeToggle))
            _editModeToggle.QueueFree();

        _editModeToggle = null;
        _library = null;
        _refinedPoolFilter = null;
    }

    private static void RefreshAfterPoolChange()
    {
        if (_library is null || !GodotObject.IsInstanceValid(_library))
            return;

        RebindRefinedPoolFilterPredicate();

        if (IsRefinedPoolViewActive)
        {
            AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter")
                ?.Invoke(_library, [_refinedPoolFilter]);
            AccessTools.Method(typeof(NCardLibrary), "UpdateFilter")
                ?.Invoke(_library, [false]);
            return;
        }

        if (EditModeEnabled)
            AccessTools.Method(typeof(NCardLibrary), "UpdateFilter")?.Invoke(_library, [false]);
    }
}
