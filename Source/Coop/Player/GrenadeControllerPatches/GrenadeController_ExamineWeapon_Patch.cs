﻿using SIT.Core.Coop.NetworkPacket;
using SIT.Tarkov.Core;
using StayInTarkov;
using StayInTarkov.Networking;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SIT.Core.Coop.Player.GrenadeControllerPatches
{
    internal class GrenadeController_ExamineWeapon_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.GrenadeController);
        public override string MethodName => "GrenadeController_ExamineWeapon";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, "ExamineWeapon");
        }

        public static List<string> CallLocally = new();

        [PatchPrefix]
        public static bool PrePatch(object __instance, EFT.Player ____player)
        {
            if (CallLocally.Contains(____player.ProfileId))
                return true;

            return false;
        }

        [PatchPostfix]
        public static void PostPatch(object __instance, EFT.Player ____player)
        {
            if (CallLocally.Contains(____player.ProfileId))
            {
                CallLocally.Remove(____player.ProfileId);
                return;
            }

            AkiBackendCommunication.Instance.SendDataToPool(new BasePlayerPacket(____player.ProfileId, "GrenadeController_ExamineWeapon").Serialize());
        }

        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {
            Logger.LogInfo("GrenadeController_ExamineWeapon_Patch:Replicated");

            if (!dict.ContainsKey("data"))
                return;

            BasePlayerPacket examineWeaponPacket = new();
            examineWeaponPacket = examineWeaponPacket.DeserializePacketSIT(dict["data"].ToString());

            if (HasProcessed(GetType(), player, examineWeaponPacket))
                return;

            if (player.HandsController is EFT.Player.GrenadeController grenadeController)
            {
                CallLocally.Add(player.ProfileId);
                grenadeController.ExamineWeapon();
            }
        }
    }
}