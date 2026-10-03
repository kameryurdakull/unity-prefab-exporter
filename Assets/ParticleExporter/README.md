# Prefab Package Exporter

Import PrefabPackageExporter.unitypackage or copy Assets/ParticleExporter into your Unity project. The original installation folder is retained for compatibility; ParticleSystem components are no longer required.

## Project window
Select one or more .prefab assets, then right-click **Prefab Exporter > Export Selected Prefabs...** to export them together in one .unitypackage. Ctrl/Cmd-click and Shift-click selections are supported. Mixed selections use only supported prefabs; folders are not scanned recursively. Export roots must be .prefab assets under Assets, not scene objects, imported models, or Package Manager assets.

## Collect from different folders
Right-click **Prefab Exporter > Add to Export Window** to append selected prefabs to the existing collection. Repeat from other folders. Duplicate entries are ignored. The collection survives script compilation/domain reload while the window exists; it is not a saved collection preset.

Open **Tools > Prefab Package Exporter** to add individual or selected prefabs, remove entries, or clear the list. **Analyze dependencies** previews the contents. **Export .unitypackage** exports the entire collection, also without a prior analysis. Exporting keeps the collection.

## Dependencies
Includes selected prefabs and recursive serialized dependencies reported by Unity: materials, textures, shaders, animation assets, scripts, nested prefabs, and other referenced project assets. Local script assembly definition/reference files are included when discovered. Unrelated files and project settings are excluded.

Package Manager assets cannot be embedded. Referenced package names appear in the preview and before export; install them in the destination project. Missing scripts on selected prefab hierarchies block export. Dependencies are rebuilt immediately before exporting.

Code-only references such as Resources.Load paths or Addressables keys, external script assembly dependencies, and project settings must be transferred separately. This is an editor-only tool with no runtime library requirements.
