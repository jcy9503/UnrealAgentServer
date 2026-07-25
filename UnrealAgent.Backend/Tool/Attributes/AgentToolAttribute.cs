namespace UnrealAgent.Backend.Tool.Attributes;

/// <summary>
/// ToolRegistry가 자동 스캔하는 도구 마커 어트리뷰트입니다.
/// Claude API에 전달할 도구 이름과 설명을 지정합니다. 
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AgentToolAttribute(string name, string description) : Attribute
{
	/// <summary>
	/// Claude API에 전달할 도구 이름
	/// </summary>
	public string Name { get; } = name;
	
	/// <summary>
	/// Claude 에게 보여줄 도구 설명
	/// </summary>
	public string Description { get; } = description;
}
