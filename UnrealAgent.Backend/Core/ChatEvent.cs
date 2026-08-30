namespace UnrealAgent.Backend.Core;

public abstract record ChatEvent
{
	public sealed record Text(string Content) : ChatEvent;

	public sealed record Thinking(string Content) : ChatEvent;

	public sealed record ToolStart(string ToolUseId, string Name, string Input) : ChatEvent;
	
	public sealed record ToolEnd(string ToolUseId, string Name, string Result) : ChatEvent;
	
	public sealed record Done : ChatEvent;
}
