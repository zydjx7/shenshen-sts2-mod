using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Shenshen.ShenshenCode.Character;

namespace Shenshen.ShenshenCode.Visuals;

/// <summary>
/// Layered Godot animation, dispatched by BaseLib 3.4.7's non-Spine animation bridge.
/// This deliberately does not construct the game's Spine-only CreatureAnimator.
/// </summary>
public sealed class ShenshenVisuals
{
    public const string AssetDirectory = MainFile.ResPath + "/images/character_forms/human/mystery";
    public const string RigPath = AssetDirectory + "/rig.json";
    public const string CatAssetDirectory = MainFile.ResPath + "/images/character_forms/cat/kitten";
    public const string CatRigPath = CatAssetDirectory + "/rig.json";
    public const float AttackDelay = 0.24f;
    public const float CastDelay = 0.28f;
    public const float DeathDuration = 1.2f;

    private static readonly ConditionalWeakTable<NCreatureVisuals, ShenshenVisuals> Controllers = new();
    private readonly NCreatureVisuals _root = new();
    private readonly Node2D _human = new() { Name = "HumanRig" };
    // Painted parts keep smooth linear sampling regardless of the host scene's filter.
    private readonly Node2D _canvas = new() { Name = "Canvas", TextureFilter = CanvasItem.TextureFilterEnum.Linear };
    private readonly Node2D _catCanvas = new() { Name = "CatCanvas", Visible = false };
    private readonly AnimationPlayer _player = new() { Name = "AnimationPlayer" };
    private readonly Dictionary<string, RigPart> _parts;
    private readonly Dictionary<string, Node2D> _pivots = new();
    private readonly Dictionary<string, Sprite2D> _sprites = new();
    private ShenshenForm _form = ShenshenForm.Human;
    private readonly bool _hasCat;
    private readonly bool _traceAnimations = OS.GetCmdlineArgs().Contains("--shenshen-test", StringComparer.Ordinal);

    private ShenshenVisuals(RigLayout layout, RigLayout? catLayout)
    {
        _hasCat = catLayout is { Parts.Length: > 0 };
        foreach (var part in layout.Parts) part.Directory = AssetDirectory;
        foreach (var part in catLayout?.Parts ?? [])
        {
            part.Directory = CatAssetDirectory;
            part.IsCat = true;
        }
        _parts = layout.Parts.Concat(catLayout?.Parts ?? []).ToDictionary(part => part.Id);
        AddUnique(new Control { MouseFilter = Control.MouseFilterEnum.Ignore }, "FormVfx");
        var body = AddUnique(new Node2D(), "Visuals");
        body.Scale = Vector2.One * (layout.DisplayHeight / layout.DesignSize[1]);
        body.AddChild(_human);
        _human.AddChild(_canvas);
        _canvas.Position = -Vector(layout.Feet);
        _human.AddChild(_catCanvas);
        if (catLayout != null)
        {
            var ratio = (catLayout.DisplayHeight / catLayout.DesignSize[1]) / body.Scale.X;
            _catCanvas.Scale = Vector2.One * ratio;
            _catCanvas.Position = -Vector(catLayout.Feet) * ratio;
        }
        foreach (var part in _parts.Values) AddPart(part.Id);
        SortLocalLayers();
        body.AddChild(_player);
        AddUnique(new Control
        {
            Position = new Vector2(-layout.DisplayHeight * 0.43f, -layout.DisplayHeight),
            Size = new Vector2(layout.DisplayHeight * 0.86f, layout.DisplayHeight),
            MouseFilter = Control.MouseFilterEnum.Ignore
        }, "Bounds");
        AddUnique(new Marker2D { Position = new Vector2(0, -layout.DisplayHeight * 0.52f) }, "CenterPos");
        AddUnique(new Marker2D { Position = new Vector2(0, -layout.DisplayHeight - 65) }, "IntentPos");
        AddUnique(new Marker2D { Position = new Vector2(0, -layout.DisplayHeight - 40) }, "OrbPos");
        AddUnique(new Marker2D { Position = new Vector2(0, -layout.DisplayHeight * 0.9f) }, "TalkPos");
        BuildAnimations();
        if (_traceAnimations)
            _player.AnimationStarted += animation => TraceAnimation(animation, "started");
        _root.Ready += () =>
        {
            UpdateEffectAnchors();
            _player.Play("Idle");
        };
        // BaseLib forwards clips, but does not queue an idle after a one-shot animation.
        _player.AnimationFinished += animation =>
        {
            if (_traceAnimations) TraceAnimation(animation, "finished");
            if (animation != "Dead") _player.Play("Idle");
        };
    }

    private void TraceAnimation(StringName animation, string eventName) =>
        MainFile.Logger.Info($"VisualAnimation form={_form} clip={animation} event={eventName}");

    public static NCreatureVisuals Create()
    {
        var json = Godot.FileAccess.GetFileAsString(RigPath);
        var layout = JsonSerializer.Deserialize<RigLayout>(json)
            ?? throw new InvalidOperationException("Missing or invalid Shenshen rig: " + RigPath);
        var cat = Godot.FileAccess.FileExists(CatRigPath)
            ? JsonSerializer.Deserialize<RigLayout>(Godot.FileAccess.GetFileAsString(CatRigPath)) : null;
        var controller = new ShenshenVisuals(layout, cat);
        Controllers.Add(controller._root, controller);
        return controller._root;
    }

    /// <summary>
    /// Entry point for the tracked FormPower state; swaps only the mutually exclusive visual
    /// canvases. It does not own or modify any gameplay state.
    /// </summary>
    public static void SetForm(NCreatureVisuals? visuals, ShenshenForm form, bool animate = true)
    {
        if (visuals == null || !Controllers.TryGetValue(visuals, out var controller)) return;
        if (controller._form == form) return;
        controller._form = form;
        controller._canvas.Visible = form == ShenshenForm.Human || !controller._hasCat;
        controller._catCanvas.Visible = form == ShenshenForm.Cat && controller._hasCat;
        controller.UpdateEffectAnchors();
        if (animate && visuals.IsNodeReady()) controller._player.Play("Cast");
    }

    private void UpdateEffectAnchors()
    {
        // A cat occupies only the lower part of the design canvas. Anchor effects to its
        // actual body rather than the full human-sized canvas height.
        var cat = _catCanvas.Visible;
        if (_sprites.TryGetValue(cat ? "cat_body" : "torso", out var body))
            // The human front gown extends to her calves; impact effects stay on the upper body.
            _root.GetNode<Marker2D>("CenterPos").Position = PartPoint(body,
                cat ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.2f));
        if (_sprites.TryGetValue(cat ? "cat_head" : "head", out var head))
            _root.GetNode<Marker2D>("TalkPos").Position = PartCenter(head);
    }

    private Vector2 PartCenter(Sprite2D sprite) => PartPoint(sprite, new Vector2(0.5f, 0.5f));

    private Vector2 PartPoint(Sprite2D sprite, Vector2 normalizedPosition)
    {
        var rect = sprite.GetRect();
        var point = rect.Position + rect.Size * normalizedPosition;
        // SetForm can run before AddChildSafely has inserted these nodes into the tree.
        for (Node2D node = sprite; node != _root; node = (Node2D)node.GetParent())
            point = node.Transform * point;
        return point;
    }

    private T AddUnique<T>(T node, string name) where T : Node
    {
        node.Name = name;
        _root.AddChild(node);
        node.Owner = _root;
        node.UniqueNameInOwner = true;
        return node;
    }

    private Node2D AddPart(string id)
    {
        if (_pivots.TryGetValue(id, out var existing)) return existing;
        var part = _parts[id];
        var parent = string.IsNullOrEmpty(part.Parent) ? (part.IsCat ? _catCanvas : _canvas) : AddPart(part.Parent);
        var pivot = new Node2D { Name = "Part_" + id, Position = Vector(part.Position), RotationDegrees = part.Rotation };
        parent.AddChild(pivot);
        var path = part.File.StartsWith("res://", StringComparison.Ordinal) ? part.File : part.Directory + "/" + part.File;
        var source = GD.Load<Texture2D>(path) ?? throw new InvalidOperationException("Cannot load Shenshen part: " + path);
        Texture2D texture = part.Region is { Length: 4 } region
            ? new AtlasTexture
            {
                Atlas = source,
                Region = new Rect2(region[0], region[1], region[2], region[3]),
                FilterClip = true
            }
            : source;
        var sprite = new Sprite2D
        {
            Name = "Sprite",
            Texture = texture,
            Centered = false,
            Offset = -Vector(part.Pivot) * texture.GetSize(),
            Scale = Vector(part.Size) / texture.GetSize(),
            Visible = id is not ("head_closed" or "cat_head_closed" or "magic")
        };
        pivot.AddChild(sprite);
        _pivots.Add(id, pivot);
        _sprites.Add(id, sprite);
        return pivot;
    }

    private void SortLocalLayers()
    {
        // The game's combat scene is at Z=-10 and its front VFX are at Z=-9.
        // Real per-part Z values would put limbs behind the room or heads above pause UI.
        // Keep every CanvasItem at Z=0; rig z only orders siblings inside this character.
        var order = new Dictionary<Node, int>();
        foreach (var part in _parts.Values)
        {
            order.Add(_pivots[part.Id], part.Z);
            order.Add(_sprites[part.Id], part.Z);
        }
        foreach (var parent in _pivots.Values.Prepend(_catCanvas).Prepend(_canvas))
        {
            var children = parent.GetChildren().OrderBy(child => order.GetValueOrDefault(child)).ToArray();
            for (var index = 0; index < children.Length; index++)
                parent.MoveChild(children[index], index);
        }
    }

    private string PartPath(string id)
    {
        var part = _parts[id];
        return (string.IsNullOrEmpty(part.Parent) ? "HumanRig/" + (part.IsCat ? "CatCanvas" : "Canvas") : PartPath(part.Parent)) + "/Part_" + id;
    }

    private void BuildAnimations()
    {
        var library = new AnimationLibrary();
        var idle = RestAnimation(3.6f, loop: true);
        Rotate(idle, "torso", (0, -0.008f), (1.8f, 0.008f), (3.6f, -0.008f));
        Rotate(idle, "head", (0, 0.012f), (1.8f, -0.012f), (3.6f, 0.012f));
        // The historical ponytail part now contains the chosen hat's trailing ribbon.
        Rotate(idle, "ponytail", (0, -0.06f), (1.1f, 0.045f), (2.5f, -0.035f), (3.6f, -0.06f));
        Rotate(idle, "cape", (0, -0.025f), (1.8f, 0.03f), (3.6f, -0.025f));
        Rotate(idle, "arm_free", (0, -0.018f), (1.8f, 0.018f), (3.6f, -0.018f));
        Move(idle, "torso", (0, Vector2.Zero), (1.8f, new Vector2(0, -9)), (3.6f, Vector2.Zero));
        Blink(idle, 2.45f, 2.58f);
        Rotate(idle, "cat_body", (0, -0.008f), (1.8f, 0.008f), (3.6f, -0.008f));
        Move(idle, "cat_body", (0, Vector2.Zero), (1.8f, new Vector2(0, -8)), (3.6f, Vector2.Zero));
        Rotate(idle, "cat_head", (0, 0.025f), (1.8f, -0.025f), (3.6f, 0.025f));
        Rotate(idle, "cat_tail", (0, -0.10f), (1.2f, 0.12f), (2.4f, -0.03f), (3.6f, -0.10f));
        Blink(idle, 2.45f, 2.58f, cat: true);
        library.AddAnimation("Idle", idle);

        var attack = RestAnimation(0.62f);
        Rotate(attack, "torso", (0, 0), (0.13f, -0.065f), (AttackDelay, 0.08f), (0.62f, 0));
        Rotate(attack, "arm_staff", (0, 0), (0.13f, -0.36f), (AttackDelay, 0.48f), (0.62f, 0));
        Rotate(attack, "arm_free", (0, 0), (AttackDelay, -0.18f), (0.62f, 0));
        Rotate(attack, "head", (0, 0), (AttackDelay, -0.04f), (0.62f, 0));
        Rotate(attack, "ponytail", (0, 0), (0.3f, -0.14f), (0.62f, 0));
        Rotate(attack, "cape", (0, 0), (0.3f, -0.06f), (0.62f, 0));
        Track(attack, "HumanRig:position", (0, Vector2.Zero), (AttackDelay, new Vector2(35, 0)), (0.62f, Vector2.Zero));
        Magic(attack, 0.16f, 0.38f);
        // Lift and swipe the near paw forward; the other three feet keep supporting the body.
        Rotate(attack, "cat_front", (0, 0), (0.13f, 0.22f), (AttackDelay, -0.78f), (0.39f, -0.42f), (0.62f, 0));
        Move(attack, "cat_front", (0, Vector2.Zero), (AttackDelay, new Vector2(8, -15)), (0.62f, Vector2.Zero));
        foreach (var support in new[] { "cat_front_back", "cat_rear_back", "cat_rear" })
            Move(attack, support, (0, Vector2.Zero), (AttackDelay, new Vector2(-35 / _catCanvas.Scale.X, 0)), (0.62f, Vector2.Zero));
        Rotate(attack, "cat_body", (0, 0), (AttackDelay, 0.04f), (0.62f, 0));
        Rotate(attack, "cat_head", (0, 0), (AttackDelay, -0.06f), (0.62f, 0));
        Rotate(attack, "cat_tail", (0, 0), (0.3f, -0.2f), (0.62f, 0));
        library.AddAnimation("Attack", attack);

        var cast = RestAnimation(0.85f);
        Rotate(cast, "arm_staff", (0, 0), (CastDelay, -0.15f), (0.62f, -0.15f), (0.85f, 0));
        Rotate(cast, "arm_free", (0, 0), (CastDelay, -0.05f), (0.62f, -0.05f), (0.85f, 0));
        // Bring the normally concealed free hand out from the long cloak to cast.
        Move(cast, "arm_free", (0, Vector2.Zero), (CastDelay, new Vector2(250, 20)),
            (0.62f, new Vector2(250, 20)), (0.85f, Vector2.Zero));
        Rotate(cast, "head", (0, 0), (CastDelay, -0.035f), (0.85f, 0));
        Rotate(cast, "cape", (0, 0), (0.45f, 0.06f), (0.85f, 0));
        Rotate(cast, "ponytail", (0, 0), (0.45f, -0.09f), (0.85f, 0));
        Magic(cast, 0.18f, 0.7f);
        Scale(cast, "magic", (0, new Vector2(0.8f, 0.8f)), (CastDelay, Vector2.One),
            (0.55f, new Vector2(1.12f, 1.12f)), (0.85f, new Vector2(0.8f, 0.8f)));
        Rotate(cast, "magic", (0, -0.08f), (0.55f, 0.1f), (0.85f, 0.16f));
        Rotate(cast, "cat_head", (0, 0), (CastDelay, -0.10f), (0.85f, 0));
        Rotate(cast, "cat_tail", (0, 0), (0.45f, 0.25f), (0.85f, 0));
        Move(cast, "cat_body", (0, Vector2.Zero), (CastDelay, new Vector2(0, -8)), (0.85f, Vector2.Zero));
        library.AddAnimation("Cast", cast);
        library.AddAnimation("PowerUp", (Animation)cast.Duplicate());

        var hit = RestAnimation(0.42f);
        Track(hit, "HumanRig:position", (0, Vector2.Zero), (0.07f, new Vector2(-32, 0)), (0.42f, Vector2.Zero));
        Track(hit, "HumanRig:modulate", (0, Colors.White), (0.06f, new Color(1f, 0.58f, 0.7f)), (0.42f, Colors.White));
        Rotate(hit, "torso", (0, 0), (0.08f, -0.085f), (0.42f, 0));
        Rotate(hit, "head", (0, 0), (0.1f, -0.12f), (0.42f, 0));
        Rotate(hit, "arm_staff", (0, 0), (0.1f, 0.10f), (0.42f, 0));
        Rotate(hit, "arm_free", (0, 0), (0.1f, 0.12f), (0.42f, 0));
        Rotate(hit, "ponytail", (0, 0), (0.18f, 0.18f), (0.42f, 0));
        Blink(hit, 0.02f, 0.26f);
        Rotate(hit, "cat_body", (0, 0), (0.08f, -0.08f), (0.42f, 0));
        Move(hit, "cat_body", (0, Vector2.Zero), (0.1f, new Vector2(-10, 12)), (0.42f, Vector2.Zero));
        Scale(hit, "cat_body", (0, Vector2.One), (0.1f, new Vector2(0.98f, 0.94f)), (0.42f, Vector2.One));
        Rotate(hit, "cat_head", (0, 0), (0.1f, 0.09f), (0.42f, 0));
        Move(hit, "cat_head", (0, Vector2.Zero), (0.1f, new Vector2(-8, 5)), (0.42f, Vector2.Zero));
        Rotate(hit, "cat_tail", (0, 0), (0.18f, 0.2f), (0.42f, 0));
        Rotate(hit, "cat_front", (0, 0), (0.1f, 0.16f), (0.42f, 0));
        Rotate(hit, "cat_front_back", (0, 0), (0.1f, 0.09f), (0.42f, 0));
        Move(hit, "cat_front", (0, Vector2.Zero), (0.1f, new Vector2(-9, 3)), (0.42f, Vector2.Zero));
        Blink(hit, 0.02f, 0.26f, cat: true);
        library.AddAnimation("Hit", hit);

        var dead = RestAnimation(DeathDuration);
        Track(dead, "HumanRig:position", (0, Vector2.Zero), (0.5f, new Vector2(-35, 30)), (DeathDuration, new Vector2(-65, 70)));
        Track(dead, "HumanRig:rotation", (0, 0f), (0.5f, -0.16f), (DeathDuration, -0.8f));
        Rotate(dead, "torso", (0, 0), (DeathDuration, -0.2f));
        Rotate(dead, "head", (0, 0), (DeathDuration, 0.25f));
        Rotate(dead, "arm_staff", (0, 0), (DeathDuration, 0.4f));
        Rotate(dead, "arm_free", (0, 0), (DeathDuration, -0.3f));
        Rotate(dead, "cat_body", (0, 0), (DeathDuration, -0.16f));
        Rotate(dead, "cat_head", (0, 0), (DeathDuration, 0.25f));
        Rotate(dead, "cat_tail", (0, 0), (DeathDuration, -0.2f));
        Rotate(dead, "cat_front", (0, 0), (DeathDuration, 0.25f));
        if (_sprites.ContainsKey("head_closed"))
        {
            Visibility(dead, "head", (0, true), (0.2f, false));
            Visibility(dead, "head_closed", (0, false), (0.2f, true));
        }
        if (_sprites.ContainsKey("cat_head_closed"))
        {
            Visibility(dead, "cat_head", (0, true), (0.2f, false));
            Visibility(dead, "cat_head_closed", (0, false), (0.2f, true));
        }
        library.AddAnimation("Dead", dead);
        library.AddAnimation("Revive", RestAnimation(0.5f));
        library.AddAnimation("Relaxed", (Animation)idle.Duplicate());
        _player.AddAnimationLibrary("", library);
    }

    private Animation RestAnimation(float length, bool loop = false)
    {
        var animation = new Animation { Length = length, LoopMode = loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None };
        Track(animation, "HumanRig:position", (0, Vector2.Zero));
        Track(animation, "HumanRig:rotation", (0, 0f));
        Track(animation, "HumanRig:modulate", (0, Colors.White));
        foreach (var part in _parts.Values)
        {
            Track(animation, PartPath(part.Id) + ":rotation", (0, Mathf.DegToRad(part.Rotation)));
            Track(animation, PartPath(part.Id) + ":position", (0, Vector(part.Position)));
            Track(animation, PartPath(part.Id) + ":scale", (0, Vector2.One));
        }
        Visibility(animation, "head", (0, true));
        Visibility(animation, "head_closed", (0, false));
        Visibility(animation, "cat_head", (0, true));
        Visibility(animation, "cat_head_closed", (0, false));
        Visibility(animation, "magic", (0, false));
        return animation;
    }

    private void Rotate(Animation animation, string id, params (float Time, float Value)[] keys)
    {
        if (!_parts.TryGetValue(id, out var part)) return;
        Track(animation, PartPath(id) + ":rotation", keys.Select(key =>
            (key.Time, Variant.From(Mathf.DegToRad(part.Rotation) + key.Value))).ToArray());
    }

    private void Move(Animation animation, string id, params (float Time, Vector2 Value)[] keys)
    {
        if (!_parts.TryGetValue(id, out var part)) return;
        Track(animation, PartPath(id) + ":position", keys.Select(key =>
            (key.Time, Variant.From(Vector(part.Position) + key.Value))).ToArray());
    }

    private void Scale(Animation animation, string id, params (float Time, Vector2 Value)[] keys)
    {
        if (!_parts.ContainsKey(id)) return;
        Track(animation, PartPath(id) + ":scale", keys.Select(key =>
            (key.Time, Variant.From(key.Value))).ToArray());
    }

    private void Blink(Animation animation, float start, float end, bool cat = false)
    {
        var head = cat ? "cat_head" : "head";
        var closed = cat ? "cat_head_closed" : "head_closed";
        if (!_sprites.ContainsKey(closed)) return;
        Visibility(animation, head, (0, true), (start, false), (end, true));
        Visibility(animation, closed, (0, false), (start, true), (end, false));
    }

    private void Magic(Animation animation, float start, float end) =>
        Visibility(animation, "magic", (0, false), (start, true), (end, false));

    private void Visibility(Animation animation, string id, params (float Time, bool Value)[] keys)
    {
        if (!_sprites.ContainsKey(id)) return;
        var index = Track(animation, PartPath(id) + "/Sprite:visible", keys.Select(key =>
            (key.Time, Variant.From(key.Value))).ToArray());
        animation.ValueTrackSetUpdateMode(index, Animation.UpdateMode.Discrete);
    }

    private static int Track(Animation animation, string path, params (float Time, Variant Value)[] keys)
    {
        var nodePath = new NodePath(path);
        var existing = animation.FindTrack(nodePath, Animation.TrackType.Value);
        if (existing >= 0) animation.RemoveTrack(existing);
        var track = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackSetPath(track, nodePath);
        foreach (var (time, value) in keys) animation.TrackInsertKey(track, time, value);
        return track;
    }

    private static Vector2 Vector(float[] value) => new(value[0], value[1]);

    private sealed class RigLayout
    {
        [JsonPropertyName("design_size")] public float[] DesignSize { get; set; } = [1000, 1400];
        [JsonPropertyName("feet")] public float[] Feet { get; set; } = [500, 1340];
        [JsonPropertyName("display_height")] public float DisplayHeight { get; set; } = 300;
        [JsonPropertyName("parts")] public RigPart[] Parts { get; set; } = [];
    }

    private sealed class RigPart
    {
        internal string Directory { get; set; } = AssetDirectory;
        internal bool IsCat { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("file")] public string File { get; set; } = "";
        [JsonPropertyName("parent")] public string? Parent { get; set; }
        [JsonPropertyName("position")] public float[] Position { get; set; } = [0, 0];
        [JsonPropertyName("size")] public float[] Size { get; set; } = [100, 100];
        [JsonPropertyName("pivot")] public float[] Pivot { get; set; } = [0.5f, 0.5f];
        [JsonPropertyName("region")] public float[]? Region { get; set; }
        [JsonPropertyName("rotation")] public float Rotation { get; set; }
        [JsonPropertyName("z")] public int Z { get; set; }
    }
}
