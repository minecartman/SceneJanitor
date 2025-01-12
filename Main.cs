using BoneLib.BoneMenu;
using BoneLib.Notifications;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using LabFusion;
using LabFusion.Network;
using Il2CppJetBrains.Annotations;
using System.Runtime.CompilerServices;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Zones;
using Il2CppSLZ.Marrow.AI;
using Il2CppSLZ.Marrow.VoidLogic;
using System.Runtime.InteropServices;

namespace scene_janitor // if this code looks similar to other code mods then it probably is because i had to look at other peoples open source code just to know how to attempt this (i dont know if thats discouraged or not so if it is i apologize)
{
	internal partial class Main : MelonMod
	{
		public override void OnInitializeMelon()
		{
			base.OnInitializeMelon();

			MelonLogger.Msg("Scene Janitor Loaded");

			var ModBonePage = Page.Root.CreatePage("Scene Janitor", new Color(0.486f, 0.871f, 1f));

			var FusionSyncing = ModBonePage.CreateBool("Use Fusion Methods", new Color(1f, 1f, 1f), VarSaves.FusionSyncBool, (value) => VarSaves.FusionSyncBool = value);
			ModBonePage.CreateFunction("NOTE: Non Fusion Methods are currently extremely buggy.", new Color(1f,1f,0.5f), null);

			// Prop Cleanup

			var PropCleanupPage = ModBonePage.CreatePage("Crate Management", new Color(0.8f,0.6f,1f));

			var DespawnButton = PropCleanupPage.CreateFunction("Despawn All Props", new Color(1f, 0.5f, 0.5f), DespawnProps);
			var RestoreButton = PropCleanupPage.CreateFunction("Clear & Restore Old Props", new Color(0.5f, 1f, 0.5f), RestoreCleanup);
			var SpawnButton = PropCleanupPage.CreateFunction("Spawn All Props", new Color(1f, 1f, 0.5f), PropSpawn);

			// NPC Cleanup

			var NPCCleanupPage = ModBonePage.CreatePage("NPC Cleanup", new Color(0.6f,1f,0.6f));

			var AllNpcClear = NPCCleanupPage.CreateFunction("Clear all NPCs", new Color(1f, 1f, 1f), AllNPCClear);
			var FordClear = NPCCleanupPage.CreateFunction("Clear Fords", new Color(0.6f, 1f, 0.6f), FordClearVoid);
			var NullClear = NPCCleanupPage.CreateFunction("Clear Nullbodies", new Color(1f, 0.8f, 0.6f), NullClearVoid);
			var CrabClear = NPCCleanupPage.CreateFunction("Clear Crablets", new Color(1f, 0.6f, 0.6f), CrabClearVoid);
			var OmniClear = NPCCleanupPage.CreateFunction("Clear Omnis", new Color(1f, 1f, 0.8f), OmniClearVoid);
			var SkelClear = NPCCleanupPage.CreateFunction("Clear Skeletons", new Color(0.9f, 0.9f, 0.9f), SkeleClearVoid);
			var PeasantClear = NPCCleanupPage.CreateFunction("Clear Peasants", new Color(0.8f, 0.7f, 1f), PeasantCLearVoid);
			var ZombClear = NPCCleanupPage.CreateFunction("Clear Early Exits", new Color(0.5f, 0.5f, 1f), ZombieVoid);
		}

		public void NotificationVoid(string Message, NotificationType NotifType, float Length, bool ShowTitle)
		{
			var testNotif = new BoneLib.Notifications.Notification
			{
				Title = "Scene Janitor",
				Message = Message,
				Type = NotifType,
				PopupLength = Length,
				ShowTitleOnPopup = ShowTitle,
			};

			BoneLib.Notifications.Notifier.Send(testNotif);
		}

		// Prop Cleanup

		public void PropCleanup(bool Despawning, bool Restoring, string NotifText)
		{
			var didcleanup = false;

			if (VarSaves.FusionSyncBool == true)
			{
				if (!NetworkInfo.IsServer)
				{
					NotificationVoid("You are not the server host!", NotificationType.Error, 4f, true);
					return;
				}

				if (Despawning == true)
				{
					didcleanup = true;
					LabFusion.Utilities.PooleeUtilities.DespawnAll();
				}
			}

			GameObject[] CollectedGameObjects = GameObject.FindObjectsOfType<GameObject>();

			if (VarSaves.FusionSyncBool == false && Despawning == true)
			{
				GameObject[] AssetSpawnObjects = CrateSpawnSequencer.FindObjectsOfType<GameObject>();

				foreach (GameObject Object in AssetSpawnObjects)
				{
					if (Object.GetComponent<Poolee>() != null && Object.GetComponent<MarrowEntity>() != null && Object.layer != LayerMask.NameToLayer("Player") && Object.tag == "Untagged")
					{
						if (Object.GetComponentInChildren<Tracker>() != null && Object.GetComponent<MarrowBody>() != null | Object.GetComponentInChildren<MarrowBody>() != null && Object.GetComponentInChildren<InteractableHost>() != null | Object.GetComponent<InteractableHost>() != null)
						{
							didcleanup = true;
							Object.GetComponent<Poolee>().Despawn();
						}
					}
				}
			}

			foreach (GameObject Object in CollectedGameObjects)
			{
				if (Restoring == true)
				{
					if (Object.GetComponent<CrateSpawner>() != null && Object.layer == LayerMask.NameToLayer("Ignore Raycast") && Object.active == true)
					{
						if (Object.GetComponent<CrateSpawner>().manualMode == false)
						{
							didcleanup = true;
							Object.GetComponent<CrateSpawner>().SpawnSpawnable();
						}
					}
				}
			}

			if (didcleanup == true)
			{
				MelonLogger.Msg(NotifText);
				NotificationVoid(NotifText, NotificationType.Success, 1f, true);
			}
		}

		private void RestoreCleanup()
		{
			PropCleanup(true, true, "Cleared & Restored Props");
		}

		private void PropSpawn()
		{
			PropCleanup(false, true, "Spawned Props");
		}

		private void DespawnProps()
		{
			if (VarSaves.FusionSyncBool == true)
			{
				NotificationVoid("There is no reason to use this with fusion installed lol", NotificationType.Error, 5f, true);
			}
			else
			{
				PropCleanup(true, false, "Despawned Props");
			}
		}

		public void NPCCleanup(string nameLook, string NotifText, bool SpecificNPC)
		{
			var didcleanup = false;

			if (VarSaves.FusionSyncBool == true)
			{
				if (!NetworkInfo.IsServer)
				{
					NotificationVoid("You are not the server host!", NotificationType.Error, 4f, true);
					return;
				}

				
			}

			GameObject[] AssetSpawnObjects = CrateSpawnSequencer.FindObjectsOfType<GameObject>();

			foreach (GameObject Object in AssetSpawnObjects)
			{
				if (Object.GetComponent<Poolee>() != null && Object.GetComponent<MarrowEntity>() != null && Object.layer != LayerMask.NameToLayer("Player") && Object.tag == "Untagged")
				{
					if (Object.GetComponentInChildren<Tracker>() != null && Object.GetComponent<MarrowBody>() != null | Object.GetComponentInChildren<MarrowBody>() != null && Object.GetComponentInChildren<InteractableHost>() != null | Object.GetComponent<InteractableHost>() != null && Object.GetComponent<AIBrain>())
					{
						if (SpecificNPC == true)
						{
							if (Object.name.Contains(nameLook))
							{
								didcleanup = true;
								Object.GetComponent<Poolee>().Despawn();
							}
						}
						else
						{
							didcleanup = true;
							Object.GetComponent<Poolee>().Despawn();
						}
					}
				}
			}

			if (didcleanup == true && NotifText != null)
			{
				MelonLogger.Msg(NotifText);
				NotificationVoid(NotifText, NotificationType.Success, 1f, true);
			}
		}

		private void AllNPCClear()
		{
			NPCCleanup(null, "Cleared all NPCs", false);
		}

		private void FordClearVoid()
		{
			NPCCleanup("NPC_Ford_BWOrig", "Cleared all Fords", true);
		}

		private void NullClearVoid()
		{
			NPCCleanup("Null", "Cleared all Nullbodies", true);
			//NPCCleanup("NullBody_RooftopAgent", "Cleared all Nullbody Agents", true);
			//NPCCleanup("NullBodyCorrupted", "Cleared all Corrupted Nullbodies", true);
			//NPCCleanup("Peasant Null", null, true);
		}

		private void CrabClearVoid()
		{
			NPCCleanup("Crablet", "I HATE THOSE STUPID CRABS GAHHHHHHH", true);
			//NPCCleanup("CrabletPlus", "no more big crab", true);
		}

		private void OmniClearVoid()
		{
			NPCCleanup("omniProjector_hazmat", "Cleared all Omni Projectors", true);
		}

		private void SkeleClearVoid()
		{
			NPCCleanup("Skeleton", "Cleared all Skeletons", true);
			//NPCCleanup("Skeleton - Fire Mage Variant", "Cleared all Fire Mage Skeletons", true);
			//NPCCleanup("Skeleton - Exterminator Variant", "Cleared all Steel Skeletons", true);
		}

		private void PeasantCLearVoid()
		{
			NPCCleanup("NPC_Peasant", "Cleared all Peasants", true);
		}

		private void ZombieVoid()
		{
			NPCCleanup("Ford_EarlyExit", "Cleared all Early Exits", true);
		}
	}
}
