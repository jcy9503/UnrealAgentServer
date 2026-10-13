using UnrealAgent.Backend.Model.Attributes;

namespace UnrealAgent.Backend.Model.Models;

/// <summary>
/// Claude Haiku 5.5 모델 정의
/// </summary>
[AgentModel(Order = 3)]
public class Haiku55 : IModel
{
	public const string ModelId = "claude-haiku-5-5";
	public       string Id              => ModelId;
	public       string DisplayName     => "Haiku 5.5";
	public       string Description     => "분류, 정보 추출, 라우팅 등 대량 처리와 빠른 응답이 필요한 작업을 위한 모델입니다.";
	public       int    MaxOutputTokens => 128_000;
	public       int    ContextWindow   => 1_000_000;
}
