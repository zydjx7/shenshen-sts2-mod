using System.Reflection;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Visuals;

// Runs inside the existing isolated MegaDot harness with the real game, BaseLib and packed art.
internal static class AnimationChecks
{
    internal static void Run(Node host, Action<bool, string> check)
    {
        CheckTriggerBridge(check);
        var restored = ShenshenVisuals.Create();
        ShenshenVisuals.SetForm(restored, ShenshenForm.Cat, animate: false);
        host.AddChild(restored);
        check(!restored.GetNode<Node2D>("Visuals/HumanRig/Canvas").Visible &&
            restored.GetNode<Node2D>("Visuals/HumanRig/CatCanvas").Visible &&
            restored.GetNode<AnimationPlayer>("Visuals/AnimationPlayer").CurrentAnimation == "Idle",
            "tracked cat restored before tree insertion survives Ready idle initialization");
        host.RemoveChild(restored);
        restored.Free();
        var visuals = ShenshenVisuals.Create();
        host.AddChild(visuals);
        try
        {
            var player = visuals.GetNode<AnimationPlayer>("Visuals/AnimationPlayer");
            player.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
            var human = visuals.GetNode<Node2D>("Visuals/HumanRig/Canvas");
            var cat = visuals.GetNode<Node2D>("Visuals/HumanRig/CatCanvas");
            var torso = human.GetNode<Node2D>("Part_torso");
            var head = torso.GetNode<Sprite2D>("Part_head/Sprite");
            var closed = torso.GetNode<Sprite2D>("Part_head/Part_head_closed/Sprite");
            var ponytail = torso.GetNode<Node2D>("Part_head/Part_ponytail");
            var arm = torso.GetNode<Node2D>("Part_arm_staff");
            var staff = arm.GetNode<Node2D>("Part_staff");
            check(!visuals.HasSpineAnimation && visuals.Body.Name == "Visuals" && visuals.Bounds.Size.Y > 0,
                "layered visuals ready required nodes without Spine");
            check(player.CurrentAnimation == "Idle" && human.Visible && !cat.Visible,
                "layered visuals start human idle automatically");
            var torsoRest = torso.Position;
            var tailRest = ponytail.Rotation;
            player.Advance(1.1);
            check(torso.Position != torsoRest && !Mathf.IsEqualApprox(ponytail.Rotation, tailRest),
                "idle breathes body and sways a separate ponytail layer");
            player.Seek(2.5, true);
            check(!head.Visible && closed.Visible, "idle blink swaps matching full head layers");
            player.Seek(2.7, true);
            check(head.Visible && !closed.Visible, "idle blink reopens eyes");
            var armRest = arm.Rotation;
            Play(player, "Attack", ShenshenVisuals.AttackDelay);
            check(!Mathf.IsEqualApprox(arm.Rotation, armRest) && staff.GetParent() == arm,
                "attack rotates a separate shoulder pivot carrying the staff");
            Finish(player);
            check(player.CurrentAnimation == "Idle", "attack completion returns to idle");
            Play(player, "Cast", ShenshenVisuals.CastDelay);
            check(!Mathf.IsEqualApprox(arm.Rotation, armRest), "cast moves the staff arm independently");
            Finish(player);
            check(player.CurrentAnimation == "Idle", "cast completion returns to idle");
            Play(player, "Hit", 0.08f);
            check(visuals.GetNode<Node2D>("Visuals/HumanRig").Position != Vector2.Zero,
                "hit visibly recoils the rig");
            Finish(player);
            check(player.CurrentAnimation == "Idle", "hit completion returns to idle");
            Play(player, "Dead", ShenshenVisuals.DeathDuration + 0.1f);
            check(player.CurrentAnimation != "Idle" && Math.Abs(visuals.GetNode<Node2D>("Visuals/HumanRig").Rotation) > 0.1,
                "death holds a collapsed final pose instead of returning to idle");
            Play(player, "Revive", 0.6f);
            player.Advance(0);
            check(player.CurrentAnimation == "Idle" && Mathf.IsZeroApprox(visuals.GetNode<Node2D>("Visuals/HumanRig").Rotation),
                "revive clears death transforms and returns to idle");

            var humanCenter = visuals.VfxSpawnPosition.Position;
            ShenshenVisuals.SetForm(visuals, ShenshenForm.Cat, animate: false);
            check(!human.Visible && cat.Visible, "cat transformation displays exactly one form canvas");
            check(player.CurrentAnimation == "Idle", "restoring tracked cat form does not start a new action");
            check(visuals.VfxSpawnPosition.Position.Y > humanCenter.Y,
                "cat hit effects move down to the actual smaller body");
            var catBody = cat.GetNode<Node2D>("Part_cat_body");
            var catTail = cat.GetNode<Node2D>("Part_cat_tail");
            var catHead = catBody.GetNode<Sprite2D>("Part_cat_head/Sprite");
            var catClosed = catBody.GetNode<Sprite2D>("Part_cat_head/Part_cat_head_closed/Sprite");
            var catPaw = cat.GetNode<Node2D>("Part_cat_front");
            Play(player, "Idle", 0);
            var catRest = catBody.Position;
            var catTailRest = catTail.Rotation;
            player.Advance(1.1);
            check(catBody.Position != catRest && !Mathf.IsEqualApprox(catTail.Rotation, catTailRest),
                "cat idle breathes body and sways a separate tail layer");
            player.Seek(2.5, true);
            check(!catHead.Visible && catClosed.Visible, "cat blink swaps only its matching head layers");
            player.Seek(2.7, true);
            check(catHead.Visible && !catClosed.Visible, "cat blink reopens eyes");
            var pawRest = catPaw.Rotation;
            Play(player, "Attack", ShenshenVisuals.AttackDelay);
            check(!Mathf.IsEqualApprox(catPaw.Rotation, pawRest), "cat attack swings a separate front paw");
            Finish(player);
            check(player.CurrentAnimation == "Idle" && !human.Visible && cat.Visible,
                "cat attack returns to cat idle without revealing human canvas");
            ShenshenVisuals.SetForm(visuals, ShenshenForm.Human);
            check(human.Visible && !cat.Visible && player.CurrentAnimation == "Cast",
                "switching back displays only human canvas and plays transformation cast");
            Finish(player);
            check(player.CurrentAnimation == "Idle", "transformation cast returns to human idle");
        }
        finally
        {
            host.RemoveChild(visuals);
            visuals.Free();
        }
    }

    private static void CheckTriggerBridge(Action<bool, string> check)
    {
        var creature = new NCreature();
        var visuals = new NCreatureVisuals();
        typeof(NCreature).GetProperty(nameof(NCreature.Visuals))!.SetValue(creature, visuals);
        creature.AddChild(visuals);
        var player = MakePlayer(["Idle", "Attack", "Cast", "PowerUp", "Hit", "Dead", "Revive"]);
        visuals.AddChild(player);
        foreach (var trigger in new[] { "Idle", "Attack", "Cast", "PowerUp", "Hit", "Dead", "Revive" })
        {
            creature.SetAnimationTrigger(trigger);
            check(player.CurrentAnimation == trigger, "BaseLib non-Spine bridge forwards " + trigger);
        }
        creature.Free();

        creature = new NCreature();
        visuals = new NCreatureVisuals();
        typeof(NCreature).GetProperty(nameof(NCreature.Visuals))!.SetValue(creature, visuals);
        creature.AddChild(visuals);
        player = MakePlayer(["attack", "hurt", "die"]);
        visuals.AddChild(player);
        foreach (var (trigger, clip) in new[] { ("Hit", "hurt"), ("Dead", "die"), ("Attack", "attack") })
        {
            creature.SetAnimationTrigger(trigger);
            check(player.CurrentAnimation == clip, "BaseLib bridge resolves " + trigger + " fallback " + clip);
        }
        creature.SetAnimationTrigger("Missing");
        check(player.CurrentAnimation == "attack" && CustomAnimation.PlayCustomAnimation(creature, "Missing"),
            "missing clip leaves playback unchanged; dispatch bool only indicates handler existence");
        creature.Free();
    }

    private static AnimationPlayer MakePlayer(string[] names)
    {
        var player = new AnimationPlayer();
        var library = new AnimationLibrary();
        foreach (var name in names) library.AddAnimation(name, new Animation { Length = 1 });
        player.AddAnimationLibrary("", library);
        return player;
    }

    private static void Play(AnimationPlayer player, string clip, float seconds)
    {
        player.Play(clip);
        player.Advance(seconds);
    }

    private static void Finish(AnimationPlayer player)
    {
        player.Advance(2);
        player.Advance(0);
    }
}
