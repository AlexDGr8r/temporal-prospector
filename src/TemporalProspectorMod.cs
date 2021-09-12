using Vintagestory.API.Common;

[assembly: ModInfo( "TemporalProspector",
	Description = "Temporally prospect the resources of the world",
	Website     = "https://github.com/AlexDGr8r/temporal-prospector",
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
