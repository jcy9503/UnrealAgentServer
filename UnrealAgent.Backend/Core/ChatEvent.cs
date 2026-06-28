namespace UnrealAgent.Backend.Core;

public abstract record ChatEvent
{
	public sealed record Text(string Content) : ChatEvent;

	public sealed record Thinking(string Content) : ChatEvent;
	
	public sealed record Done : ChatEvent;
}
