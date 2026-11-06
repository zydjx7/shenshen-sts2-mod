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

        // Inspect the 3-by-4 sprite-cell gutters, not just the atlas's outer corners:
        // a regenerated atlas can otherwise leave opaque fog behind the painted parts.
        using var atlasImage = ResourceLoader.Load<Texture2D>(ShenshenVisuals.AssetDirectory + "/parts.png").GetImage();
        if (atlasImage.IsCompressed()) atlasImage.Decompress();
        var gutters = Enumerable.Range(0, 4).SelectMany(column => Enumerable.Range(0, 5)
            .Select(row => new Vector2I(Math.Min(atlasImage.GetWidth() - 1, column * atlasImage.GetWidth() / 3),
                Math.Min(atlasImage.GetHeight() - 1, row * atlasImage.GetHeight() / 4))));
        check(gutters.All(point => atlasImage.GetPixelv(point).A <= 0.01f),
            "packed mystery atlas preserves transparent gutters between sprite cells");

        using var rig = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(ShenshenVisuals.RigPath));
        var parts = rig.RootElement.GetProperty("parts").EnumerateArray()
            .ToDictionary(part => part.GetProperty("id").GetString()!);
        var headPart = parts["head"];
        var headRegion = headPart.GetProperty("region").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        var region = new Rect2I(headRegion[0], headRegion[1], headRegion[2], headRegion[3]);
        var selectBackground = ResourceLoader.Load<PackedScene>(hero.CustomCharacterSelectBg).Instantiate<Control>();
        try
        {
            var sharedHead = selectBackground.GetNode<Sprite2D>(PartPath("head", parts) + "/Sprite").Texture as AtlasTexture;
            check(sharedHead != null && sharedHead.Atlas.ResourcePath == ShenshenVisuals.AssetDirectory + "/parts.png" &&
                sharedHead.Region == (Rect2)region,
                "selection portrait directly shares the animation rig's approved head atlas region");
            check(parts.All(pair => MatchesPart(selectBackground, pair.Key, pair.Value, parts)),
                "selection portrait preserves every combat rig part's atlas, pivot, size and local transform");
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

    private static string PartPath(string id, IReadOnlyDictionary<string, JsonElement> parts)
    {
        var part = parts[id];
        var parent = part.TryGetProperty("parent", out var value) ? value.GetString() : null;
        return (string.IsNullOrEmpty(parent) ? "Portrait/Rig/Canvas" : PartPath(parent, parts)) + "/Part_" + id;
    }

    private static bool MatchesPart(Node scene, string id, JsonElement part, IReadOnlyDictionary<string, JsonElement> parts)
    {
        var pivot = scene.GetNode<Node2D>(PartPath(id, parts));
        var sprite = pivot.GetNode<Sprite2D>("Sprite");
        var region = part.GetProperty("region").EnumerateArray().Select(value => value.GetSingle()).ToArray();
        var atlas = sprite.Texture as AtlasTexture;
        var rotation = part.TryGetProperty("rotation", out var value) ? value.GetSingle() : 0;
        return atlas != null && atlas.Atlas.ResourcePath == ShenshenVisuals.AssetDirectory + "/" + part.GetProperty("file").GetString() &&
            atlas.Region == new Rect2(region[0], region[1], region[2], region[3]) &&
            pivot.Position.IsEqualApprox(Vector(part.GetProperty("position"))) &&
            Math.Abs(pivot.RotationDegrees - rotation) < 0.001f &&
            sprite.Offset.IsEqualApprox(-Vector(part.GetProperty("pivot")) * sprite.Texture.GetSize()) &&
            sprite.Scale.IsEqualApprox(Vector(part.GetProperty("size")) / sprite.Texture.GetSize()) &&
            sprite.Visible == (id is not ("head_closed" or "magic")) && sprite.ZIndex == 0;
    }

    private static Vector2 Vector(JsonElement array) => new(array[0].GetSingle(), array[1].GetSingle());

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
