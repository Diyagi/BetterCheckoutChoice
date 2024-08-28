using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BetterCheckoutChoice;

public static class CheckoutManagerExt
{
    private static FieldInfo m_Checkouts = AccessTools.DeclaredField(typeof(CheckoutManager), "m_Checkouts");
    private static FieldInfo m_TempCheckouts = AccessTools.DeclaredField(typeof(CheckoutManager), "m_TempCheckouts");
    
    public static List<Checkout> GetCheckoutList(this CheckoutManager checkoutManager)
    {
        return (List<Checkout>)m_Checkouts.GetValue(checkoutManager);
    }
    
    public static List<Checkout> GetTempCheckoutList(this CheckoutManager checkoutManager)
    {
        return (List<Checkout>)m_TempCheckouts.GetValue(checkoutManager);
    }
    
    public static void SetTempCheckouts(this CheckoutManager checkoutManager, List<Checkout> tempCheckouts)
    {
        m_TempCheckouts.SetValue(checkoutManager, tempCheckouts);
    }
}