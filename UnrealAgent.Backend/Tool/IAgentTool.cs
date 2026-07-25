using System.Text.Json;
using UnrealAgent.Backend.Agent;

namespace UnrealAgent.Backend.Tool;

//----------------------------------------------------------------------------------------
// IAgentTool
//----------------------------------------------------------------------------------------

/// <summary>
/// 에이전트 도구 실행 인터페이스입니다.
/// [AgentTool] 어트리뷰트와 함께 구현하면 ToolRegistry 가 자동 스캔합니다.
/// </summary>
public interface IAgentTool
{
	/// <summary>
	/// 도구를 실행하고 결과를 반환
	/// </summary>
	Task<ToolResult> ExecuteAsync(string InputJson, AgentSession Session, CancellationToken Ct = default);
}

//----------------------------------------------------------------------------------------
// AgentTool<TInput>
//----------------------------------------------------------------------------------------

/// <summary>
/// 타입 안전 도구 기본 클래스
/// JSON 입력을 TInput 레코드로 자동 역직렬화
/// </summary>
/// <example>
/// <code>
/// public sealed record Input(
///		[property: JsonPropertyName("city")]
///		[property: Description("날씨를 조회할 도시 이름 (예: 'Seoul', 'Busan')")]
///		string City,
///
///		[property: JsonPropertyName("unit")]
///		[property: Description("온도 단위: 'celsius' 또는 'fahrenheit' (기본값: celsius)")]
///		string? Unit = null);
/// </code>
/// </example>
public abstract class AgentTool<TInput> : IAgentTool
{
	/// <summary>
	/// JSON 문자열을 TInput으로 역직렬화를 하여 실행
	/// </summary>
	public Task<ToolResult> ExecuteAsync(string InputJson, AgentSession Session, CancellationToken Ct = default)
	{
		TInput Input = JsonSerializer.Deserialize<TInput>(InputJson) ?? throw new ArgumentException($"Failed to deserialize Input: {InputJson}");

		return ExecuteAsync(Input, Session, Ct);
	}
	
	/// <summary>
	/// 타입 안전한 도구 실행 메서드
	/// </summary>
	protected abstract Task<ToolResult> ExecuteAsync(TInput Input, AgentSession Session, CancellationToken Ct);
}
