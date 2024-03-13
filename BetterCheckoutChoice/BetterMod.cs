using MelonLoader;

namespace BetterCheckoutChoice;

public class BetterMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
        
    public override void OnEarlyInitializeMelon()
    {
        Logger = LoggerInstance;
    }
}