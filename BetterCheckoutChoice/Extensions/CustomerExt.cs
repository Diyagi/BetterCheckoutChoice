using System.Reflection;
using HarmonyLib;

namespace BetterCheckoutChoice;

[HarmonyPatch]
public static class CustomerExt
{
    private static FieldInfo m_Checkout = AccessTools.DeclaredField(typeof(Customer), "m_Checkout");
    private static MethodInfo goToCheckoutMethodInfo = AccessTools.Method(typeof(Customer), "GoToCheckout");
    
    public static Checkout GetCheckout(this Customer customer)
    {
        return (Checkout)m_Checkout.GetValue(customer);
    }

    // Needed to use reflection and invoke bc melon loader somehow fucked reverse patch
    public static void GoToCheckout(this Customer instance)
    {
        goToCheckoutMethodInfo.Invoke(instance, []);
    }
}