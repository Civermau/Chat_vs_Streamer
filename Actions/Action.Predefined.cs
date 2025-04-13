using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Timers;
using HarmonyLib;
using UnityEngine;

public partial class Action
{
    Harmony harmony = new Harmony("dev.civermau.twitchapimod");
	System.Timers.Timer timer;

    // Taken from https://github.com/FranzFischer78/FishMod
	// Really liked the concept, though would be cool if the viewers could turn the streamer into a fish.
    public void TurnPlayerIntoAFish()
    {
		var original = typeof(Stat_Oxygen).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
		var transpiler = typeof(OxygenUnderwaterPatch).GetMethod("Transpiler");
		harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));

		StartTimer();
    }

	private void StartTimer()
	{
		timer = new System.Timers.Timer(30000);
		timer.Elapsed += OnTimeout;
		timer.Start();
	}

	private void OnTimeout(object sender, ElapsedEventArgs e)
	{
		TurnPlayerBackToHuman();
	}
	
    public void TurnPlayerBackToHuman()
    {
        var original = typeof(Stat_Oxygen).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
        harmony.Unpatch(original, HarmonyPatchType.Transpiler);
    }

	public void GiveRandomItem()
	{
		var items = System.Enum.GetValues(typeof(ItemType));
		var randomItem = (ItemType)items.GetValue(new System.Random().Next(items.Length));
		RAPI.GetLocalPlayer().Inventory.AddItem(randomItem.ToString(), 1);
		Debug.Log($"Added {randomItem.ToString()} to inventory");
	}

	public void RemoveRandomItemFromInventory()
	{
		var items = RAPI.GetLocalPlayer().Inventory.allSlots.FindAll(slot => slot.HasValidItemInstance()).Select(slot => slot.itemInstance);
		var randomItem = items.ElementAt(new System.Random().Next(items.Count()));
		RAPI.GetLocalPlayer().Inventory.RemoveItem(randomItem.UniqueName, RAPI.GetLocalPlayer().Inventory.GetItemCount(randomItem.UniqueName));
		Debug.Log($"Removed {randomItem.UniqueName} from inventory");
	}

	public void DehydratePlayer()
	{
		var player = RAPI.GetLocalPlayer();
		player.Stats.stat_thirst.Normal.Value -= 100;
		Debug.Log("Dehydrated player");
	}

	public void HydratePlayer()
	{
		var player = RAPI.GetLocalPlayer();
		player.Stats.stat_thirst.Normal.Value += 100;
		Debug.Log("Hydrated player");
	}

	public void StarvePlayer()
	{
		var player = RAPI.GetLocalPlayer();
		player.Stats.stat_hunger.Normal.Value -= 100;
		Debug.Log("Starved player");
	}

	public void FeedPlayer()
	{
		var player = RAPI.GetLocalPlayer();
		player.Stats.stat_hunger.Normal.Value += 100;
		Debug.Log("Fed player");
	}

	public void WipeInventory()
	{
		RAPI.GetLocalPlayer().Inventory.Clear();
		Debug.Log("Wiped inventory");
	}
}

public static class OxygenUnderwaterPatch
{
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = new List<CodeInstruction>(instructions);
		for (int i = 0; i < codes.Count; i++)
		{
			if (codes[i].opcode == OpCodes.Ldc_I4_2)
			{
				codes[i].opcode = OpCodes.Ldc_I4_0;
				codes.RemoveRange(i + 2, 4);
				break;
			}
		}
		return codes;
	}
}