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
        using var sourceImage = ReadImage(ResourceLoader.Load<Texture2D>(sourcePath));
        var head = layout.GetProperty("head_region").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        var region = new Rect2I(head[0], head[1], head[2], head[3]);
        const string selectionDirectory = "res://Shenshen/images/character_select/apricot-dawn-gold";
        using var selection = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(selectionDirectory + "/selection.json"));
        var selectionSourcePath = selectionDirectory + "/" + selection.RootElement.GetProperty("image").GetString();
        using var selectionImage = ReadImage(ResourceLoader.Load<Texture2D>(selectionSourcePath));
        var select = selection.RootElement.GetProperty("select_icon_region").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        var selectRegion = new Rect2I(select[0], select[1], select[2], select[3]);
        var visibleBounds = VisibleBounds(sourceImage);
        check(visibleBounds.HasArea() && TransparentBorder(sourceImage),
            "packed approved character image retains a transparent border and visible painted content");
        var selectBackground = ResourceLoader.Load<PackedScene>(hero.CustomCharacterSelectBg).Instantiate<Control>();
        var combatVisuals = hero.CreateVisuals();
        try
        {
            var artwork = selectBackground.GetNode<TextureRect>("Artwork");
            var combatMesh = combatVisuals.GetNode<Polygon2D>("Visuals/HumanRig/Canvas/Mesh");
            check(artwork.Texture is CompressedTexture2D && artwork.Texture.ResourcePath == selectionSourcePath &&
                selectionImage.GetSize() == new Vector2I(1672, 941),
                "selection background loads the approved full-screen apricot-dawn-gold image");
            check(CanvasItems(selectBackground).All(item => item.Modulate == Colors.White &&
                item.SelfModulate == Colors.White && item.ZIndex == 0 && item.ZAsRelative),
                "selection artwork retains its approved palette without additional tint or layer overrides");
            check(artwork.ExpandMode == TextureRect.ExpandModeEnum.IgnoreSize &&
                artwork.StretchMode == TextureRect.StretchModeEnum.KeepAspectCovered,
                "selection artwork fills the frame with uniform scaling rather than stretching the pose");
            check(artwork.AnchorLeft == 0 && artwork.AnchorTop == 0 &&
                artwork.AnchorRight == 0 && artwork.AnchorBottom == 0 &&
                artwork.Scale.IsEqualApprox(Vector2.One) && Mathf.IsZeroApprox(artwork.Rotation) &&
                artwork.MouseFilter == Control.MouseFilterEnum.Ignore,
                "selection layout owns the artwork frame without competing anchors or input interception");
            check(combatMesh.Texture.ResourcePath == sourcePath &&
                combatMesh.Polygon.Length == combatMesh.UV.Length &&
                combatMesh.Polygon.Zip(combatMesh.UV).All(pair => pair.First.IsEqualApprox(pair.Second)),
                "combat rest vertices map directly to original source pixels without reassembling body parts");
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
            ContainsSameRegion(selectIconImage, selectionImage, selectRegion),
            "character-select close-up copies the approved background pixels without resampling or padding");

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

    internal static async Task RunLayout(Node host, Action<bool, string> check)
    {
        var window = host.GetTree().Root;
        var originalSize = window.Size;
        var screen = new Control { Name = "SelectionLayoutScreen", MouseFilter = Control.MouseFilterEnum.Ignore };
        var nativeBackground = new Control
        {
            Name = "AnimatedBg",
            Position = new Vector2(-388, -80),
            PivotOffset = new Vector2(1280, 600),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        host.AddChild(screen);
        screen.AddChild(nativeBackground);
        var hero = ModelDb.Character<Hero>();
        var background = ResourceLoader.Load<PackedScene>(hero.CustomCharacterSelectBg).Instantiate<Control>();
        nativeBackground.AddChild(background);
        try
        {
            foreach (var (size, zoom) in new[]
            {
                (new Vector2I(1920, 1080), 1.1f),
                (new Vector2I(1736, 977), 1.1f),
                (new Vector2I(1600, 1200), 1.2683f)
            })
            {
                window.Size = size;
                screen.Size = size;
                nativeBackground.Size = (Vector2)size + new Vector2(640, 120);
                nativeBackground.Scale = Vector2.One * zoom;
                for (var frame = 0; frame < 2; frame++)
                    await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);

                var transform = background.GetGlobalTransform();
                check(transform.X.IsEqualApprox(Transform2D.Identity.X) &&
                    transform.Y.IsEqualApprox(Transform2D.Identity.Y) && transform.Origin.Length() < 0.01f &&
                    background.Size.IsEqualApprox((Vector2)size),
                    $"selection layout cancels native overscan and fills the {size.X}x{size.Y} viewport");
                check(!background.TopLevel && background.ZIndex == 0 && background.ZAsRelative && background.ClipContents,
                    $"selection layout keeps native drawing order and clips overscan at {size.X}x{size.Y}");
                var artwork = background.GetNode<TextureRect>("Artwork");
                var artworkBounds = artwork.GetGlobalRect();
                var imageSize = artwork.Texture.GetSize();
                check(artworkBounds.Position.X <= 0.5f && artworkBounds.Position.Y <= 0.5f &&
                    artworkBounds.End.X >= size.X - 0.5f && artworkBounds.End.Y >= size.Y - 0.5f &&
                    Math.Abs(artworkBounds.End.X - size.X) <= 0.5f &&
                    Mathf.IsEqualApprox(artworkBounds.Size.X / imageSize.X, artworkBounds.Size.Y / imageSize.Y),
                    $"selection artwork covers {size.X}x{size.Y} uniformly and aligns to the right edge");
                var viewportBounds = new Rect2(Vector2.Zero, (Vector2)size);
                var artworkTransform = artwork.GetGlobalTransform();
                check(new[] { new Vector2(650, 560), new Vector2(1500, 220) }.All(point =>
                    viewportBounds.HasPoint(artworkTransform * (point * artwork.Size / imageSize))),
                    $"both approved hand positions remain visible at {size.X}x{size.Y}");
            }
        }
        finally
        {
            screen.Free();
            window.Size = originalSize;
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
