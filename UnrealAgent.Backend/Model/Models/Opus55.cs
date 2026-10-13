using UnrealAgent.Backend.Model.Attributes;

namespace UnrealAgent.Backend.Model.Models;

/// <summary>
/// Claude Opus 5.5 모델 정의
/// </summary>
[AgentModel(Order = 0)]
public sealed class Opus55 : IModel
{
	public const string ModelId = "claude-opus-5-5";
	public       string Id              => ModelId;
	public       string DisplayName     => "Opus 5.5";
	public       string Description     => "장시간 수행되는 에이전트 기반 코딩과 지식 작업을 위한 모델입니다.";
	public       int    MaxOutputTokens => 128_000;
	public       int    ContextWindow   => 1_000_000;
}
