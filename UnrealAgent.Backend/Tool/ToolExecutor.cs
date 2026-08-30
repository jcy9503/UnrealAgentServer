using System.Runtime.CompilerServices;
using UnrealAgent.Backend.Agent;
using UnrealAgent.Backend.Conversation;
using UnrealAgent.Backend.Core;

namespace UnrealAgent.Backend.Tool;

/// <summary>
/// 도구를 실행하고 결과를 AssistantSpan 에 기록
/// </summary>
public class ToolExecutor(ToolRegistry ToolRegistry)
{
	/// <summary>
	/// 도구를 실행하고 결과를ㄹ AssistantSpan의 ToolExecutions에 추가
	/// 도구가 세션 모드를 변경 시 ModeChanged 이벤트를 발행
	/// </summary>
	/// <param name="ToolCall"></param>
	/// <param name="AssistantSpan"></param>
	/// <param name="Session"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	public async IAsyncEnumerable<ChatEvent> ExecuteAsync(Block.ToolUse ToolCall, AssistantSpan        AssistantSpan,
	                                                      AgentSession  Session,  CancellationToken Ct = default)
	{
		yield return new ChatEvent.ToolStart(ToolCall.Id, ToolCall.Name, ToolCall.InputJson);

		ToolResult Result = await ToolRegistry.ExecuteAsync(ToolCall.Name, ToolCall.InputJson, Session, Ct);

		AssistantSpan.ToolExecution ToolExecution = new AssistantSpan.ToolExecution(ToolCall.Id, ToolCall.Name, Result.Content, !Result.bIsSuccess);
		AssistantSpan.ToolExecutions.Add(ToolExecution);
		
		yield return new ChatEvent.ToolEnd(ToolCall.Id, ToolCall.Name, Result.Content);
	}
}
