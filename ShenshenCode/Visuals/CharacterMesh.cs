using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Shenshen.ShenshenCode.Visuals;

/// <summary>A continuous mesh keeps sleeves, limbs and their original painted joins together.</summary>
internal sealed class CharacterMesh
{
    internal readonly Polygon2D Node;
    internal readonly MeshLayout Layout;
    internal readonly Vector2[] Rest;
    private readonly bool _cat;
    private readonly Vector2 _size;

    internal CharacterMesh(string directory, bool cat)
    {
        _cat = cat;
        Layout = JsonSerializer.Deserialize<MeshLayout>(Godot.FileAccess.GetFileAsString(directory + "/rig.json"))!;
        _size = Vec(Layout.DesignSize);
        const int columns = 48, rows = 64;
        Rest = new Vector2[(columns + 1) * (rows + 1)];
        for (var y = 0; y <= rows; y++)
        for (var x = 0; x <= columns; x++)
            Rest[y * (columns + 1) + x] = new Vector2(x / (float)columns, y / (float)rows) * _size;
        var triangles = new Godot.Collections.Array();
        for (var y = 0; y < rows; y++)
        for (var x = 0; x < columns; x++)
        {
            var a = y * (columns + 1) + x;
            triangles.Add(new[] { a, a + 1, a + columns + 2 });
            triangles.Add(new[] { a, a + columns + 2, a + columns + 1 });
        }
        Node = new Polygon2D
        {
            Name = "Mesh", Texture = GD.Load<Texture2D>(directory + "/" + Layout.Image),
            Polygon = Rest, UV = Rest, Polygons = triangles,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear
        };
    }

    internal Vector2[] Pose(Motion motion) => Rest.Select(point => Warp(point, motion)).ToArray();

    // Rest-coordinate weights prevent accumulated drift after interrupted actions.
    internal Vector2 Warp(Vector2 point, Motion motion)
    {
        var p = point / _size;
        var q = p;
        if (_cat)
        {
            var body = Bell(p, new(.49f, .59f), new(.29f, .21f));
            var upper = 1 - Ramp(.70f, .84f, p.Y);
            q += new Vector2(.012f * motion.Strike - .025f * motion.Recoil,
                -.006f * motion.Breath + .018f * motion.Recoil) * body * upper;
            var head = Bell(p, new(.66f, .38f), new(.21f, .28f));
            q += new Vector2(-.018f * motion.Recoil, -.008f * motion.Cast + .012f * motion.Recoil) * head;
            // Spread the bend along the leg: a narrow vertical bell folds triangles at the wrist.
            var paw = Ramp(.58f, .80f, p.Y) * Ramp(.455f, .49f, p.X)
                * (1 - Ramp(.585f, .62f, p.X));
            // A flat weight over the toes translates the whole round paw without pinching its tip.
            q += new Vector2(.012f * motion.Strike, -.065f * Math.Max(0, motion.Strike)) * paw;
            var tail = (1 - Ramp(.36f, .45f, p.X)) * (1 - Ramp(.54f, .65f, p.Y));
            q += new Vector2(.007f * motion.Sway, -.010f * motion.Sway - .012f * motion.Cast) * tail;
        }
        else
        {
            // Keep both feet and the approved ankle contours planted in every non-death action.
            var upper = 1 - Ramp(.75f, .87f, p.Y);
            q += new Vector2(-.030f * motion.Recoil + .008f * motion.Strike,
                .008f * motion.Recoil - .003f * motion.Breath) * upper;
            var gown = Ramp(.33f, .58f, p.Y) * (1 - Ramp(.73f, .83f, p.Y));
            q.X += .006f * motion.Sway * gown;
            var ribbon = (1 - Ramp(.35f, .44f, p.X)) * Bell(p, new(.32f, .21f), new(.15f, .13f));
            q.X += .008f * motion.Sway * ribbon;
            // The wand and gripping hand rotate together, blending only at the shoulder.
            var arm = Ramp(.61f, .70f, p.X) * Ramp(.12f, .20f, p.Y)
                * (1 - Ramp(.40f, .58f, p.Y));
            var shoulder = new Vector2(.62f, .39f) * _size;
            var angle = -.11f * motion.Cast + .13f * motion.Strike + .04f * motion.Recoil;
            var rotated = (point - shoulder).Rotated(angle) + shoulder;
            q += (rotated - point) / _size * arm;
            var freeHand = Bell(p, new(.43f, .50f), new(.055f, .10f));
            q += new Vector2(-.012f, -.018f) * motion.Cast * freeHand;
        }
        return q * _size;
    }

    private static float Bell(Vector2 point, Vector2 center, Vector2 radius)
    {
        var d = (point - center) / radius;
        return MathF.Exp(-2 * d.LengthSquared());
    }

    private static float Ramp(float from, float to, float value)
    {
        var t = Math.Clamp((value - from) / (to - from), 0, 1);
        return t * t * (3 - 2 * t);
    }

    internal static Vector2 Vec(float[] value) => new(value[0], value[1]);
    internal readonly record struct Motion(float Breath = 0, float Sway = 0, float Cast = 0, float Strike = 0, float Recoil = 0);
    internal sealed class MeshLayout
    {
        [JsonPropertyName("design_size")] public float[] DesignSize { get; set; } = [];
        [JsonPropertyName("feet")] public float[] Feet { get; set; } = [];
        [JsonPropertyName("display_height")] public float DisplayHeight { get; set; }
        [JsonPropertyName("image")] public string Image { get; set; } = "character.png";
        [JsonPropertyName("center")] public float[] Center { get; set; } = [];
        [JsonPropertyName("head")] public float[] Head { get; set; } = [];
        [JsonPropertyName("staff_tip")] public float[] StaffTip { get; set; } = [];
        [JsonPropertyName("halo_center")] public float[] HaloCenter { get; set; } = [];
    }
}
