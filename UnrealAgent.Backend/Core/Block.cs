namespace UnrealAgent.Backend.Core;

public abstract record Block
{
	public sealed record Text(string Content) : Block;

	public sealed record Thinking(string Content, string? Signature) : Block;
}