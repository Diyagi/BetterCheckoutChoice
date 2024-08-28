using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MyBox;
using UnityEngine;

namespace BetterCheckoutChoice;

public class Patches
{
    [HarmonyPatch(typeof(CheckoutManager))]
    [HarmonyPatch("GetAvailableCheckout", MethodType.Getter)]
    public class CheckManGetAvailableCheckout()
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Checkout __result)
        {
            CheckoutManager.Instance.SetTempCheckouts(
                    CheckoutManager.Instance.GetCheckoutList().Where(i => 
                        !i.Full && 
                        i.CurrentState != Checkout.State.PLACING && 
                        i.CurrentState != Checkout.State.CLOSED)
                .ToList());
            
            if (CheckoutManager.Instance.GetTempCheckoutList().Count <= 0)
            {
                __result = null;
                return false;
            }

            // Try to return an checkout with the least amount of customers and
            // with automated checkout or the one the player is interacting with
            Checkout chosenCheckout = CheckoutManager.Instance.GetTempCheckoutList()
                .OrderBy(x => x.GetCustomerList().Count)
                .FirstOrDefault(checkout => checkout.HasCashier || checkout.InInteraction || checkout.IsSelfCheckout);

            if (chosenCheckout != null)
            {
                __result = chosenCheckout;
                return false;
            }
            
            // If we get to this point, it means theres no checkout that is
            // automated or the player is interacting with, so we just
            // return whatever checkout there is
            __result = CheckoutManager.Instance.GetTempCheckoutList()[0];
            return false;
        }
    }

    [HarmonyPatch(typeof(Checkout))]
    [HarmonyPatch("AskForCustomer")]
    public class CheckAskForCustomer()
    {
        [HarmonyPostfix]
        public static void Postfix(Checkout __instance)
        {
            if (__instance.GetCustomerList().Count > 0) return;
            if (CheckoutManager.Instance.m_CustomersAwaiting.Count > 0) return;
            if (!(__instance.HasCashier || __instance.IsSelfCheckout)) return;

            __instance.RebalanceQueues();
        }
    }
    
    [HarmonyPatch(typeof(Checkout))]
    [HarmonyPatch("InstantInteract")]
    public class CheckoutInteract()
    {
        [HarmonyPostfix]
        public static void Postfix(Checkout __instance, bool __result)
        {
            if (__result)
            {
                __instance.RebalanceQueues();
            }
        }
    }
    
    [HarmonyPatch(typeof(CustomerManager))]
    [HarmonyPatch("OnEnable")]
    public class CmOnEnable()
    {
        [HarmonyPostfix]
        public static void Postfix(CustomerManager __instance)
        {
            __instance.StartCoroutine(FixStrandedCustomers(__instance));
        }
    }

    
    private static readonly MethodInfo OnCheckoutMovedFi = AccessTools.Method(typeof(Customer), "OnCheckoutMoved");
    private static readonly MethodInfo OnCheckoutBoxedFi = AccessTools.Method(typeof(Customer), "OnCheckoutBoxed");
    // ReSharper disable Unity.PerformanceAnalysis
    private static IEnumerator FixStrandedCustomers(CustomerManager customerManager)
    {
        for (;;)
        {
            customerManager.GetActiveCustomersList().ForEach(i =>
            {
                Checkout customerCheckout = i.GetCheckout();
                if (!customerCheckout) return;
                if (customerCheckout.GetCustomerList().Contains(i)) return;
            
                BetterMod.Logger.Warning("Stranded customer found, trying to force checkout search!");
                try
                {
                    // Access the method via reflection and create an action from it,
                    // it was the only way i found to properly remove the action from the delegate
                    Action<Checkout> onCheckoutBoxedAction = (Action<Checkout>)Delegate.CreateDelegate(
                        typeof(Action<Checkout>),
                        i,
                        OnCheckoutBoxedFi);
                    Action<Checkout> onCheckoutMovedAction = (Action<Checkout>)Delegate.CreateDelegate(
                        typeof(Action<Checkout>),
                        i,
                        OnCheckoutMovedFi);

                    CheckoutInteraction instance = CheckoutInteraction.Instance;
                    instance.onCheckoutBoxed =
                        (Action<Checkout>)Delegate.Remove(instance.onCheckoutBoxed, onCheckoutBoxedAction);
                    CheckoutInteraction instance2 = CheckoutInteraction.Instance;
                    instance2.onCheckoutClosed =
                        (Action<Checkout>)Delegate.Remove(instance2.onCheckoutClosed, onCheckoutMovedAction);
                    
                    i.GoToCheckout();
                    
                    customerCheckout.Unsubscribe(i);
                }
                catch (Exception e)
                {
                    BetterMod.Logger.Error(e);
                    throw;
                }
            });
            yield return new WaitForSeconds(5);
        }
#pragma warning disable CS0162 // Unreachable code detected
        yield break;
#pragma warning restore CS0162 // Unreachable code detected
    }
    
    /// <summary>
    /// Patches the FinishShopping method to set the customer checkout to null
    /// </summary>
    [HarmonyPatch(typeof(Customer))]
    [HarmonyPatch("FinishShopping")]
    public class CustomerFinishShopping()
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = new List<CodeInstruction>(instructions);
            
            var instructionsToInsert = new List<CodeInstruction>();
            
            instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
            instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldnull));
            instructionsToInsert.Add(new CodeInstruction(OpCodes.Stfld, AccessTools.DeclaredField(typeof(Customer), "m_Checkout")));
            
            code.InsertRange(0, instructionsToInsert);
            
            return code;
        }
    }
    
    #if DEBUG
    [HarmonyPatch(typeof(CustomerManager))]
    [HarmonyPatch("CanSpawnCustomer", MethodType.Setter)]
    public class OverrideCustomerSpawn()
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }
    #endif
}
