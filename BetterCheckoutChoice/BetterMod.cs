using MelonLoader;
using UnityEngine;

namespace BetterCheckoutChoice;

public class BetterMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
        
    public override void OnEarlyInitializeMelon()
    {
        Logger = LoggerInstance;
    }
    
    #if DEBUG
    public override void OnUpdate()
    {
        if(Input.GetKeyDown(KeyCode.T))
        {
            CustomerManager.Instance.SpawnCustomer();
        }
    }
    
    public override void OnInitializeMelon()
    {
        Application.runInBackground = true;
    }
    #endif
}