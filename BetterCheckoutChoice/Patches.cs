using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
#pragma warning disable CS0162 // Unreachable code detected

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

            // Filters checkouts to ones with cashiers or self checkouts
            var checkoutList = CheckoutManager.Instance.GetTempCheckoutList()
                .Where(checkout => checkout.HasCashier || checkout.InInteraction || checkout.IsSelfCheckout)
                .ToList();

            var groupedCheckouts = checkoutList
                .GroupBy(x => x.GetCustomerList().Count) // Group by customer count
                .OrderBy(group => group.Key) // Order by the customer count
                .FirstOrDefault(); // Get the group with the smallest count

            if (groupedCheckouts != null && groupedCheckouts.Any())
            {
                // Randomly select one checkout if there are multiple with the same count
                var randomCheckout = groupedCheckouts.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                
                __result = randomCheckout;
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
                    customerCheckout.UnsubscribeAndRemoveDelegate(i);
                    i.GoToCheckout();   
                }
                catch (Exception e)
                {
                    BetterMod.Logger.Error(e);
                    throw;
                }
            });
            yield return new WaitForSeconds(5);
        }
        // ReSharper disable once HeuristicUnreachableCode
        yield break;
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
}
