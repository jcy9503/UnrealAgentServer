using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Model;

namespace UnrealAgent.Frontend.UI.Input;

public partial class ThinkingToggle
{
	[Inject] private ModelSettings Settings { get; set; } = null!;

	private void Toggle() { Settings.bThinkingEnabled = !Settings.bThinkingEnabled; }

	private string LabelColorClass => Settings.bThinkingEnabled ? "text-white" : "text-[#666]";

	private string TrackClass => Settings.bThinkingEnabled ? "think-on" : "think-off";
}
