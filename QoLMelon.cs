using MelonLoader;

[assembly: MelonInfo(typeof(FellAndSellQoL.QoLMod), "Fell & Sell QoL", "1.0.0", "Touriste")]
[assembly: MelonGame("Art Games Studio SA", "Fell & Sell")]

namespace FellAndSellQoL
{
    public class QoLMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Fell & Sell QoL loaded!");
        }
    }
}