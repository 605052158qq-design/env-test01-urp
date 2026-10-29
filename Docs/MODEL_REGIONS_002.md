# Iteration 002: six map-region working split

## Scope

No FBX was exported or overwritten in this iteration. The source hierarchy remains present; this is a reversible Blender-collection organization pass.

## Working file

`SourceAssets/Blender/ENV_Regions_6_Split.blend`

## Region collections

| Collection | Meshes | Spatial role |
| --- | ---: | --- |
| `ENV_Region_01_NorthWest` | 1,516 | north-west map segment |
| `ENV_Region_02_NorthCenter` | 1,824 | north-centre map segment |
| `ENV_Region_03_NorthEast` | 2,631 | north-east map segment |
| `ENV_Region_04_SouthWest` | 1,531 | south-west map segment |
| `ENV_Region_05_SouthCenter` | 2,201 | south-centre map segment |
| `ENV_Region_06_SouthEast` | 1,001 | south-east / snow-area map segment |

The regions were calculated from the world-space bounding-box centres of the environment meshes, then grouped into a 2×3 spatial layout. Every collection has an `ENV_Region_XX_Anchor` empty. Meshes are *linked* to their region collection rather than moved out of the original source hierarchy.

## Shared assets

`ENV_Shared_Assets` keeps the independently handled `ENV_Boat`, `ENV_Boat_Hull`, `ENV_Boat_Interior`, `ENV_Water_Surface`, and `ENV_Dock` visible as separate assets.

## Before any future export

Review meshes along each regional boundary in Blender. The six groups are a robust spatial first pass, not a claim that every gameplay boundary has already been approved. Export only a reviewed region as selected objects; do not export the complete source map.
