# Iteration 001: boat and water split

## Produced assets

- `Assets/Map/Models/Processed/ENV_Boat.fbx`
- `Assets/Map/Models/Processed/ENV_Water_Surface.fbx`

## Source

Both assets were extracted from disconnected mesh islands in `ENV_test01_1021.FBX` object `Plane009` using Blender. The original FBX remains unchanged.

The Blender source files are deliberately kept outside `Assets/` so Unity will not import them:

- `SourceAssets/Blender/ENV_Source_Import_Backup.blend` — a clean import backup made before any hierarchy or name changes.
- `SourceAssets/Blender/ENV_Processed_Source.blend` — the working source for manual export. It contains `ENV_SourceMap`, `ENV_Boat` (`ENV_Boat_Hull` and `ENV_Boat_Interior`), `ENV_Water_Surface`, and `ENV_Dock`.

Only objects whose purpose was confirmed were renamed. The remaining environment objects intentionally retain their original names pending an asset-by-asset identification pass.

## Intended Unity use

- Place `ENV_Boat` under a dedicated `BoatRoot` GameObject. Boat movement, arrival markers, collider, and interaction scripts attach to that root only.
- Place `ENV_Water_Surface` under `EnvironmentRoot`. Water rendering and water-specific material controls attach to this asset only.
- Keep the existing `ENV_test01_1021.FBX` as the temporary static environment until a separate static-environment export is verified. Do not place the original `Plane009` together with the new boat and water assets, or the old embedded boat/water geometry will overlap them.

## Verification

The extracted boat was visually checked from top and three-quarter views in Blender. It contains a hull mesh and an interior mesh, with a shared root suitable for movement.

## Remaining work

The complete static-environment FBX export exceeded the Blender MCP call timeout. It has not been added, so there is no unverified replacement for the current environment.
