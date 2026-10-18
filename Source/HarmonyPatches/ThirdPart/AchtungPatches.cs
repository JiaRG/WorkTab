using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab.HarmonyPatches.ThirdPart;

public static class AchtungPatches
{
    public static bool Prepare()
    {
        return ModsConfig.IsActive("brrainz.achtung");
    }

    [HarmonyPatch("Brrainz.DynamicWorkTypes", "Update")]
    public static class DynamicWorkTypesUpdate
    {
        public static bool Prepare()
        {
            return AchtungPatches.Prepare();
        }

        public static bool Prefix()
        {
            DefGenerator_GenerateImpliedDefs_PreResolve.ReBuildWorkTabColumns();
            return false;
        }
    }

    [HarmonyPatch("Brrainz.DynamicWorkTypes", "AddWorkTypeDef")]
    public static class DynamicWorkTypesAddWorkTypeDef
    {
        public static bool Prepare()
        {
            return AchtungPatches.Prepare();
        }

        public static void Prefix(ref WorkTypeDef copyFrom)
        {
            if (copyFrom == WorkTypeDefOf.Doctor)
            {
                // Fix [FSF] Complex Jobs mod changed name.
                copyFrom = DefDatabase<WorkTypeDef>.GetNamedSilentFail("FSFNurse") ?? WorkTypeDefOf.Doctor;
            }
        }

        public static Type ReturnNull(string typeName)
        {
            return null;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            var codeInstructions = instr.ToList();
            var typeByName = AccessTools.Method(typeof(AccessTools), nameof(AccessTools.TypeByName), new[] { typeof(string) });
            for (var i = 0; i < codeInstructions.Count - 1; i++)
            {
                var instruction = codeInstructions[i];
                if (instruction.opcode == OpCodes.Ldstr && Equals(instruction.operand, "WorkTab.PawnColumnDef_WorkGiver") &&
                    codeInstructions[i + 1].Calls(typeByName))
                {
                    codeInstructions[i + 1].operand = AccessTools.Method(typeof(DynamicWorkTypesAddWorkTypeDef), nameof(ReturnNull));
                }
            }
            return codeInstructions;
        }
    }
}
