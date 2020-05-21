using System;
using System.Reflection;

namespace KModkit
{
	public static class KMNeedyModuleExtensions
	{
		public static void SetResetDelayTime(this KMNeedyModule module, float min, float max)
		{
			if (module.gameObject.GetComponent("NeedyComponent") == null)
				return; // Running in the Test Harness?
			Type targetType = module.gameObject.GetComponent("NeedyComponent").GetType();
			FieldInfo resetMin = targetType.GetField("ResetDelayMin", BindingFlags.Public | BindingFlags.Instance);
			FieldInfo resetMax = targetType.GetField("ResetDelayMax", BindingFlags.Public | BindingFlags.Instance);
			resetMin.SetValue(module.gameObject.GetComponent("NeedyComponent"), min);
			resetMax.SetValue(module.gameObject.GetComponent("NeedyComponent"), max);
		}
	}
}
