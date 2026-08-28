using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using HarmonyLib;
using RefinedGem.Services;

namespace RefinedGem.UI;

public static class CardLibrarySelectionController
{
    private static NCardLibrary? _library;
    private static CheckButton? _editModeButton;

    public static bool EditModeEnabled => _editModeButton?.ButtonPressed ?? false;

    public static void Attach(NCardLibrary library)
    {
        if (_library is not null)
            return;

        _library = library;
        _editModeButton = new CheckButton
        {
            Text = RefinedGemUiText.Get("refined_gem.ui.edit_mode_off"),
            FocusMode = Control.FocusModeEnum.All,
        };

        _editModeButton.Toggled += OnEditModeToggled;
        library.AddChild(_editModeButton);
        library.MoveChild(_editModeButton, 0);
    }

    public static bool TryToggleCard(CardModel card)
    {
        if (!EditModeEnabled)
            return false;

        RefinedPoolService.ToggleCard(card);
        RefreshGrid();
        return true;
    }

    private static void OnEditModeToggled(bool pressed)
    {
        if (_editModeButton is null)
            return;

        _editModeButton.Text = RefinedGemUiText.Get(
            pressed ? "refined_gem.ui.edit_mode_on" : "refined_gem.ui.edit_mode_off");
        RefreshGrid();
    }

    private static void RefreshGrid()
    {
        if (_library is null)
            return;

        var updateFilter = AccessTools.Method(typeof(NCardLibrary), "UpdateFilter");
        updateFilter?.Invoke(_library, null);
    }
}
