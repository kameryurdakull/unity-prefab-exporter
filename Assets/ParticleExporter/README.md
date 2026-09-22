# Particle Package Exporter

Import `ParticlePackageExporter.unitypackage` into the Unity project that contains your particle prefabs, or copy the `Assets/ParticleExporter` folder into that project.

Open **Tools > Particle Package Exporter** in the Unity Editor. Add particle prefab assets from the Project window, select **Analyze dependencies**, review the exact asset list, and select **Export .unitypackage**.

The exporter includes the selected prefab assets and the recursive dependencies reported by Unity's AssetDatabase: referenced materials, textures, shaders, animation assets, scripts, nested prefabs, and other referenced project assets. It exports that explicit list without Unity's additional dependency expansion. Unreferenced files and unrelated project settings are excluded.

Package Manager assets cannot be embedded in a `.unitypackage`; the window lists referenced Unity packages so the destination project can install them. Missing scripts on selected prefabs block export. References loaded only by code at runtime (for example, `Resources.Load`, Addressables keys, or path strings) are not serialized dependencies and must be handled separately.
