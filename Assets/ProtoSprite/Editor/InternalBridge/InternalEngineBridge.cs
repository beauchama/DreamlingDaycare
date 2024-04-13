using UnityEngine;
using UnityEditor.ShortcutManagement;

namespace ProtoSprite.Editor
{
    public static class InternalEngineBridge
    {
		public class ShortcutContext : IShortcutToolContext
		{
			public bool active
			{
				get
				{
					if (ShortcutIntegration.instance.contextManager.HasAnyPriorityContext())
						return false;

					return true;
				}
			}
		}

		public static void RegisterShortcutContext(ShortcutContext context)
		{
			ShortcutIntegration.instance.contextManager.RegisterToolContext(context);
		}

		public static void UnregisterShortcutContext(ShortcutContext context)
		{
			ShortcutIntegration.instance.contextManager.DeregisterToolContext(context);
		}
	}
}