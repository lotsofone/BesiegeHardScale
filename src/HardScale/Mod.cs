using System;
using Modding;
using UnityEngine;

namespace HardScale
{
	public class Mod : ModEntryPoint
	{
        public static GameObject Instance;
		public override void OnLoad()
        {
            Mod.Instance = new GameObject("Hard Scale Mod");
            UnityEngine.Object.DontDestroyOnLoad(Mod.Instance);
            Mod.Instance.AddComponent<ScaleUI>();
            // Called when the mod is loaded.
        }
	}
}
