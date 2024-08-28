using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace BetterCheckoutChoice;

public static class CustomerManagerExt
{
    private static FieldInfo m_ActiveCustomers = AccessTools.DeclaredField(typeof(CustomerManager), "m_ActiveCustomers");
    
    public static List<Customer> GetActiveCustomersList(this CustomerManager customerManager)
    {
        return (List<Customer>)m_ActiveCustomers.GetValue(customerManager);
    }
    
    // [HarmonyReversePatch]
    // [MethodImpl(MethodImplOptions.NoInlining)]
    // [HarmonyPatch(typeof(CustomerManager), "StartCoroutine")]
    // public static void StartCoroutine(this Customer instance, IEnumerator routine) =>
    //     // its a stub so it has no initial content
    //     throw new NotImplementedException("It's a stub");
}