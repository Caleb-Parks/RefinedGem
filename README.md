# Refined Gem

Slay the Spire 2 mod that lets you curate a custom **Refined** card pool from the Card Library and replace your run card sources with the **Refined Gem** relic.

## Requirements

- Slay the Spire 2 (0.107.1+)
- [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295) (`STS2-RitsuLib`)

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Output: `dist\RefinedGem\`

Deploy to the game mods folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Deploy
```

## Usage

1. Enable **Refined Gem** and **RitsuLib** in the in-game mod menu.
2. Open **Card Library** (compendium). Use **Edit Refined Pool** to toggle cards into your pool from any character/pool filter.
3. Select the **Refined** compendium filter to review your curated pool.
4. During a run, obtain **Refined Gem**. While you hold it and your Refined pool is non-empty, combat rewards and shop cards are drawn from that pool (per player in multiplayer).
5. In mod settings, enable **Add Refined Gem to Neow** to allow the relic at Neow.

If your Refined pool is empty, the relic has no card-pool effect.

## Manual test checklist

- [ ] Add/remove cards across multiple pool filters; restart game and confirm persistence
- [ ] Refined filter shows only curated cards
- [ ] Refined Gem with empty pool: character pool unchanged
- [ ] Refined Gem with curated pool: rewards and shop use refined cards only
- [ ] Neow toggle off/on changes Neow eligibility
- [ ] Multiplayer: each player's relic and pool apply only to that player
