using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Shenshen.ShenshenCode.Character;
using static Shenshen.ShenshenCode.Visuals.CharacterMesh;

namespace Shenshen.ShenshenCode.Visuals;

/// <summary>Continuous 2D mesh animation through BaseLib's existing non-Spine trigger bridge.</summary>
public sealed class ShenshenVisuals
{
    public const string AssetDirectory = MainFile.ResPath + "/images/character_forms/human/solar-b5";
    public const string RigPath = AssetDirectory + "/rig.json";
    public const string CatAssetDirectory = MainFile.ResPath + "/images/character_forms/cat/kitten-faithful";
    public const string CatRigPath = CatAssetDirectory + "/rig.json";
    public const string StaffTipPath = "Visuals/HumanRig/Canvas/Mesh/StaffTip";
    public const float AttackDelay = .24f, CastDelay = .28f, DeathDuration = 1.2f;

    private static readonly ConditionalWeakTable<NCreatureVisuals, ShenshenVisuals> Controllers = new();
    private readonly NCreatureVisuals _root = new();
    private readonly Node2D _human = new() { Name = "HumanRig" };
    private readonly Node2D _canvas = new() { Name = "Canvas" };
    private readonly Node2D _catCanvas = new() { Name = "CatCanvas", Visible = false };
    private readonly AnimationPlayer _player = new() { Name = "AnimationPlayer" };
    private readonly CharacterMesh _girl = new(AssetDirectory, false);
    private readonly CharacterMesh _cat = new(CatAssetDirectory, true);
    private ShenshenForm _form;

    private ShenshenVisuals()
    {
        AddUnique(new Control { MouseFilter = Control.MouseFilterEnum.Ignore }, "FormVfx");
        var body = AddUnique(new Node2D(), "Visuals");
        body.AddChild(_human);
        _human.AddChild(_canvas);
        _human.AddChild(_catCanvas);
        SetCanvas(_canvas, _girl);
        SetCanvas(_catCanvas, _cat);
        var halo = GD.Load<PackedScene>(AssetDirectory + "/solar-ring.tscn").Instantiate<Node2D>();
        halo.Name = "SolarRing";
        halo.Position = Vec(_girl.Layout.HaloCenter);
        _canvas.AddChild(halo);
        _canvas.MoveChild(halo, 0);
        _girl.Node.AddChild(new Marker2D { Name = "StaffTip", Position = Vec(_girl.Layout.StaffTip) });
        var spell = new Node2D { Name = "Spell", Modulate = new Color(1, 1, 1, 0) };
        _girl.Node.GetNode("StaffTip").AddChild(spell);
        spell.AddChild(new Line2D
        {
            Name = "Ring", Width = 5, Antialiased = true, DefaultColor = new Color("ffe4a2"),
            Points = Enumerable.Range(0, 33).Select(i => Vector2.FromAngle(i * Mathf.Tau / 32) * 45).ToArray()
        });
        var claws = new Node2D { Name = "ClawSwipe", Modulate = new Color(1, 1, 1, 0) };
        _cat.Node.AddChild(claws);
        for (var i = 0; i < 3; i++)
            claws.AddChild(new Line2D
            {
                Width = 5, Antialiased = true, DefaultColor = new Color("ffe8c6"),
                Points = [new(850 + i * 22, 770), new(900 + i * 22, 728), new(950 + i * 22, 715)]
            });
        body.AddChild(_player);
        var height = _girl.Layout.DisplayHeight;
        AddUnique(new Control { Position = new(-height * .45f, -height), Size = new(height * .9f, height),
            MouseFilter = Control.MouseFilterEnum.Ignore }, "Bounds");
        AddUnique(new Marker2D(), "CenterPos");
        AddUnique(new Marker2D { Position = new(0, -height - 55) }, "IntentPos");
        AddUnique(new Marker2D { Position = new(0, -height - 30) }, "OrbPos");
        AddUnique(new Marker2D(), "TalkPos");
        BuildAnimations();
        if (OS.GetCmdlineArgs().Contains("--shenshen-test", StringComparer.Ordinal))
        {
            _player.AnimationStarted += clip => MainFile.Logger.Info($"VisualAnimation form={_form} clip={clip} event=started");
            _player.AnimationFinished += clip => MainFile.Logger.Info($"VisualAnimation form={_form} clip={clip} event=finished");
        }
        _root.Ready += () => { UpdateEffectAnchors(); _player.Play("Idle"); };
        _player.AnimationFinished += clip => { if (clip != "Dead") _player.Play("Idle"); };
    }

    public static NCreatureVisuals Create()
    {
        var controller = new ShenshenVisuals();
        Controllers.Add(controller._root, controller);
        return controller._root;
    }

    public static void SetForm(NCreatureVisuals? visuals, ShenshenForm form, bool animate = true)
    {
        if (visuals == null || !Controllers.TryGetValue(visuals, out var controller) || controller._form == form) return;
        controller._form = form;
        controller._canvas.Visible = form == ShenshenForm.Human;
        controller._catCanvas.Visible = form == ShenshenForm.Cat;
        controller.UpdateEffectAnchors();
        if (animate && visuals.IsNodeReady()) controller._player.Play("Cast");
    }

    private static void SetCanvas(Node2D canvas, CharacterMesh mesh)
    {
        canvas.Scale = Vector2.One * mesh.Layout.DisplayHeight / mesh.Layout.DesignSize[1];
        canvas.Position = -Vec(mesh.Layout.Feet) * canvas.Scale;
        canvas.AddChild(mesh.Node);
    }

    private void UpdateEffectAnchors()
    {
        var mesh = _form == ShenshenForm.Cat ? _cat : _girl;
        var canvas = _form == ShenshenForm.Cat ? _catCanvas : _canvas;
        _root.GetNode<Marker2D>("CenterPos").Position = canvas.Position + Vec(mesh.Layout.Center) * canvas.Scale;
        _root.GetNode<Marker2D>("TalkPos").Position = canvas.Position + Vec(mesh.Layout.Head) * canvas.Scale;
    }

    private T AddUnique<T>(T node, string name) where T : Node
    {
        node.Name = name; _root.AddChild(node); node.Owner = _root; node.UniqueNameInOwner = true;
        return node;
    }

    private void BuildAnimations()
    {
        var library = new AnimationLibrary();
        var idle = Clip(3.6f, true,
            (0, new()), (.9f, new(1, 1)), (1.8f, new()), (2.7f, new(-1, -1)), (3.6f, new()));
        library.AddAnimation("Idle", idle);
        library.AddAnimation("Relaxed", (Animation)idle.Duplicate());
        var attack = Clip(.62f, false, (0, new()), (.13f, new(Sway: -.4f, Strike: -.25f)),
            (AttackDelay, new(Sway: .7f, Strike: 1)), (.39f, new(Sway: 1, Strike: .5f)), (.62f, new()));
        Spell(attack, .14f, .24f, .39f, .62f);
        Track(attack, "HumanRig/CatCanvas/Mesh/ClawSwipe:modulate", (0, new Color(1, 1, 1, 0)),
            (.15f, new Color(1, 1, 1, 0)), (.24f, Colors.White), (.4f, new Color(1, 1, 1, 0)));
        library.AddAnimation("Attack", attack);
        var cast = Clip(.85f, false, (0, new()), (CastDelay, new(Breath: 1, Sway: .6f, Cast: 1)),
            (.55f, new(Breath: .8f, Sway: 1, Cast: 1)), (.85f, new()));
        Spell(cast, .10f, .28f, .58f, .85f);
        library.AddAnimation("Cast", cast);
        library.AddAnimation("PowerUp", (Animation)cast.Duplicate());
        var hit = Clip(.42f, false, (0, new()), (.08f, new(Sway: -.6f, Recoil: 1)),
            (.22f, new(Sway: .8f, Recoil: .4f)), (.42f, new()));
        Track(hit, "HumanRig:modulate", (0, Colors.White), (.06f, new Color(1, .65f, .72f)), (.42f, Colors.White));
        library.AddAnimation("Hit", hit);
        var dead = Clip(DeathDuration, false, (0, new()), (DeathDuration, new(Recoil: 1)));
        Track(dead, "HumanRig:rotation", (0, 0f), (DeathDuration, -.65f));
        Track(dead, "HumanRig:position", (0, Vector2.Zero), (DeathDuration, new Vector2(-20, 20)));
        library.AddAnimation("Dead", dead);
        library.AddAnimation("Revive", Clip(.5f, false, (0, new()), (.5f, new())));
        _player.AddAnimationLibrary("", library);
    }

    private Animation Clip(float duration, bool loop, params (float Time, Motion Pose)[] keys)
    {
        var clip = new Animation { Length = duration, LoopMode = loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None };
        Track(clip, "HumanRig:position", (0, Vector2.Zero));
        Track(clip, "HumanRig:rotation", (0, 0f));
        Track(clip, "HumanRig:modulate", (0, Colors.White));
        Track(clip, "HumanRig/Canvas/Mesh/StaffTip/Spell:modulate", (0, new Color(1, 1, 1, 0)));
        Track(clip, "HumanRig/Canvas/Mesh/StaffTip/Spell:scale", (0, Vector2.One));
        Track(clip, "HumanRig/CatCanvas/Mesh/ClawSwipe:modulate", (0, new Color(1, 1, 1, 0)));
        Track(clip, "HumanRig/Canvas/Mesh:polygon", keys.Select(k => (k.Time, Variant.From(_girl.Pose(k.Pose)))).ToArray());
        Track(clip, "HumanRig/CatCanvas/Mesh:polygon", keys.Select(k => (k.Time, Variant.From(_cat.Pose(k.Pose)))).ToArray());
        Track(clip, "HumanRig/Canvas/Mesh/StaffTip:position", keys.Select(k =>
            (k.Time, Variant.From(_girl.Warp(Vec(_girl.Layout.StaffTip), k.Pose)))).ToArray());
        Track(clip, "HumanRig/Canvas/SolarRing:modulate", keys.Select(k =>
            (k.Time, Variant.From(new Color(1, 1, 1, .82f + .16f * k.Pose.Cast + .04f * k.Pose.Breath)))).ToArray());
        return clip;
    }

    private static void Spell(Animation clip, float start, float peak, float hold, float end)
    {
        const string path = "HumanRig/Canvas/Mesh/StaffTip/Spell";
        Track(clip, path + ":modulate", (0, new Color(1, 1, 1, 0)), (start, new Color(1, 1, 1, 0)),
            (peak, Colors.White), (hold, Colors.White), (end, new Color(1, 1, 1, 0)));
        Track(clip, path + ":scale", (0, Vector2.One * .45f), (peak, Vector2.One), (hold, Vector2.One * 1.2f), (end, Vector2.One * 1.4f));
    }

    private static void Track(Animation animation, string path, params (float Time, Variant Value)[] keys)
    {
        var nodePath = new NodePath(path);
        var old = animation.FindTrack(nodePath, Animation.TrackType.Value);
        if (old >= 0) animation.RemoveTrack(old);
        var track = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackSetPath(track, nodePath);
        foreach (var (time, value) in keys) animation.TrackInsertKey(track, time, value);
    }
}
