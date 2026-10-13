using UnrealAgent.Backend.Model.Attributes;

namespace UnrealAgent.Backend.Model.Models;

/// <summary>
/// Claude Sonnet 5.5 모델 정의
/// </summary>
[AgentModel(Order = 2)]
public sealed class Sonnet55 : IModel
{
	public const string ModelId = "claude-sonnet-5-5";
	public       string Id              => ModelId;
	public       string DisplayName     => "Sonnet 5.5";
	public       string Description     => "속도와 지능이 최적으로 조화를 이룬 모델입니다.";
	public       int    MaxOutputTokens => 128_000;
	public       int    ContextWindow   => 1_000_000;
}
