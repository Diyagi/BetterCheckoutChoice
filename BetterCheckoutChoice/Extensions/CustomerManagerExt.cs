using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BetterCheckoutChoice;

public static class CustomerManagerExt
{
    private static FieldInfo m_ActiveCustomers = AccessTools.DeclaredField(typeof(CustomerManager), "m_ActiveCustomers");
    
    public static List<Customer> GetActiveCustomersList(this CustomerManager customerManager)
    {
        return (List<Customer>)m_ActiveCustomers.GetValue(customerManager);
    }
}