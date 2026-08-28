# Refined Gem

Slay the Spire 2 mod that lets you curate a custom **Refined** card pool from the Card Library and replace your run card sources with the **Refined Gem** relic.

## Requirements

- Slay the Spire 2 (0.107.1+)
- [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295) (`STS2-RitsuLib`)

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Output: `dist\RefinedGem\` (also deployed to the game's `mods\RefinedGem` folder)

## Usage

1. Enable **Refined Gem** and **RitsuLib** in the in-game mod menu.
2. Open **Card Library** (compendium). Use **Edit Refined Pool** to toggle cards into your pool from any character/pool filter.
3. Select the **Refined** compendium filter to review your curated pool.
4. During a run, obtain **Refined Gem** (including from Neow). While you hold it and your Refined pool is non-empty, combat rewards and shop cards are drawn from that pool (per player in multiplayer).

If your Refined pool is empty, the relic has no card-pool effect.

## Refined pool file

Your curated pool is stored in `refined_pool.json` next to the mod DLL (for example `mods/RefinedGem/refined_pool.json`). The pool is global for that mod install, not per save profile.

Edit the file directly with any text editor, or let the Card Library UI update it when you toggle cards. Re-open the Card Library after manual edits so the game reloads the file.

Example:

```json
[
  "DRAMATIC_ENTRANCE",
  "FLASH_OF_STEEL"
]
```

Each entry is a card slug (`CardModel.Id.Entry`). Unknown or mistyped slugs are ignored at runtime; the rest of the pool still works.

On first run after this update, if you had cards saved in the old profile-scoped location, they are copied into `refined_pool.json` automatically.

## Manual test checklist

- [ ] Add/remove cards in Card Library; confirm `refined_pool.json` updates in the mod folder
- [ ] Manually edit `refined_pool.json` with one valid slug and one invalid slug; confirm only the valid card appears in the Refined filter
- [ ] Rebuild/deploy the mod; confirm an existing `refined_pool.json` is not overwritten
- [ ] Refined filter shows only curated cards
- [ ] Refined Gem with empty pool: character pool unchanged
- [ ] Refined Gem with curated pool: rewards and shop use refined cards only
- [ ] Refined Gem appears at Neow and in Compendium > Relic Collection > Ancient > Neow
- [ ] Dev console `REFINED_GEM` grants the relic with correct title and description
- [ ] Multiplayer: each player's relic and pool apply only to that player
