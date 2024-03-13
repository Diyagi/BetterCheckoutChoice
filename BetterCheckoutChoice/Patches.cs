using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace BetterCheckoutChoice;

public class Patches
{
    [HarmonyPatch(typeof(CheckoutManager), "Start")]
    public class CheckManStart()
    {
        internal static List<Checkout> m_TempCheckouts;
        internal static List<Checkout> m_Checkouts;
        
        [HarmonyPostfix]
        public static void Postfix(CheckoutManager __instance)
        {
            m_TempCheckouts = (List<Checkout>)AccessTools.DeclaredField(typeof(CheckoutManager), "m_TempCheckouts").GetValue(__instance);
            m_Checkouts = (List<Checkout>)AccessTools.DeclaredField(typeof(CheckoutManager), "m_Checkouts")
                .GetValue(__instance);
        }
    }
    
    
    [HarmonyPatch(typeof(CheckoutManager))]
    [HarmonyPatch("GetAvailableCheckout", MethodType.Getter)]
    public class CheckManGetAvailableCheckout()
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Checkout __result)
        {
            CheckManStart.m_TempCheckouts = CheckManStart.m_Checkouts.Where(i => !i.Full && i.CurrentState != Checkout.State.PLACING)
                .ToList();
            
            if (CheckManStart.m_TempCheckouts.Count <= 0)
            {
                __result = null;
                return false;
            }

            // Try to return an checkout with the least amount of customers and
            // with automated checkout or the one the player is interacting with
            Checkout chosenCheckout = CheckManStart.m_TempCheckouts
                .OrderBy(x => x.GetCustomerList().Count)
                .FirstOrDefault(checkout => checkout.HasCashier || checkout.InInteraction);

            if (chosenCheckout != null)
            {
                __result = chosenCheckout;
                return false;
            }
            
            // If we get to this point, it means theres no checkout that is
            // automated or the player is interacting with, so we just
            // return whatever checkout there is
            __result = CheckManStart.m_TempCheckouts[0];
            return false;
        }
    }
}