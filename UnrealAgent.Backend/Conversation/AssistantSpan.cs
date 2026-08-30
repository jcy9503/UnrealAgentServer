using UnrealAgent.Backend.Core;

namespace UnrealAgent.Backend.Conversation;

/// <summary>
/// Claude API 호출 1회 결과
/// 어시스턴트 응답 블록과 도구 실행 결과를 포함
/// </summary>
public sealed class AssistantSpan
{
	/// <summary>
	/// 어시스턴트 응답 블록 목록
	/// </summary>
	public required IReadOnlyList<Block> AssistantBlocks { get; init; }

	public sealed record ToolExecution(string ToolUseId, string Name, string Output, bool bIsError);

	public List<ToolExecution> ToolExecutions { get; } = [];
}
