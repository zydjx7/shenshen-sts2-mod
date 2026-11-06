using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Shenshen.ShenshenCode.Cards;

namespace Shenshen.ShenshenCode.Mechanics;

/// <summary>Only presentation changes; the original card and its saved state remain intact.</summary>
public static class TransformationCardFace
{
    public static async Task Refresh(Player player, bool animate = true)
    {
        if (player.PlayerCombatState == null) return;
        var cards = player.PlayerCombatState.AllCards.OfType<TransformationMagic>().ToArray();
        await Task.WhenAll(cards.Select(card => RefreshCard(card, animate)));
    }

    private static async Task RefreshCard(CardModel card, bool animate)
    {
        var node = NCard.FindOnTable(card);
        if (node == null || !node.IsInsideTree() || !node.IsNodeReady()) return;
        if (!animate)
        {
            node.UpdateVisuals(node.DisplayingPile, CardPreviewMode.Normal);
            return;
        }

        // Animate the card's visual body, leaving native hand-holder layout and
        // card-play position/scale tweens in control of the outer NCard.
        var body = node.Body;
        var scale = body.Scale;
        var pivot = body.PivotOffset;
        body.PivotOffset = body.Size / 2f;
        var completion = new TaskCompletionSource();
        void Complete() => completion.TrySetResult();
        node.TreeExiting += Complete;
        var tween = body.CreateTween();
        tween.TweenProperty(body, "scale:x", 0.03f, 0.08f).SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(node) && node.Model == card)
                node.UpdateVisuals(node.DisplayingPile, CardPreviewMode.Normal);
        }));
        tween.TweenProperty(body, "scale:x", scale.X, 0.08f).SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(Complete));
        try
        {
            // Pooling may cancel a bound tween without TreeExiting. This visual
            // flourish must never hold the card-play action indefinitely.
            await Task.WhenAny(completion.Task, Task.Delay(500));
        }
        finally
        {
            if (GodotObject.IsInstanceValid(tween)) tween.Kill();
            if (GodotObject.IsInstanceValid(node)) node.TreeExiting -= Complete;
            if (GodotObject.IsInstanceValid(node) && GodotObject.IsInstanceValid(body) && node.Model == card)
            {
                body.Scale = scale;
                body.PivotOffset = pivot;
                if (node.IsInsideTree()) node.UpdateVisuals(node.DisplayingPile, CardPreviewMode.Normal);
            }
        }
    }
}
