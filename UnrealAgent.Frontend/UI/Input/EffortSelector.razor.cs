using Anthropic.Models.Messages;
using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Model;

namespace UnrealAgent.Frontend.UI.Input;

public partial class EffortSelector
{
	[Inject] private ModelSettings Settings { get; set; } = null!;

	private record EffortLevel(string Label, Effort Value, string Description);

	private static readonly EffortLevel[] Levels =
	[
		new("Low", Effort.Low, "가장 효율적. 일부 성능 저하와 함께 상당한 토큰 절감 제공"),
		new("Mid", Effort.Medium, "적당한 토큰 절감을 제공하는 균형 잡힌 접근 방식"),
		new("High", Effort.High, "탁월한 결과를 위해 작업에 필요한 만큼 토큰을 사용"),
		new("XHigh", Effort.Xhigh, "장기 작업을 위한 확장된 성능"),
		new("Max", Effort.Max, "토큰 사용에 제약이 없는 절대 최대 성능")
	];

	private string EffortLabelClass => Settings.Effort == Effort.Max ? "text-white" : "text=[#666]";

	private void SelectEffort(Effort Level)
	{
		Settings.Effort = Level;
	}
}