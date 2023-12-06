#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using XRL.Core;
using XRL.UI;
using XRL;
using XRL.World;

namespace ItsYourChoice
{
	[HarmonyPatch(typeof(XRLCore))]
	[HarmonyPatch(nameof(XRLCore.PlayerTurn))]
	[HarmonyPatch(new Type[] { })]
	public class XRLCore_PlayerTurn
	{
		public static void Detour()
		{
			CommandReloadEvent.Execute(The.Player);
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);

			matcher
				.Start()
				.MatchEndForward(new CodeMatch[] {
					new(OpCodes.Ldloc_S),
					new(OpCodes.Ldstr, "CmdReload"),
					new(OpCodes.Call, AccessTools.Method(
						type: typeof(string),
						name: "op_Equality",
						parameters: new Type[] { typeof(string), typeof(string), }
					)),
					new(OpCodes.Brtrue),
				});
			if (matcher.IsInvalid) {
				Logger.buildLog.Error("Failed to match reload action block!");
				return instructions;
			}
			var reloadLabel = (Label)matcher.Operand;

			var reloadWarning = new CodeMatch[] {
				new(OpCodes.Ldloc_S),
				new(OpCodes.Brtrue_S),

				new(OpCodes.Ldnull),
				new(OpCodes.Br_S),

				new(OpCodes.Ldloc_S),
				new(OpCodes.Ldstr, "NoAmmoSound"),
				new(OpCodes.Ldnull),
				new(OpCodes.Call, AccessTools.Method(
					type: typeof(GameObject),
					name: nameof(GameObject.GetSoundTag),
					parameters: new Type[] { typeof(string), typeof(string) }
				)),

				new(OpCodes.Ldc_R4, 0.0f),
				new(OpCodes.Ldc_R4, 1f),
				new(OpCodes.Ldc_R4, 1f),
				new(OpCodes.Ldc_I4_0),
				new(OpCodes.Ldc_R4, 0.0f),
				new(OpCodes.Call, AccessTools.Method(
					type: typeof(SoundManager),
					name: nameof(SoundManager.PlaySound),
					parameters: new Type[] { typeof(string), typeof(float), typeof(float), typeof(float), typeof(SoundRequest.SoundEffectType), typeof(float) }
				)),
				new(OpCodes.Ldloc_S),
				new(OpCodes.Dup),
				new(OpCodes.Brtrue_S),

				new(OpCodes.Pop),
				new(OpCodes.Ldstr, "You need to reload! ("),
				new(OpCodes.Ldstr, "CmdReload"),
				new(OpCodes.Call, AccessTools.Method(
					type: typeof(Options),
					name: "get_ModernUI",
					parameters: new Type[] { }
				)),
				new(OpCodes.Ldc_I4_0),
				new(OpCodes.Ldc_I4_3),
				new(OpCodes.Call, AccessTools.Method(
					type: typeof(ControlManager),
					name: nameof(ControlManager.getCommandInputDescription),
					parameters: new Type[] { typeof(string), typeof(bool), typeof(bool), typeof(ControlManager.InputDeviceType),  }
				)),
				new(OpCodes.Ldstr, ")"),
				new(OpCodes.Call, AccessTools.Method(
					type: typeof(string),
					name: nameof(string.Concat),
					parameters: new Type[] { typeof(string), typeof(string), typeof(string), }
				)),

				new(OpCodes.Ldc_I4_1),
				new(OpCodes.Ldc_I4_1),
				new(OpCodes.Ldc_I4_1),
				new(OpCodes.Call, AccessTools.Method(
					type: typeof(Popup),
					name: nameof(Popup.ShowFail),
					parameters: new Type[] { typeof(string), typeof(bool), typeof(bool), typeof(bool), }
				)),
				new(OpCodes.Br),
			};
			matcher
				.Start()
				.MatchStartForward(reloadWarning);
			if (matcher.IsInvalid) {
				Logger.buildLog.Error("Failed to match reload warning block!");
				return instructions;
			}

			var result = matcher
				.RemoveInstructions(reloadWarning.Length)
				.Insert(new CodeInstruction[] {
					new(OpCodes.Br, reloadLabel),
				})
				.Instructions();
			return result;
		}
	}
}
