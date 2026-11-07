using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Shenshen.ShenshenCode.Visuals;

/// <summary>Uses the same human mesh in the shop's native player-character slot.</summary>
internal static class ShenshenMerchantVisuals
{
    public const string ScenePath = MainFile.ResPath + "/scenes/merchant/shenshen_merchant.tscn";

    public static void Register()
    {
        // BaseLib 3.4.7 stores post-conversion delegates as Action<Node>.
        NodeFactory.RegisterSceneType<Node>(ScenePath, (typeof(NMerchantCharacter), Populate));
    }

    private static void Populate(Node character)
    {
        var visuals = ShenshenVisuals.Create();
        visuals.Name = "ShenshenVisuals";
        character.AddChild(visuals);
    }
}
