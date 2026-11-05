using System.Text.Json;
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

        using var rig = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(ShenshenVisuals.RigPath));
        var headPart = rig.RootElement.GetProperty("parts").EnumerateArray()
            .Single(part => part.GetProperty("id").GetString() == "head");
        var headRegion = headPart.GetProperty("region").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        var region = new Rect2I(headRegion[0], headRegion[1], headRegion[2], headRegion[3]);
        var selectBackground = ResourceLoader.Load<PackedScene>(hero.CustomCharacterSelectBg).Instantiate<Control>();
        try
        {
            var sharedHead = selectBackground.GetNode<TextureRect>("Portrait/Head").Texture as AtlasTexture;
            check(sharedHead != null && sharedHead.Atlas.ResourcePath == ShenshenVisuals.AssetDirectory + "/parts.png" &&
                sharedHead.Region == (Rect2)region,
                "selection portrait directly shares the animation rig's approved head atlas region");
            using var body = ReadImage(selectBackground.GetNode<TextureRect>("Portrait/Body").Texture);
            check(new[] { new Vector2I(450, 80), new Vector2I(475, 160), new Vector2I(490, 220) }
                .All(point => body.GetPixelv(point).A <= 0.01f),
                "selection body base removes the old independently drawn face behind the shared head");
        }
        finally
        {
            selectBackground.Free();
        }
        using var avatarImage = ReadImage(hero.IconTexture);
        using var selectIconImage = ReadImage(hero.CharacterSelectIcon);
        check(ContainsSameHead(avatarImage, atlasImage, region),
            "HUD and map avatar preserve the approved animation head pixels with transparent padding");
        check(ContainsSameHead(selectIconImage, atlasImage, region),
            "character select button preserves the approved animation head pixels without stretching its face");

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

    private static Image ReadImage(Texture2D texture)
    {
        var image = texture.GetImage();
        if (image.IsCompressed()) image.Decompress();
        return image;
    }

    private static bool ContainsSameHead(Image icon, Image atlas, Rect2I region)
    {
        if (icon.GetWidth() < region.Size.X || icon.GetHeight() < region.Size.Y) return false;
        var offset = new Vector2I((icon.GetWidth() - region.Size.X) / 2, (icon.GetHeight() - region.Size.Y) / 2);
        var destination = new Rect2I(offset, region.Size);
        for (var y = 0; y < icon.GetHeight(); y++)
        for (var x = 0; x < icon.GetWidth(); x++)
        {
            var pixel = icon.GetPixel(x, y);
            var expected = destination.HasPoint(new Vector2I(x, y))
                ? atlas.GetPixel(x - offset.X + region.Position.X, y - offset.Y + region.Position.Y)
                : Colors.Transparent;
            // Godot's alpha-border import may fill RGB in fully transparent texels.
            // Compare visible premultiplied color so hidden gutter RGB is immaterial.
            const float tolerance = 1.01f / 255;
            if (Math.Abs(pixel.A - expected.A) > tolerance ||
                Math.Abs(pixel.R * pixel.A - expected.R * expected.A) > tolerance ||
                Math.Abs(pixel.G * pixel.A - expected.G * expected.A) > tolerance ||
                Math.Abs(pixel.B * pixel.A - expected.B * expected.A) > tolerance) return false;
        }
        return true;
    }
}
