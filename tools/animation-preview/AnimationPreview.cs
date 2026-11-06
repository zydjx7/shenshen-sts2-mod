using System.Runtime.Loader;
using Godot;
using Shenshen.ShenshenCode.Visuals;
using Shenshen.ShenshenCode.Character;

public partial class AnimationPreview : Node
{
    private MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals? _movieRoot;
    private AnimationPlayer? _moviePlayer;
    private Label? _movieLabel;
    private double _time;
    private int _stage = -1;

    public override async void _Ready()
    {
        try
        {
            var gameData = System.Environment.GetEnvironmentVariable("SHENSHEN_GAME_DATA")!;
            AssemblyLoadContext.Default.Resolving += (_, name) =>
            {
                var path = Path.Combine(gameData, name.Name + ".dll");
                return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
            };
            var root = ShenshenVisuals.Create();
            root.Position = new Vector2(260, 710);
            root.Scale = Vector2.One * 2;
            // Match the installed game's combat scene/background Z relationship.
            AddChild(new ColorRect { Name = "CombatBackground", Size = new Vector2(600, 760),
                Color = new Color(0.1f, 0.08f, 0.15f), ZIndex = -11 });
            var scene = new Node2D { Name = "CombatSceneContainer", ZIndex = -10 };
            AddChild(scene);
            scene.AddChild(root);
            var player = root.GetNode<AnimationPlayer>("Visuals/AnimationPlayer");
            player.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
            AddChild(new Label { Position = new Vector2(20, 20), Text = "Independent Godot preview - not in-game footage" });
            var label = new Label { Position = new Vector2(20, 45), Text = "Girl / Idle" };
            AddChild(label);
            if (System.Environment.GetEnvironmentVariable("SHENSHEN_PREVIEW_MOVIE") == "1")
            {
                _movieRoot = root;
                _moviePlayer = player;
                _movieLabel = label;
                return;
            }
            foreach (var frame in new[] { ("Idle", 0f, "idle"), ("Idle", .9f, "idle-breathe"),
                ("Cast", .28f, "cast"), ("Cast", .55f, "cast-hold"),
                ("Attack", .13f, "attack-windup"), ("Attack", .24f, "attack"),
                ("Attack", .39f, "attack-recover"), ("Hit", .08f, "hurt"),
                ("Idle", 0f, "restored-idle") })
            {
                label.Text = $"Girl / {frame.Item1} @ {frame.Item2:0.00}s";
                player.Play(frame.Item1);
                player.Seek(frame.Item2, true);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var path = ProjectSettings.GlobalizePath("res://" + frame.Item3 + ".png");
                var error = GetViewport().GetTexture().GetImage().SavePng(path);
                if (error != Error.Ok) throw new IOException("Screenshot failed: " + error);
                GD.Print("SHENSHEN_PREVIEW " + path);
            }
            ShenshenVisuals.SetForm(root, ShenshenForm.Cat, animate: false);
            foreach (var frame in new[] { ("Idle", 0f, "cat-idle"), ("Idle", .9f, "cat-idle-breathe"),
                ("Cast", .28f, "cat-cast"), ("Attack", .13f, "cat-attack-windup"),
                ("Attack", .24f, "cat-attack"), ("Attack", .39f, "cat-attack-recover"),
                ("Hit", .1f, "cat-hurt"), ("Idle", 0f, "cat-restored-idle") })
            {
                label.Text = $"Kitten / {frame.Item1} @ {frame.Item2:0.00}s";
                player.Play(frame.Item1);
                player.Seek(frame.Item2, true);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var path = ProjectSettings.GlobalizePath("res://" + frame.Item3 + ".png");
                var error = GetViewport().GetTexture().GetImage().SavePng(path);
                if (error != Error.Ok) throw new IOException("Screenshot failed: " + error);
                GD.Print("SHENSHEN_PREVIEW " + path);
            }
            ShenshenVisuals.SetForm(root, ShenshenForm.Human, animate: false);
            player.Play("Idle");
            player.Seek(0, true);
            label.Text = "Combat layers / pause overlay Z=0";
            AddChild(new ColorRect { Size = new Vector2(600, 760), Color = new Color(0, 0, 0, 0.55f), ZIndex = 0 });
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var pauseError = GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://pause-layers.png"));
            if (pauseError != Error.Ok) throw new IOException("Pause screenshot failed: " + pauseError);
            GD.Print("SHENSHEN_PREVIEW_FRAMES_DONE");
            GetTree().Quit(0);
        }
        catch (Exception e)
        {
            GD.PrintErr("SHENSHEN_PREVIEW_FAIL " + e);
            GetTree().Quit(1);
        }
    }

    public override void _Process(double delta)
    {
        if (_movieRoot == null || _moviePlayer == null || _movieLabel == null) return;
        _time += delta;
        var stage = _time < 2 ? 0 : _time < 3.2 ? 1 : _time < 4.2 ? 2 : _time < 5.2 ? 3 :
            _time < 6.2 ? 4 : _time < 8.2 ? 5 : _time < 9.3 ? 6 : _time < 10.3 ? 7 :
            _time < 11.3 ? 8 : _time < 13.3 ? 9 : 10;
        if (stage != _stage)
        {
            _stage = stage;
            var clip = stage switch { 1 or 4 or 8 => "Cast", 2 or 6 => "Attack", 3 or 7 => "Hit", _ => "Idle" };
            var kitten = stage is 4 or 5 or 6 or 7;
            ShenshenVisuals.SetForm(_movieRoot, kitten ? ShenshenForm.Cat : ShenshenForm.Human, animate: stage is 4 or 8);
            _moviePlayer.Play(clip);
            if (stage is 0 or 5) _moviePlayer.Seek(1.0, true);
            _movieLabel.Text = stage switch
            {
                4 => "Independent Godot / Transform to kitten",
                8 => "Independent Godot / Transform to girl",
                10 => "Independent Godot / Pause overlay layer test",
                _ => (kitten ? "Kitten / " : "Girl / ") + clip
            };
            if (stage == 10)
                AddChild(new ColorRect { Name = "PauseOverlay", Size = new Vector2(600, 760),
                    Color = new Color(0, 0, 0, 0.55f), ZIndex = 0 });
        }
        if (stage != 10) _moviePlayer.Advance(delta);
        if (_time > 14.8)
        {
            GD.Print("SHENSHEN_PREVIEW_MOVIE_DONE");
            GetTree().Quit(0);
        }
    }
}
