using System;

namespace ProtoSprite.Editor
{
	[AttributeUsage(AttributeTargets.Method)]
	public class ProtoSpriteAddMenuItemAttribute : Attribute
	{
		public string menuName { get; set; }

		public ProtoSpriteAddMenuItemAttribute(string menuName)
		{
			this.menuName = menuName;
		}
	}
}