namespace UnrealAgent.Backend.Model.Attributes;

/// <summary>
/// ModelRegistry가 자동 스캔하는 모델 마커 어트리뷰트
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AgentModelAttribute : Attribute
{
	/// <summary>
	/// 레거시 모델 여부
	/// </summary>
	public bool bIsLegacy { get; set; } = false;

	/// <summary>
	/// 정렬 순서. 낮을수록 우선순위
	/// </summary>
	public int Order { get; set; } = 100;
}
