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
    private static string? _rangeAnchorCardId;
    private static List<string>? _rangeSnapshotIds;
    private static bool _rangeOperationInclude;
    private static bool _pendingRefresh;
    private static bool _shiftWasHeld;
    private static bool _processFrameConnected;
    private static Callable _processFrameCallable;

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
            CardLibraryFeedback.Bind(library);
            _editModeToggle = (NLibraryStatTickbox)template.Duplicate();
            _editModeToggle.Name = "RefinedGemEditPoolToggle";
            _editModeToggle.Visible = true;

            parent.AddChild(_editModeToggle);
            parent.MoveChild(_editModeToggle, anchor.GetIndex() + 1);

            ConnectProcessFrame(library);
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

    /// <summary>
    /// Handles an edit-mode card click. Returns whether cards were added (true) or removed (false).
    /// </summary>
    public static bool TryHandleEditClick(CardModel card, out bool added)
    {
        added = false;
        if (!EditModeEnabled)
            return false;

        var cardId = GetStableCardId(card);
        var include = !RefinedPoolService.ContainsCard(card);
        var shiftHeld = IsShiftHeld();

        // Non-Shift: single toggle + refresh. Range anchors require Shift.
        if (!shiftHeld)
        {
            ClearRangeState(refreshIfPending: false);
            RefinedPoolService.ToggleCard(card);
            RefreshAfterPoolChange();
            added = include;
            return true;
        }

        // Shift + existing anchor => complete range, then refresh.
        if (!string.IsNullOrEmpty(_rangeAnchorCardId)
            && TryGetRangeCardIds(card, out var rangeIds))
        {
            RefinedPoolService.SetCardIdsIncluded(rangeIds, _rangeOperationInclude);
            ClearRangeState(refreshIfPending: false);
            RefreshAfterPoolChange();
            added = _rangeOperationInclude;
            return true;
        }

        // Shift + no usable anchor => start a new range. Mutate now, defer refresh.
        CaptureRangeSnapshot(cardId);
        _rangeOperationInclude = include;
        RefinedPoolService.ToggleCard(card);
        _pendingRefresh = true;
        added = include;
        return true;
    }

    private static bool IsShiftHeld() =>
        Input.IsKeyPressed(Key.Shift);

    private static void ConnectProcessFrame(NCardLibrary library)
    {
        if (_processFrameConnected)
            return;

        var tree = library.GetTree();
        if (tree is null)
            return;

        _processFrameCallable = Callable.From(OnProcessFrame);
        tree.Connect(SceneTree.SignalName.ProcessFrame, _processFrameCallable);
        _processFrameConnected = true;
        _shiftWasHeld = Input.IsKeyPressed(Key.Shift);
    }

    private static void DisconnectProcessFrame()
    {
        if (!_processFrameConnected)
            return;

        if (_library is not null && GodotObject.IsInstanceValid(_library))
        {
            var tree = _library.GetTree();
            if (tree is not null && tree.IsConnected(SceneTree.SignalName.ProcessFrame, _processFrameCallable))
                tree.Disconnect(SceneTree.SignalName.ProcessFrame, _processFrameCallable);
        }

        _processFrameConnected = false;
    }

    private static void OnProcessFrame()
    {
        var held = Input.IsKeyPressed(Key.Shift);
        if (_shiftWasHeld && !held)
            OnShiftReleased();
        _shiftWasHeld = held;
    }

    private static void OnShiftReleased()
    {
        if (_rangeAnchorCardId is null && !_pendingRefresh)
            return;

        var needsRefresh = _pendingRefresh;
        ClearRangeState(refreshIfPending: false);
        if (needsRefresh)
            RefreshAfterPoolChange();
    }

    private static void ClearRangeState(bool refreshIfPending)
    {
        var needsRefresh = refreshIfPending && _pendingRefresh;
        _rangeAnchorCardId = null;
        _rangeSnapshotIds = null;
        _pendingRefresh = false;
        if (needsRefresh)
            RefreshAfterPoolChange();
    }

    private static void CaptureRangeSnapshot(string anchorCardId)
    {
        _rangeAnchorCardId = anchorCardId;
        if (TryGetVisibleCards(out var visible))
            _rangeSnapshotIds = visible.Select(GetStableCardId).ToList();
        else
            _rangeSnapshotIds = [anchorCardId];
    }

    private static bool TryGetVisibleCards(out IReadOnlyList<CardModel> cards)
    {
        cards = Array.Empty<CardModel>();
        if (_library is null || !GodotObject.IsInstanceValid(_library))
            return false;

        if (AccessTools.Field(typeof(NCardLibrary), "_grid")?.GetValue(_library) is not NCardLibraryGrid grid
            || !GodotObject.IsInstanceValid(grid))
            return false;

        cards = grid.VisibleCards.ToList();
        return cards.Count > 0;
    }

    private static bool TryGetRangeCardIds(CardModel clicked, out IReadOnlyList<string> rangeIds)
    {
        rangeIds = Array.Empty<string>();
        if (string.IsNullOrEmpty(_rangeAnchorCardId) || _rangeSnapshotIds is null || _rangeSnapshotIds.Count == 0)
            return false;

        var clickedId = GetStableCardId(clicked);
        var anchorIndex = _rangeSnapshotIds.FindIndex(id =>
            string.Equals(id, _rangeAnchorCardId, StringComparison.Ordinal));
        var clickedIndex = _rangeSnapshotIds.FindIndex(id =>
            string.Equals(id, clickedId, StringComparison.Ordinal));

        if (anchorIndex < 0 || clickedIndex < 0)
            return false;

        var start = Math.Min(anchorIndex, clickedIndex);
        var end = Math.Max(anchorIndex, clickedIndex);
        rangeIds = _rangeSnapshotIds.GetRange(start, end - start + 1);
        return rangeIds.Count > 0;
    }

    private static string GetStableCardId(CardModel card) =>
        card.CanonicalInstance.Id.Entry;

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
        DisconnectProcessFrame();

        if (_editModeToggle is not null && GodotObject.IsInstanceValid(_editModeToggle))
            _editModeToggle.QueueFree();

        CardLibraryFeedback.Detach();
        _editModeToggle = null;
        _library = null;
        _refinedPoolFilter = null;
        _rangeAnchorCardId = null;
        _rangeSnapshotIds = null;
        _pendingRefresh = false;
        _shiftWasHeld = false;
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
