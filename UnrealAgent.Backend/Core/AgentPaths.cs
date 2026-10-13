namespace UnrealAgent.Backend.Core;

public static class AgentPaths
{
	public static readonly string UserConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".unrealagent");
}
