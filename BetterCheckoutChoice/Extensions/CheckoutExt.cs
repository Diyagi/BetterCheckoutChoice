using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MyBox;

namespace BetterCheckoutChoice;

public static class CheckoutExt
{
    private static readonly FieldInfo m_Customers = AccessTools.DeclaredField(typeof(Checkout), "m_Customers");
    private static readonly MethodInfo OnCheckoutMovedFi = AccessTools.Method(typeof(Customer), "OnCheckoutMoved");
    private static readonly MethodInfo OnCheckoutBoxedFi = AccessTools.Method(typeof(Customer), "OnCheckoutBoxed");
    
    public static List<Customer> GetCustomerList(this Checkout checkoutExt)
    {
        return (List<Customer>)m_Customers.GetValue(checkoutExt);
    }

    public static void RebalanceQueues(this Checkout checkout)
    {
        // int queueSize = CheckoutManager.Instance.GetCheckoutList().Where(i => i.GetCustomerList().Count > 2).Max(w => w.GetCustomerList().Count);
        // Checkout checkout = CheckoutManager.Instance.GetCheckoutList().First(i => i.GetCustomerList().Count == queueSize);

        List<Checkout> chkToBalance = CheckoutManager.Instance.GetCheckoutList()
            .Where(i => i.GetCustomerList().Count > 1).ToList();

        if (chkToBalance.IsNullOrEmpty()) return;

        Dictionary<Customer, Checkout> csmToTake = new();
        int csmPerCheckout = chkToBalance.Sum(i => i.GetCustomerList().Count) / (chkToBalance.Count + 1);

        chkToBalance.ForEach(checkout =>
        {
            int csmCount = checkout.GetCustomerList().Count;
            int csmToTakeCount = csmCount - csmPerCheckout;
            
            if (csmToTakeCount < 1) return;

            checkout.GetCustomerList().Skip(csmPerCheckout).ForEach(customer => csmToTake.Add(customer, checkout));
        });


        // List<Customer> customersToChange = checkout.GetCustomerList().Skip(queueSize/2).ToList();
        csmToTake.ForEach(i =>
        {
            i.Value.Unsubscribe(i.Key);

            // Access the method via reflection and create an action from it,
            // it was the only way i found to properly remove the action from the delegate
            Action<Checkout> onCheckoutBoxedAction = (Action<Checkout>)Delegate.CreateDelegate(
                typeof(Action<Checkout>),
                i.Key,
                OnCheckoutBoxedFi);
            Action<Checkout> onCheckoutMovedAction = (Action<Checkout>)Delegate.CreateDelegate(
                typeof(Action<Checkout>),
                i.Key,
                OnCheckoutMovedFi);

            CheckoutInteraction instance = CheckoutInteraction.Instance;
            instance.onCheckoutBoxed =
                (Action<Checkout>)Delegate.Remove(instance.onCheckoutBoxed, onCheckoutBoxedAction);
            CheckoutInteraction instance2 = CheckoutInteraction.Instance;
            instance2.onCheckoutClosed =
                (Action<Checkout>)Delegate.Remove(instance2.onCheckoutClosed, onCheckoutMovedAction);

            checkout.Subscribe(i.Key);
        });
    }
}