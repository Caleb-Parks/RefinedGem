using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using HarmonyLib;
using RefinedGem.Services;

namespace RefinedGem.UI;

public static class CardLibrarySelectionController
{
    private static readonly Color ActiveModulate = new(0.85f, 1f, 0.95f);

    private static NCardLibrary? _library;
    private static NLibraryStatTickbox? _editModeToggle;

    public static bool EditModeEnabled =>
        _editModeToggle is not null
        && GodotObject.IsInstanceValid(_editModeToggle)
        && _editModeToggle.IsTicked;

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
        }
        catch (Exception ex)
        {
            RefinedGemEntry.Logger.Warn($"[RefinedGem] Failed to add Edit Refined Pool toggle: {ex.Message}");
            Detach();
        }
    }

    public static bool TryToggleCard(CardModel card, NCardHolder holder)
    {
        if (!EditModeEnabled)
            return false;

        RefinedPoolService.ToggleCard(card);

        if (holder is NGridCardHolder gridHolder)
            ApplyPoolHighlight(gridHolder, RefinedPoolService.ContainsCard(card));
        else
            RefreshGrid();

        return true;
    }

    public static void ApplyPoolHighlight(NGridCardHolder holder, bool inPool) =>
        holder.Modulate = inPool ? ActiveModulate : Colors.White;

    public static void RefreshAllPoolHighlights()
    {
        if (_library is null || !GodotObject.IsInstanceValid(_library))
            return;

        var grid = AccessTools.Field(typeof(NCardLibrary), "_grid")?.GetValue(_library) as Node;
        if (grid is null)
            return;

        foreach (var child in grid.GetChildren())
        {
            if (child is not NGridCardHolder gridHolder)
                continue;

            var card = gridHolder.CardModel;
            ApplyPoolHighlight(gridHolder, card is not null && RefinedPoolService.ContainsCard(card));
        }
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
            _editModeToggle.Toggled -= OnEditModeToggled;
            _editModeToggle.SetLabel(RefinedGemUiText.Get("refined_gem.ui.edit_mode_label"));
            _editModeToggle.IsTicked = false;
            _editModeToggle.Toggled += OnEditModeToggled;
        }
        catch (Exception ex)
        {
            RefinedGemEntry.Logger.Warn($"[RefinedGem] Failed to finalize Edit Refined Pool toggle: {ex.Message}");
            Detach();
        }
    }

    private static void OnEditModeToggled(NTickbox _) => RefreshGrid();

    private static void Detach()
    {
        if (_editModeToggle is not null && GodotObject.IsInstanceValid(_editModeToggle))
        {
            _editModeToggle.Toggled -= OnEditModeToggled;
            _editModeToggle.QueueFree();
        }

        _editModeToggle = null;
        _library = null;
    }

    private static void RefreshGrid()
    {
        if (_library is null || !GodotObject.IsInstanceValid(_library))
            return;

        var updateFilter = AccessTools.Method(typeof(NCardLibrary), "UpdateFilter");
        updateFilter?.Invoke(_library, [false]);
        Callable.From(RefreshAllPoolHighlights).CallDeferred();
    }
}
