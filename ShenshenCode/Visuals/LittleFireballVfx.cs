using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Shenshen.ShenshenCode.Visuals;

/// <summary>Visual only: the native attack command waits for arrival before dealing damage.</summary>
public static class LittleFireballVfx
{
    public const float FlightDuration = 0.24f;

    public static async Task Play(Creature attacker, Creature target)
    {
        var room = NCombatRoom.Instance;
        var source = attacker.GetCreatureNode();
        var receiver = target.GetCreatureNode();
        // Headless simulations and previews without an active battle do not wait for VFX.
        if (room == null || source == null || receiver == null || !room.IsInsideTree()) return;
        var staff = source.Visuals.GetNodeOrNull<Marker2D>(ShenshenVisuals.StaffTipPath);
        var start = staff?.GlobalPosition ?? source.VfxSpawnPosition;
        var end = receiver.VfxSpawnPosition;
        var projectile = new Node2D { Name = "ShenshenLittleFireball" };
        var flame = new Sprite2D { Texture = CreateFlameTexture() };
        flame.Scale = Vector2.One * (82f / flame.Texture.GetHeight());
        flame.Rotation = (end - start).Angle() - Mathf.Pi / 4f;
        var direction = (end - start).Normalized();
        var trail = new Line2D
        {
            Width = 8f,
            DefaultColor = new Color("ff9fb8"),
            Points = [Vector2.Zero, -direction * 24f, -direction * 58f],
            Gradient = new Gradient
            {
                Colors = [new Color(1f, 0.85f, 0.61f, 0.9f), new Color(1f, 0.4f, 0.65f, 0f)]
            },
            Antialiased = true
        };
        projectile.AddChild(trail);
        projectile.AddChild(flame);
        var impact = CreateImpact();
        impact.Visible = false;
        projectile.AddChild(impact);
        var arrival = new TaskCompletionSource();
        var container = room.CombatVfxContainer;
        var arrived = false;
        void CompleteOnExit() => arrival.TrySetResult();
        void StartFlight()
        {
            projectile.GlobalPosition = start;
            var trace = OS.GetCmdlineArgs().Contains("--shenshen-test", StringComparer.Ordinal);
            if (trace) MainFile.Logger.Info($"LittleFireballVfx event=launched from={start} to={end}");
            var tween = projectile.CreateTween();
            tween.TweenProperty(projectile, "global_position", end, FlightDuration);
            tween.TweenCallback(Callable.From(() =>
            {
                flame.Visible = false;
                trail.Visible = false;
                impact.Visible = true;
                arrived = true;
                if (trace) MainFile.Logger.Info("LittleFireballVfx event=arrived");
                arrival.TrySetResult();
            }));
            // The hit flash fades independently while the native damage/hit reaction resolves.
            tween.TweenProperty(impact, "scale", Vector2.One * 1.8f, 0.18f);
            tween.Parallel().TweenProperty(impact, "modulate:a", 0f, 0.18f);
            tween.TweenCallback(Callable.From(projectile.QueueFree));
        }
        // AddChildSafely can defer insertion. The host may exit before the projectile
        // ever enters the tree, in which case neither its Ready nor TreeExiting fires.
        container.TreeExiting += CompleteOnExit;
        projectile.TreeExiting += CompleteOnExit;
        projectile.Ready += StartFlight;
        try
        {
            if (!container.IsInsideTree()) return;
            container.AddChildSafely(projectile);
            await arrival.Task;
        }
        finally
        {
            if (GodotObject.IsInstanceValid(container)) container.TreeExiting -= CompleteOnExit;
            if (GodotObject.IsInstanceValid(projectile))
            {
                projectile.Ready -= StartFlight;
                projectile.TreeExiting -= CompleteOnExit;
                if (!arrived && !projectile.IsQueuedForDeletion()) projectile.QueueFree();
            }
        }
    }

    private static Texture2D CreateFlameTexture()
    {
        return ResourceLoader.Load<Texture2D>(MainFile.ResPath + "/images/vfx/little-fireball.tres");
    }

    private static Node2D CreateImpact()
    {
        var impact = new Node2D { Name = "HitFlash" };
        var ring = new Line2D
        {
            Width = 3.5f, DefaultColor = new Color("ffe9b1"), Antialiased = true,
            Points = Enumerable.Range(0, 25).Select(index =>
                Vector2.FromAngle(index * Mathf.Tau / 24f) * 16f).ToArray()
        };
        impact.AddChild(ring);
        for (var index = 0; index < 6; index++)
        {
            var direction = Vector2.FromAngle(index * Mathf.Tau / 6f);
            impact.AddChild(new Line2D
            {
                Width = 4f, DefaultColor = new Color("ffb5ca"), Antialiased = true,
                Points = [direction * 20f, direction * 31f]
            });
        }
        return impact;
    }
}
