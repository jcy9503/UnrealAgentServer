using Microsoft.AspNetCore.Components;

namespace UnrealAgent.Frontend.UI.Layout;

public partial class Header
{
	[Parameter] public EventCallback OnSettingsClick { get; set; }
}
