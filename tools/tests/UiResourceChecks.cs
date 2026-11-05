using Godot;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Visuals;
using Hero = Shenshen.ShenshenCode.Character.Shenshen;

internal static class UiResourceChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var hero = ModelDb.Character<Hero>();
        // BaseLib changes the paths, but the native select/map getters still cast to
        // CompressedTexture2D. Loading through Texture2D alone would miss this failure.
        foreach (var (name, path) in new[]
        {
            ("HUD icon", hero.CustomIconTexturePath),
            ("HUD outline", hero.CustomIconOutlineTexturePath),
            ("character select", hero.CustomCharacterSelectIconPath),
            ("locked character select", hero.CustomCharacterSelectLockedIconPath),
            ("map marker", hero.CustomMapMarkerPath)
        })
        {
            check(path.EndsWith(".png", StringComparison.OrdinalIgnoreCase), name + " uses an imported PNG");
            check(ResourceLoader.Load<Resource>(path) is CompressedTexture2D,
                name + " imports as CompressedTexture2D rather than another Texture2D subtype");
        }

        check(hero.CharacterSelectIcon is CompressedTexture2D && hero.CharacterSelectLockedIcon is CompressedTexture2D,
            "native character-select getters accept both imported textures through BaseLib path patches");
        check(hero.MapMarker is CompressedTexture2D,
            "native map getter passes AssetCache.GetCompressedTexture2D cast");
        check(hero.IconTexture is CompressedTexture2D && hero.IconOutlineTexture is CompressedTexture2D,
            "native HUD texture and outline getters resolve the imported avatar");

        // Inspect gutters inside the atlas, not just its outer corners: a recolor can
        // leave opaque fog around otherwise correctly imported sprite regions.
        using var atlasImage = ResourceLoader.Load<Texture2D>(ShenshenVisuals.AssetDirectory + "/parts.png").GetImage();
        if (atlasImage.IsCompressed()) atlasImage.Decompress();
        var gutters = new[] { new Vector2I(0, 0), new Vector2I(340, 400), new Vector2I(670, 800),
            new Vector2I(340, 1100), new Vector2I(15, 700), new Vector2I(1023, 1000) };
        check(gutters.All(point => atlasImage.GetPixelv(point).A <= 0.01f),
            "packed human atlas preserves transparent gutters after skin recoloring");

        // These inherited placeholder hand getters accept Texture2D in this game version.
        check(hero.ArmPointingTexture is Texture2D && hero.ArmRockTexture is Texture2D &&
            hero.ArmPaperTexture is Texture2D && hero.ArmScissorsTexture is Texture2D,
            "all four inherited CustomArmTexturePath values satisfy their native Texture2D getters");
        var icon = hero.Icon;
        try
        {
            check(icon is TextureRect { Texture: CompressedTexture2D },
                "BaseLib custom HUD Control is constructed from the imported avatar");
        }
        finally
        {
            icon.Free();
        }
    }
}
