using Anthropic.Models.Messages;
using Microsoft.Extensions.DependencyInjection;
using UnrealAgent.Backend.Agent;
using UnrealAgent.Backend.Auth;
using UnrealAgent.Backend.Conversation;
using UnrealAgent.Backend.Core;
using UnrealAgent.Backend.Prompt;

ServiceCollection Services = new ServiceCollection();
Services.AddSingleton<AuthConfig>();
Services.AddSingleton<AgentSession>();
Services.AddSingleton<PromptBuilder>();

ServiceProvider serviceProvider = Services.BuildServiceProvider();
AuthConfig Auth = serviceProvider.GetRequiredService<AuthConfig>();
AgentSession AgentSession = serviceProvider.GetRequiredService<AgentSession>();
PromptBuilder PromptBuilder = serviceProvider.GetRequiredService<PromptBuilder>();

Auth.Load();

if (!Auth.IsApiKeyConfigured())
{
	Console.Write("API Key를 입력하세요: ");
	string? Key = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(Key))
	{
		Console.WriteLine("API Key가 입력되지 않았습니다.");

		return;
	}

	Auth.SetApiKey(Key);
	Console.WriteLine("API Key 저장 완료!");
}

while (true)
{
	// 사용자 입력 대기
	Console.Write("\n> ");
	string? Input = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(Input))
		continue;

	if (Input.Equals("exit", StringComparison.OrdinalIgnoreCase))
		break;
	
	// 대화 히스토리에 사용자 입력 추가
	MessageSpan CurrentMessageSpan = AgentSession.Conversation.AddMessageSpan(Input);

	// API 요청 파라미터 구성
	MessageCreateParams Parameters = PromptBuilder.Build(AgentSession);

	// 스트리밍 응답 수신 및 출력
	ApiStreamSpan ApiStreamSpan = new ApiStreamSpan();

	// 현재 출력 중인 섹션(thinking / text)을 추적해 전환 시 헤더를 한 번만 출력
	string? Section = null;

	await foreach (RawMessageStreamEvent Event in Auth.Client!.Messages.CreateStreaming(Parameters))
	{
		switch (ApiStreamSpan.Process(Event))
		{
			case ChatEvent.Thinking Think:
			{
				if (Section != "thinking")
				{
					Section = "thinking";
					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.WriteLine("\n🤔 [생각]");
				}

				Console.ForegroundColor = ConsoleColor.DarkGray;
				Console.Write(Think.Content);

				break;
			}

			case ChatEvent.Text Txt:
			{
				if (Section != "text")
				{
					Section = "text";
					Console.ResetColor();
					Console.WriteLine("\n💬 [답변]");
				}

				Console.Write(Txt.Content);

				break;
			}
		}
	}
	
	// 완료된 응답을 대화 히스토리에 저장
	switch (ApiStreamSpan.Complete())
	{
		case ApiStreamSpan.Result.EndSpan { CompletedSpan: { } AssistantSpan }:
		{
			CurrentMessageSpan.AssistantSpans.Add(AssistantSpan);

			break;
		}
	}

	Console.ResetColor();
	
	Console.WriteLine();
}