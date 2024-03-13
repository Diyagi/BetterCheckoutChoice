using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BetterCheckoutChoice;

public static class CheckoutExtensions
{
    private static FieldInfo m_Customers = AccessTools.DeclaredField(typeof(Checkout), "m_Customers");
    
    public static List<Customer> GetCustomerList(this Checkout checkout)
    {
        return (List<Customer>)m_Customers.GetValue(checkout);
    }
}