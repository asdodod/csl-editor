# Local CSL exporter

The in-game **Export CSL** button runs this helper in a hidden local Python process. It writes a UnityFS `.saber` into `CustomSabers/Workshop`. Existing files get numbered copies. Original `.saber` files are never overwritten.

This helper is separate from the mod DLL because Unity Player does not include the AssetBundle builder. Install Python 3.10+ and the packages in `requirements.txt`. Then configure it once:

```powershell
./install-local.ps1 -BeatSaberDir 'C:/Games/Beat Saber' -PythonExecutable 'C:/Python/python.exe' -UnityPyPath 'C:/Python/Lib/site-packages' -BaseBundle 'C:/MyAssets/compatible-base.saber'
```

`BaseBundle` must be a valid PC `.saber` containing `_CustomSaber`, both hand objects, mesh renderers, materials, textures and the compiled shaders used by your new construction. This is your own local asset; no author's saber is included in this release. A copy is stored beside the installed helper. An imported project uses its original bundle instead.

The helper preserves original assets and adds generated meshes, materials, embedded textures and transform edits. Export is limited to shaders already contained in that bundle. Loading another shader in the editor does not automatically merge its compiled assets into the export. Arbitrary HLSL compilation is not supported.

Generated Flux and edited Zoog, MK1 TEK and Batonblade bundles were independently loaded and their active mesh geometry checked in Unity 2022.3. This does not replace an in-game visual check of every shader, animation or CSL hit event.
