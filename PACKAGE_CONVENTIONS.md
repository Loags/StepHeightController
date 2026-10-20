# Loags Unity package conventions

Use one publisher folder and product-scoped code so separate Loags packages can share a Unity project.

- Customer content: `Assets/Loags/<Product>/`, split into `Runtime`, `Editor`, `Samples` and `Documentation`. Development content: `Assets/_Development/<Product>/`; never export it.
- Namespace and assembly prefix: `Loags.<ProductArea>`, with `.Runtime`, `.Editor`, `.Samples` and distinct development/test assemblies. Step Height Controller uses `Loags.StepHeight`; avoid naming its namespace exactly like the component type. Keep generic helper and generated input types inside the product namespace.
- Preserve every product asset and assembly GUID across moves. Use the canonical publisher folder metadata for `Assets/Loags` when exports include their parent folder. Renamed Unity object types need explicit old namespace/assembly migration metadata; external source and assembly references require documented updates.
- Keep customer and developer menus product-specific. Shader names, component menus, Resources paths, scripting symbols and persistent settings keys must also be product-specific. Existing Step Height menus remain `Tools/StepHeightController` and `Tools/StepHeightController Dev`.
- Runtime code should have only its actual dependencies. Gate optional demos separately. Setup tools must load without optional demo dependencies, preserve unrelated symbols/settings, and offer explicit, reversible actions. Do not change project-wide rendering, input, physics, quality or frame-rate settings on import.
- Export an explicit product root and only required registry dependencies. Exclude development content, tests, tooling, media, host settings, unrelated packages and temporary reports. Include only used third-party content and its notices.
- Validate each product alone and both import orders in a disposable project. Check GUIDs, assembly identities, serialized references, setup windows and dependency versions. Keep compatible installed dependencies; do not silently downgrade them.

## Repository and shared files

A shared authoring repository can hold independent product roots, one development area and a combined-import validation project. Each product keeps its own export allowlist and release version. This does not require a shared runtime library. Current product roots are `.unitypackage` exports; converting them to UPM also requires package manifests and asset-root lookup changes.

Introduce shared runtime code only for actual shared functionality. Maintain one canonical, versioned dependency rather than exporting competing copies of a shared assembly or settings asset inside several products. [UPM supports packages in repository subfolders](https://docs.unity.com/en-us/engine/6000.0/manual/packages-list/upm-git); a package manifest cannot declare another Git URL as its dependency. Use a registry dependency or explicit project-level installation when shared code becomes necessary.

## TweenHelper follow-up

TweenHelper already uses the Loags asset folder, but its source/assemblies still use `LB.TweenHelper`. Its developer content uses `Assets/_Project/TweenHelperDevelopment`; some component menus, Resources settings and shader names also retain older conventions. Those can migrate separately using the same GUID/type-migration policy. Its newer Unity/UI/URP baseline must be respected when validating both products together.
