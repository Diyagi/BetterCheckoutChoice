using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

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

    public static void RebalanceQueues(this Checkout initiatingCheckout)
    {
        var overloadedCheckouts = CheckoutManager.Instance.GetCheckoutList()
            .Where(checkout => checkout.GetCustomerList().Count > 1)
            .ToList();

        if (!overloadedCheckouts.Any()) return;

        int totalCustomerCount = overloadedCheckouts.Sum(checkout => checkout.GetCustomerList().Count);
        int idealCustomerCountPerCheckout = totalCustomerCount / (overloadedCheckouts.Count + 1);

        var customersToReassign = overloadedCheckouts
            .SelectMany(checkout => checkout.GetCustomerList().Skip(idealCustomerCountPerCheckout)
                .Select(customer => new { Customer = customer, Checkout = checkout }))
            .ToDictionary(x => x.Customer, x => x.Checkout);

        foreach (var entry in customersToReassign)
        {
            entry.Value.UnsubscribeAndRemoveDelegate(entry.Key);
            initiatingCheckout.Subscribe(entry.Key);
        }
    }

    public static void UnsubscribeAndRemoveDelegate(this Checkout fromCheckout, Customer customer)
    {
        fromCheckout.Unsubscribe(customer);

        var onCheckoutBoxedAction = CreateDelegateAction(customer, OnCheckoutBoxedFi);
        var onCheckoutMovedAction = CreateDelegateAction(customer, OnCheckoutMovedFi);

        var checkoutInteraction = CheckoutInteraction.Instance;
        checkoutInteraction.onCheckoutBoxed = 
            (Action<Checkout>)Delegate.Remove(checkoutInteraction.onCheckoutBoxed, onCheckoutBoxedAction);
        checkoutInteraction.onCheckoutClosed = 
            (Action<Checkout>)Delegate.Remove(checkoutInteraction.onCheckoutClosed, onCheckoutMovedAction);
    }

    private static Action<Checkout> CreateDelegateAction(Customer customer, MethodInfo actionField)
    {
        return (Action<Checkout>)Delegate.CreateDelegate(typeof(Action<Checkout>), customer, actionField);
    }

}