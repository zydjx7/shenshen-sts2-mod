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

        using var rig = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(ShenshenVisuals.RigPath));
        var layout = rig.RootElement;
        var sourcePath = ShenshenVisuals.AssetDirectory + "/" + layout.GetProperty("image").GetString();
        var ringPath = ShenshenVisuals.AssetDirectory + "/solar-ring.tscn";
        using var sourceImage = ReadImage(ResourceLoader.Load<Texture2D>(sourcePath));
        var head = layout.GetProperty("head_region").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        var region = new Rect2I(head[0], head[1], head[2], head[3]);
        var select = layout.GetProperty("select_icon_region").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        var selectRegion = new Rect2I(select[0], select[1], select[2], select[3]);
        var visibleBounds = VisibleBounds(sourceImage);
        check(visibleBounds.HasArea() && TransparentBorder(sourceImage),
            "packed approved character image retains a transparent border and visible painted content");
        var selectBackground = ResourceLoader.Load<PackedScene>(hero.CustomCharacterSelectBg).Instantiate<Control>();
        var combatVisuals = hero.CreateVisuals();
        try
        {
            var portraitRig = selectBackground.GetNode<Node2D>("Portrait/Rig");
            var canvas = portraitRig.GetNode<Node2D>("Canvas");
            var portrait = canvas.GetNode<Sprite2D>("Character");
            var combatMesh = combatVisuals.GetNode<Polygon2D>("Visuals/HumanRig/Canvas/Mesh");
            var portraitRing = canvas.GetNode<Node2D>("SolarRing");
            var combatRing = combatVisuals.GetNode<Node2D>("Visuals/HumanRig/Canvas/SolarRing");
            check(portrait.Texture is CompressedTexture2D && portrait.Texture.ResourcePath == sourcePath &&
                combatMesh.Texture.ResourcePath == sourcePath && portrait.Texture == combatMesh.Texture,
                "selection portrait and actual combat mesh share the same approved character.png resource");
            check(!portrait.Centered && !portrait.RegionEnabled && portrait.Position.IsZeroApprox() &&
                portrait.Offset.IsZeroApprox() && portrait.Scale.IsEqualApprox(Vector2.One) &&
                Mathf.IsZeroApprox(portrait.Rotation) && portraitRig.Scale.X > 0 &&
                Mathf.IsEqualApprox(portraitRig.Scale.X, portraitRig.Scale.Y),
                "selection portrait preserves the complete source image with only uniform frame scaling");
            check(combatMesh.Polygon.Length == combatMesh.UV.Length &&
                combatMesh.Polygon.Zip(combatMesh.UV).All(pair => pair.First.IsEqualApprox(pair.Second)),
                "combat rest vertices map directly to original source pixels without reassembling body parts");
            check(portraitRing.SceneFilePath == ringPath && combatRing.SceneFilePath == ringPath &&
                portraitRing.Position.IsEqualApprox(combatRing.Position) &&
                portraitRing.Scale.IsEqualApprox(combatRing.Scale) &&
                portraitRing.GetIndex() < portrait.GetIndex() && combatRing.GetIndex() < combatMesh.GetIndex(),
                "selection and combat instantiate the same solar-ring scene at the same local placement behind the body");
            check(CanvasItems(portraitRig).All(item => item.ZIndex == 0 && item.ZAsRelative),
                "selection character and every halo line keep relative Z zero");
            var corners = new[] { visibleBounds.Position, visibleBounds.End,
                new Vector2(visibleBounds.End.X, visibleBounds.Position.Y),
                new Vector2(visibleBounds.Position.X, visibleBounds.End.Y) };
            check(corners.All(point => InPortraitFrame(PortraitPoint(portrait, point))) &&
                portraitRing.GetChildren().OfType<Line2D>().All(line =>
                    line.Points.All(point => InPortraitFrame(PortraitPoint(line, point)))),
                "selection frame contains the actual painted silhouette and all shared halo ticks");
        }
        finally
        {
            selectBackground.Free();
            combatVisuals.Free();
        }
        using var avatarImage = ReadImage(hero.IconTexture);
        using var selectIconImage = ReadImage(hero.CharacterSelectIcon);
        check(avatarImage.GetSize() == new Vector2I(450, 450) &&
            selectIconImage.GetWidth() * 65 == selectIconImage.GetHeight() * 44,
            "HUD keeps its square canvas and selection matches the native 132:195 portrait ratio");
        check(ContainsSameRegion(avatarImage, sourceImage, region),
            "HUD and map avatar preserve the approved head_region pixels with transparent padding");
        check(selectIconImage.GetSize() == selectRegion.Size &&
            ContainsSameRegion(selectIconImage, sourceImage, selectRegion),
            "character-select close-up fills its canvas with approved source pixels and no added padding");

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

    private static Rect2 VisibleBounds(Image image)
    {
        var minimum = image.GetSize();
        var maximum = new Vector2I(-1, -1);
        for (var y = 0; y < image.GetHeight(); y++)
        for (var x = 0; x < image.GetWidth(); x++)
        {
            if (image.GetPixel(x, y).A <= 0.04f) continue;
            minimum = new Vector2I(Math.Min(minimum.X, x), Math.Min(minimum.Y, y));
            maximum = new Vector2I(Math.Max(maximum.X, x), Math.Max(maximum.Y, y));
        }
        return maximum.X < 0 ? new Rect2() : new Rect2(minimum, maximum - minimum + Vector2I.One);
    }

    private static bool TransparentBorder(Image image) =>
        Enumerable.Range(0, image.GetWidth()).All(x =>
            image.GetPixel(x, 0).A <= 0.04f && image.GetPixel(x, image.GetHeight() - 1).A <= 0.04f) &&
        Enumerable.Range(0, image.GetHeight()).All(y =>
            image.GetPixel(0, y).A <= 0.04f && image.GetPixel(image.GetWidth() - 1, y).A <= 0.04f);

    // The packed portrait is outside the tree here; accumulate local transforms up to its Control frame.
    private static Vector2 PortraitPoint(Node2D node, Vector2 point)
    {
        while (true)
        {
            point = node.Transform * point;
            if (node.GetParent() is not Node2D parent) return point;
            node = parent;
        }
    }

    private static bool InPortraitFrame(Vector2 point) =>
        point.X >= -0.5f && point.Y >= -0.5f && point.X <= 633.834f && point.Y <= 950.5f;

    private static IEnumerable<CanvasItem> CanvasItems(Node node)
    {
        if (node is CanvasItem item) yield return item;
        foreach (var child in node.GetChildren())
            foreach (var descendant in CanvasItems(child)) yield return descendant;
    }

    private static bool ContainsSameRegion(Image icon, Image atlas, Rect2I region)
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
