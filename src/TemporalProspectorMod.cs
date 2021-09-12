using Vintagestory.API.Common;

[assembly: ModInfo( "TemporalProspector",
	Description = "An example mod using VS Code and .NET",
	Website     = "https://github.com/copygirl/howto-example-mod",
	Authors     = new []{ "AlexDGr8r" } )]

namespace TemporalProspector
{
	public class TemporalProspectorMod : ModSystem
	{
		public override void Start(ICoreAPI api)
		{
			api.RegisterItemClass("ItemTemporalProspectingPick", typeof(ItemTemporalProspectingPick));
		}
	}
}
