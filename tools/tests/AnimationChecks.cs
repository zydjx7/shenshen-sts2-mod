using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Models;
using Shenshen.ShenshenCode.Character;
using Shenshen.ShenshenCode.Visuals;
using Hero = Shenshen.ShenshenCode.Character.Shenshen;

// Inspect actual AnimationPlayer output in the isolated MegaDot harness, not pose formulas.
internal static class AnimationChecks
{
    internal static void Run(Node host, Action<bool, string> check)
    {
        CheckTriggerBridge(check);
        var character = ModelDb.Character<Hero>();
        var restored = character.CreateVisuals();
        ShenshenVisuals.SetForm(restored, ShenshenForm.Cat, animate: false);
        host.AddChild(restored);
        check(!restored.GetNode<Node2D>("Visuals/HumanRig/Canvas").Visible &&
            restored.GetNode<Node2D>("Visuals/HumanRig/CatCanvas").Visible &&
            restored.GetNode<AnimationPlayer>("Visuals/AnimationPlayer").CurrentAnimation == "Idle",
            "tracked cat restored before tree insertion survives Ready idle initialization");
        host.RemoveChild(restored);
        restored.Free();

        var visuals = character.CreateVisuals();
        host.AddChild(visuals);
        try
        {
            var player = visuals.GetNode<AnimationPlayer>("Visuals/AnimationPlayer");
            player.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
            var rig = visuals.GetNode<Node2D>("Visuals/HumanRig");
            var human = rig.GetNode<Node2D>("Canvas");
            var cat = rig.GetNode<Node2D>("CatCanvas");
            var girlMesh = human.GetNode<Polygon2D>("Mesh");
            var catMesh = cat.GetNode<Polygon2D>("Mesh");
            var staff = visuals.GetNode<Marker2D>(ShenshenVisuals.StaffTipPath);
            var spell = staff.GetNode<Node2D>("Spell");
            var claws = catMesh.GetNode<Node2D>("ClawSwipe");
            var girlRest = girlMesh.UV;
            var catRest = catMesh.UV;
            check(CanvasItems(visuals).All(item => item.ZIndex == 0 && item.ZAsRelative),
                "all character layers inherit combat Z without escaping behind the room or above pause UI");
            check(!visuals.HasSpineAnimation && visuals.Body.Name == "Visuals" && visuals.Bounds.Size.Y > 0,
                "continuous mesh visuals expose the native required nodes without Spine");
            check(player.CurrentAnimation == "Idle" && human.Visible && !cat.Visible,
                "continuous mesh visuals start human idle automatically");
            check(human.GetNode<Node2D>("SolarRing").GetIndex() < girlMesh.GetIndex(),
                "shared solar ring draws behind the character within the same local render layer");
            Sample(player, "Idle", 0);
            check(SamePoints(girlMesh.Polygon, girlRest) && SamePoints(catMesh.Polygon, catRest),
                "both resting meshes retain original image coordinates with Polygon equal to UV");
            var staffRest = staff.Position;
            Sample(player, "Idle", 0.9);
            check(MaxTravel(girlRest, girlMesh.Polygon) > 1 && MaxTravel(catRest, catMesh.Polygon) > 1,
                "both idle clips deform painted character vertices rather than only an overlay");

            Sample(player, "Cast", ShenshenVisuals.CastDelay / 2);
            var midpoint = girlMesh.Polygon;
            var staffMidpoint = staff.Position;
            Sample(player, "Cast", ShenshenVisuals.CastDelay);
            var castPeak = girlMesh.Polygon;
            var moving = Enumerable.Range(0, girlRest.Length)
                .Where(i => girlRest[i].DistanceTo(castPeak[i]) > 2).ToArray();
            check(moving.Length > 0 && moving.All(i =>
                    midpoint[i].DistanceTo(girlRest[i].Lerp(castPeak[i], 0.5f)) < 0.05f &&
                    midpoint[i].DistanceTo(girlRest[i]) > 0.5f &&
                    midpoint[i].DistanceTo(castPeak[i]) > 0.5f),
                "Godot interpolates packed Vector2 arrays continuously halfway through cast");
            check(staff.Position.DistanceTo(staffRest) > 2 &&
                staffMidpoint.DistanceTo(staffRest.Lerp(staff.Position, 0.5f)) < 0.05f &&
                staff.Position.DistanceTo(MapPoint(girlMesh, staffRest)) < 1,
                "StaffTip follows the same deformed wand geometry and interpolates between cast keys");
            check(spell.Modulate.A > 0.9f, "cast visibly reveals the spell at the moving staff tip");
            var spellScale = spell.Scale;
            Sample(player, "Cast", 0.5);
            check(spell.Scale.DistanceTo(spellScale) > 0.01f,
                "cast spell expands during playback instead of remaining a fixed overlay");

            foreach (var clip in new[] { "Attack", "Cast", "PowerUp", "Hit" })
            {
                Play(player, clip, clip == "Hit" ? 0.08f : ShenshenVisuals.AttackDelay);
                check(MaxTravel(girlRest, girlMesh.Polygon) > 1, clip + " visibly deforms the human mesh");
                Finish(player);
                check(player.CurrentAnimation == "Idle" && human.Visible && !cat.Visible &&
                    SamePoints(girlMesh.Polygon, girlRest) && spell.Modulate.A < 0.01f,
                    clip + " returns to the original human idle pose and clears spell effects");
            }
            Play(player, "Cast", ShenshenVisuals.CastDelay);
            Play(player, "Hit", 0.08f);
            check(spell.Modulate.A < 0.01f, "hit interruption clears the active cast effect immediately");
            Play(player, "Attack", ShenshenVisuals.AttackDelay);
            Sample(player, "Idle", 0);
            check(SamePoints(girlMesh.Polygon, girlRest) && rig.Position.IsZeroApprox() &&
                Mathf.IsZeroApprox(rig.Rotation) && rig.Modulate.IsEqualApprox(Colors.White),
                "interrupted cast, hit and attack reset all mesh, tint and root transforms");
            CheckDeathAndRevive(player, rig, girlMesh, check, "human");

            var humanCenter = visuals.VfxSpawnPosition.Position;
            ShenshenVisuals.SetForm(visuals, ShenshenForm.Cat, animate: false);
            check(!human.Visible && cat.Visible && player.CurrentAnimation == "Idle",
                "tracked cat restoration displays exactly one canvas without starting a new action");
            check(visuals.VfxSpawnPosition.Position.Y > humanCenter.Y,
                "cat hit effects use its lower actual body center");

            // Landmark positions are read from the approved kitten artwork, not deformation weights.
            var nearPaw = Nearest(catRest, new Vector2(770, 925));
            var supports = new[] { new Vector2(440, 920), new Vector2(610, 915), new Vector2(940, 915) }
                .Select(point => Nearest(catRest, point)).ToArray();
            var supportRest = supports.Select(i => catMesh.ToGlobal(catRest[i])).ToArray();
            Play(player, "Attack", ShenshenVisuals.AttackDelay);
            var pawMove = catMesh.ToGlobal(catMesh.Polygon[nearPaw]) - catMesh.ToGlobal(catRest[nearPaw]);
            check(pawMove.X > 3 && pawMove.Y < -3 && claws.Modulate.A > 0.9f,
                "cat attack raises and advances the near front paw with visible claw motion");
            check(supports.Select((index, i) =>
                    catMesh.ToGlobal(catMesh.Polygon[index]).DistanceTo(supportRest[i])).All(distance => distance < 1),
                "cat attack leaves the other three support paws within one screen pixel of rest");
            Finish(player);
            check(player.CurrentAnimation == "Idle" && cat.Visible && !human.Visible &&
                SamePoints(catMesh.Polygon, catRest) && claws.Modulate.A < 0.01f,
                "cat attack returns to its original idle silhouette and clears claw effects");
            Play(player, "Hit", 0.08f);
            var hitPose = catMesh.Polygon;
            var bodyPoint = Nearest(catRest, new Vector2(700, 650));
            var headPoint = Nearest(catRest, new Vector2(1000, 450));
            check(hitPose[bodyPoint].DistanceTo(catRest[bodyPoint]) > 1 &&
                hitPose[headPoint].DistanceTo(catRest[headPoint]) > 1 && rig.Modulate != Colors.White,
                "cat hit deforms its body and head with a visible damage tint");
            Finish(player);
            check(player.CurrentAnimation == "Idle" && SamePoints(catMesh.Polygon, catRest) &&
                rig.Modulate.IsEqualApprox(Colors.White) && cat.Visible && !human.Visible,
                "cat hit clears deformation and damage tint while retaining only cat form");
            CheckDeathAndRevive(player, rig, catMesh, check, "cat");
            CheckMeshSamples(player, girlMesh, catMesh, supports, check);

            // Switching during an attack must replace the active clip and its transient effects.
            Play(player, "Attack", ShenshenVisuals.AttackDelay);
            ShenshenVisuals.SetForm(visuals, ShenshenForm.Human);
            player.Advance(0);
            check(human.Visible && !cat.Visible && player.CurrentAnimation == "Cast" && claws.Modulate.A < 0.01f,
                "switching during a cat attack shows only human and clears the interrupted claw effect");
            Finish(player);
            check(player.CurrentAnimation == "Idle" && SamePoints(girlMesh.Polygon, girlRest),
                "transformation cast returns to the approved human idle silhouette");
        }
        finally
        {
            host.RemoveChild(visuals);
            visuals.Free();
        }
    }

    private static void CheckMeshSamples(AnimationPlayer player, Polygon2D girl, Polygon2D cat,
        int[] supportPaws, Action<bool, string> check)
    {
        var girlRest = girl.UV;
        var catRest = cat.UV;
        var girlTriangles = girl.Polygons.Select(value => value.AsInt32Array()).ToArray();
        var catTriangles = cat.Polygons.Select(value => value.AsInt32Array()).ToArray();
        // The accepted B5-3 image's ankle and toe region. No mesh-generation weights are reused.
        var ankleArea = new Rect2(360, 1340, 350, 125);
        var feet = Enumerable.Range(0, girlRest.Length).Where(i => ankleArea.HasPoint(girlRest[i])).ToArray();
        check(feet.Length > 0 && girlTriangles.Length > 0 && catTriangles.Length > 0,
            "mesh validation has ankle vertices and explicit triangles for both forms");
        Sample(player, "Idle", 0);
        var footWorld = feet.Select(i => girl.ToGlobal(girlRest[i])).ToArray();
        var pawWorld = supportPaws.Select(i => cat.ToGlobal(catRest[i])).ToArray();
        foreach (var name in new[] { "Idle", "Relaxed", "Attack", "Cast", "PowerUp", "Hit", "Revive" })
        {
            var animation = player.GetAnimation(name);
            var track = animation.FindTrack("HumanRig/Canvas/Mesh:polygon", Animation.TrackType.Value);
            var times = Enumerable.Range(0, 25).Select(i => animation.Length * i / 25.0)
                .Concat(Enumerable.Range(0, animation.TrackGetKeyCount(track))
                    .Select(i => animation.TrackGetKeyTime(track, i))).Distinct().Order().ToArray();
            var validGirl = true;
            var validCat = true;
            var fixedFeet = true;
            var fixedPaws = true;
            var fixedUv = true;
            foreach (var time in times)
            {
                Sample(player, name, time);
                var girlPose = girl.Polygon;
                var catPose = cat.Polygon;
                validGirl &= NoFlippedTriangles(girlRest, girlPose, girlTriangles);
                validCat &= NoFlippedTriangles(catRest, catPose, catTriangles);
                fixedFeet &= feet.Select((index, i) => girl.ToGlobal(girlPose[index]).DistanceTo(footWorld[i]))
                    .All(distance => distance < 0.02f);
                fixedPaws &= supportPaws.Select((index, i) => cat.ToGlobal(catPose[index]).DistanceTo(pawWorld[i]))
                    .All(distance => distance < 1);
                fixedUv &= SamePoints(girl.UV, girlRest) && SamePoints(cat.UV, catRest);
            }
            check(validGirl && validCat, name + " has no degenerate or flipped mesh triangles at keys and intermediate times");
            check(fixedFeet, name + " keeps both human ankles and feet planted in world coordinates");
            check(fixedPaws, name + " keeps the kitten's three support paws within one screen pixel");
            check(fixedUv, name + " preserves both original texture UV arrays throughout playback");
        }
        Sample(player, "Idle", 0);
    }

    private static void CheckDeathAndRevive(AnimationPlayer player, Node2D rig, Polygon2D mesh,
        Action<bool, string> check, string form)
    {
        Play(player, "Dead", ShenshenVisuals.DeathDuration + 0.1f);
        var finalPosition = rig.Position;
        var finalRotation = rig.Rotation;
        player.Advance(0.5);
        check(player.CurrentAnimation != "Idle" && Math.Abs(finalRotation) > 0.1 &&
            rig.Position.IsEqualApprox(finalPosition) && Mathf.IsEqualApprox(rig.Rotation, finalRotation),
            form + " death holds its final collapsed pose");
        Play(player, "Revive", 0.6f);
        player.Advance(0);
        check(player.CurrentAnimation == "Idle" && rig.Position.IsZeroApprox() &&
            Mathf.IsZeroApprox(rig.Rotation) && SamePoints(mesh.Polygon, mesh.UV),
            form + " revive resets root and mesh before returning to idle");
    }

    private static bool NoFlippedTriangles(Vector2[] rest, Vector2[] pose, int[][] triangles)
    {
        if (pose.Length != rest.Length || pose.Any(point => !point.IsFinite())) return false;
        foreach (var triangle in triangles)
        {
            if (triangle.Length != 3 || triangle.Any(index => index < 0 || index >= pose.Length)) return false;
            var original = (rest[triangle[1]] - rest[triangle[0]]).Cross(rest[triangle[2]] - rest[triangle[0]]);
            var actual = (pose[triangle[1]] - pose[triangle[0]]).Cross(pose[triangle[2]] - pose[triangle[0]]);
            if (Math.Abs(actual) < 0.001f || original * actual <= 0) return false;
        }
        return true;
    }

    private static Vector2 MapPoint(Polygon2D mesh, Vector2 point)
    {
        var rest = mesh.UV;
        var pose = mesh.Polygon;
        foreach (var entry in mesh.Polygons)
        {
            var indices = entry.AsInt32Array();
            var a = rest[indices[0]];
            var b = rest[indices[1]] - a;
            var c = rest[indices[2]] - a;
            var determinant = b.Cross(c);
            if (Math.Abs(determinant) < 0.001f) continue;
            var u = (point - a).Cross(c) / determinant;
            var v = b.Cross(point - a) / determinant;
            if (u >= -0.0001f && v >= -0.0001f && u + v <= 1.0001f)
                return pose[indices[0]] * (1 - u - v) + pose[indices[1]] * u + pose[indices[2]] * v;
        }
        throw new InvalidOperationException("Staff marker is outside the actual character mesh");
    }

    private static int Nearest(Vector2[] points, Vector2 target) =>
        Enumerable.Range(0, points.Length).MinBy(i => points[i].DistanceSquaredTo(target));

    private static float MaxTravel(Vector2[] rest, Vector2[] pose) =>
        rest.Select((point, i) => point.DistanceTo(pose[i])).Max();

    private static bool SamePoints(Vector2[] first, Vector2[] second) =>
        first.Length == second.Length && first.Zip(second).All(pair => pair.First.DistanceTo(pair.Second) < 0.01f);

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
        player.Seek(0, true);
        player.Advance(seconds);
    }

    private static void Sample(AnimationPlayer player, string clip, double seconds)
    {
        player.Play(clip);
        player.Seek(seconds, true);
        player.Advance(0);
    }

    private static void Finish(AnimationPlayer player)
    {
        player.Advance(2);
        player.Advance(0);
    }

    private static IEnumerable<CanvasItem> CanvasItems(Node node)
    {
        if (node is CanvasItem item) yield return item;
        foreach (var child in node.GetChildren())
            foreach (var descendant in CanvasItems(child)) yield return descendant;
    }
}
