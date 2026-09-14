# Refined Gem

Slay the Spire 2 mod that lets you curate a custom **Refined** card pool from the Card Library and replace your run card sources with the **Refined Gem** relic.

## Requirements

- Slay the Spire 2 (0.107.1+)

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Output: `dist\RefinedGem\` (also deployed to the game's `mods\RefinedGem` folder)

## Usage

1. Enable **Refined Gem** in the in-game mod menu.
2. Open **Card Library** (compendium). Use **Edit Refined Pool** to toggle cards into your pool from any character/pool filter.
3. Select the **Refined** compendium filter to review your curated pool.
4. During a run, obtain **Refined Gem** (including from Neow). While you hold it and your Refined pool is non-empty, combat rewards, shop cards, and card transforms (relics, events, cards, combat) use that pool (per player in multiplayer).

If your Refined pool is empty, the relic has no card-pool effect.

### Multiplayer

Your curated pool stays in local `refined_pool.json` for editing. When you ready up in a multiplayer lobby, the mod broadcasts that snapshot to the other peers (and resends when someone joins or rejoins). Each peer keeps an in-memory copy keyed by player so rewards/shops/transforms stay deterministic. Remote pools are **not** written to your JSON file.

All players need the same Refined Gem version (`affects_gameplay: true`).

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
- [ ] Refined Gem with curated pool: transform relics/events draw from refined cards; event transform preview cycles refined cards
- [ ] Refined pool with only Basic cards: transforms still work via uniform fallback
- [ ] Refined Gem with empty pool / no relic: transforms stay vanilla
- [ ] Fixed-target transforms (e.g. Claws) remain unchanged
- [ ] Refined Gem appears as a fourth Neow option (alongside the normal three) and in Compendium > Relic Collection > Ancient > Neow
- [ ] Dev console `REFINED_GEM` grants the relic with correct title and description
- [ ] Multiplayer: each player's relic and pool apply only to that player
- [ ] Multiplayer: two players ready up with different `refined_pool.json` contents; each still gets their own pool for rewards/transforms, and the run does not desync
- [ ] Multiplayer: late joiner receives pool snapshots from already-ready peers
- [ ] Multiplayer: local `refined_pool.json` is unchanged after a session with differently pooled peers
- [ ] Room Full of Cheese (Gorge): prefers Common refined cards; if stock is insufficient, broadens to any refined (Uniform)
- [ ] Infested Automaton Study/Touch Core: prefer Power / 0-cost from refined; broaden to any refined on failure
- [ ] Future of Potions: prefer mapped rarity+type from refined; broaden then Uniform backup on failure
- [ ] Brain Leech Rip / Endless Conveyor Fried Eel / Lead Paperweight: colorless rewards stay vanilla
- [ ] Colorful Philosophers / Kaleidoscope: off-character rewards stay vanilla
- [ ] Sea Glass: stays fully vanilla
- [ ] Crystal Sphere / Arcane Scroll / Glass Eye / Scroll Boxes: exact-rarity filters compose with refined
