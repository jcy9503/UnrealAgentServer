using UnrealAgent.Backend.Model.Attributes;

namespace UnrealAgent.Backend.Model.Models;

/// <summary>
/// Claude Fable 5.1 모델 정의
/// </summary>
[AgentModel(Order = 1)]
public class Fable51 : IModel
{
	public const string ModelId = "claude-fable-5-1";
	public       string Id              => ModelId;
	public       string DisplayName     => "Fable 5.1";
	public       string Description     => "고난도 추론과 장기적인 에이전트 작업을 위한 모델입니다.";
	public       int    MaxOutputTokens => 128_000;
	public       int    ContextWindow   => 1_000_000;
}
