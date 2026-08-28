Eight StS2-style concepts with **multicolored opalescent** gems and **transparent backgrounds**. **Production default: v03** (opalescent gem in corked glass bottle — *"Perfection, bottled."*).

| # | File | Concept |
|---|------|---------|
| 1 | `refined_gem_v01_amethyst.png` | Opalescent gem on velvet cushion |
| 2 | `refined_gem_v02_emerald.png` | Opalescent gem in gold filigree setting |
| 3 | `refined_gem_v03_ruby_bottle.png` | Opalescent gem in corked glass bottle (**production**) |
| 4 | `refined_gem_v04_sapphire_pedestal.png` | Opalescent gem cluster on stone pedestal |
| 5 | `refined_gem_v05_opal_sparkles.png` | Floating opalescent gem with sparkles |
| 6 | `refined_gem_v06_citrine_neow.png` | Opalescent gem in Neow coral/bone frame |
| 7 | `refined_gem_v07_diamond_teal.png` | Opalescent diamond in silver prongs with teal glow |
| 8 | `refined_gem_v08_geode.png` | Half-open geode with opalescent inner crystal |

Post-process transparent backgrounds:

```powershell
python tools/remove_relic_background.py art/relic/variations
```

To swap the production icon, run:

```powershell
python tools/finalize_relic_icon.py art/relic/variations/refined_gem_vNN_*.png
powershell -ExecutionPolicy Bypass -File .\build.ps1 -NoDeploy
```
