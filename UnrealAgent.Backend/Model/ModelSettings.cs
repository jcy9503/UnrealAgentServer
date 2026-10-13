using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic.Models.Messages;
using Microsoft.Win32;
using UnrealAgent.Backend.Core;

namespace UnrealAgent.Backend.Model;

/// <summary>
/// API 런타임 설정 싱글톤
/// 모델 변경 시 업데이트하면 즉시 반영
/// 설정 ~/.unrealagent/ModelSettings.json 에 자동 저장
/// </summary>
public sealed class ModelSettings(ModelRegistry Registry)
{
	private readonly string ConfigPath = Path.Combine(AgentPaths.UserConfigDir, "ModelSettings.json");

	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

	private IModel CurrentModel = new Models.Opus55();

	private bool ThinkingEnabled = true;

	private Effort CurrentEffort = Effort.High;

	public string Model => CurrentModel.Id;

	public string DisplayName => CurrentModel.DisplayName;

	public string Description => CurrentModel.Description;

	public int MaxTokens => CurrentModel.MaxOutputTokens;

	public int ContextWindow => CurrentModel.ContextWindow;

	public ThinkingConfigParam GetThinking() => bThinkingEnabled ? new ThinkingConfigAdaptive() : new ThinkingConfigDisabled();

	public OutputConfig GetEffort() => new() { Effort = Effort };

	public void Select(IModel ClaudeModel)
	{
		CurrentModel = ClaudeModel;
		Save();
	}

	public bool bThinkingEnabled
	{
		get => ThinkingEnabled;
		set
		{
			ThinkingEnabled = value;
			Save();
		}
	}

	public Effort Effort
	{
		get => CurrentEffort;
		set
		{
			CurrentEffort = value;
			Save();
		}
	}

	private void Save()
	{
		string Dir = Path.GetDirectoryName(ConfigPath)!;
		if (!Directory.Exists(Dir))
			Directory.CreateDirectory(Dir);

		JsonObject Root = new()
		{
			["model"] = Model,
			["thinking_enabled"] = ThinkingEnabled,
			["effort"] = CurrentEffort.ToString().ToLowerInvariant()
		};

		File.WriteAllText(ConfigPath, Root.ToJsonString(JsonOptions));
	}

	public void Load()
	{
		if (!File.Exists(ConfigPath))
			return;
		
		string Json = File.ReadAllText(ConfigPath);
		JsonNode? Root = JsonNode.Parse(Json);

		if (Root is null)
			return;

		if (Root["model"]?.GetValue<string>() is { } ModelId && Registry.FindById(ModelId) is { } Found)
			CurrentModel = Found;

		if (Root["thinking_enabled"] is not null)
			ThinkingEnabled = Root["thinking_enabled"]!.GetValue<bool>();

		if (Root["effort"]?.GetValue<string>() is { } EffortStr && Enum.TryParse<Effort>(EffortStr, true, out Effort ParsedEffort))
			CurrentEffort = ParsedEffort;
	}
}
