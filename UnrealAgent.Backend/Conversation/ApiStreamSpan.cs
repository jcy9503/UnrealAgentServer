using System.Text;
using Anthropic.Models.Messages;
using UnrealAgent.Backend.Core;
using Block = UnrealAgent.Backend.Core.Block;

namespace UnrealAgent.Backend.Conversation;

/// <summary>
/// API 호출 1회의 스트리밍 응답을 파싱하고 누적
/// Process()로 이벤트를 먹이고, Complete()로 결과를 확정
/// </summary>
public sealed class ApiStreamSpan
{
	/// <summary>
	/// API 스트리밍 완료 후 다음 행동을 나타내는 판별 유니온
	/// AgentLoop에서 패턴 매칭으로 루프를 제어
	/// </summary>
	public abstract record Result
	{
		/// <summary>
		/// 응담/사고가 잘려서 이어서 생성해야 함.
		/// 도구 실행 없이 다음 API 호출로 이어감.
		/// </summary>
		public sealed record Continue(AssistantSpan CompletedSpan) : Result;
		
		/// <summary>
		/// 대화가 완료
		/// </summary>
		public sealed record EndSpan(AssistantSpan CompletedSpan) : Result;
	}
	
	private abstract record ActiveBlock
	{
		public sealed record Text : ActiveBlock;
		public sealed record Thinking : ActiveBlock;
	}
	
	private ActiveBlock? CurrentBlock;
	
	private readonly StringBuilder TextBuffer     = new();
	private readonly StringBuilder ThinkingBuffer = new();
	private          string?       ThinkingSignature;
	
	/// <summary>
	/// 메시지 레벨 종료 사유
	/// </summary>
	public StopReason? FinalStopReason;

	private readonly List<Block> AssistantBlocks = [];
	
	public IReadOnlyList<Block> Blocks => AssistantBlocks;
	
	public ChatEvent? Process(RawMessageStreamEvent Event)
	{
		if (Event.TryPickContentBlockStart(out RawContentBlockStartEvent? StartEvt))	// 블록 시작
			return ProcessBlockStart(StartEvt);
		
		if (Event.TryPickContentBlockDelta(out RawContentBlockDeltaEvent? DeltaEvt))	// 글자 조각 (델타)
			return ProcessDelta(DeltaEvt);

		if (Event.TryPickContentBlockStop(out RawContentBlockStopEvent? StopEvt))		// 블록 끝
			return ProcessBlockStop();
		
		if (Event.TryPickStart(out RawMessageStartEvent? StartMsgEvt))					// 메시지 시작
			return ProcessMessageStart(StartMsgEvt);
		
		if (Event.TryPickDelta(out RawMessageDeltaEvent? MsgDelta))						// 메시지 끝 (StopReason)
			return ProcessMessageDelta(MsgDelta);

		return null;
	}

	private ChatEvent? ProcessDelta(RawContentBlockDeltaEvent DeltaEvt)
	{
		switch (CurrentBlock)
		{
			case ActiveBlock.Text when DeltaEvt.Delta.TryPickText(out TextDelta? TextDelta):
			{
				TextBuffer.Append(TextDelta.Text);
				return new ChatEvent.Text(TextDelta.Text);
			}

			case ActiveBlock.Thinking when DeltaEvt.Delta.TryPickThinking(out ThinkingDelta? ThinkingDelta):
			{
				ThinkingBuffer.Append(ThinkingDelta.Thinking);
				return new ChatEvent.Thinking(ThinkingDelta.Thinking);
			}

			case ActiveBlock.Thinking when DeltaEvt.Delta.TryPickSignature(out SignatureDelta? SignatureDelta):
			{
				ThinkingSignature = SignatureDelta.Signature;
				return null;
			}
			
			default:
				return null;
		}
		return null;
	}
	
	private ChatEvent? ProcessBlockStop()
	{
		switch (CurrentBlock)
		{
			case ActiveBlock.Text:
			{
				if (TextBuffer.Length > 0)
				{
					AssistantBlocks.Add(new Block.Text(TextBuffer.ToString()));
					TextBuffer.Clear();
				}

				break;
			}

			case ActiveBlock.Thinking:
			{
				if (ThinkingBuffer.Length > 0)
				{
					AssistantBlocks.Add(new Block.Thinking(ThinkingBuffer.ToString(), ThinkingSignature));
					ThinkingBuffer.Clear();
					ThinkingSignature = null;
				}

				break;
			}
		}
		
		CurrentBlock = null;
		return null;
	}

	private ChatEvent? ProcessMessageStart(RawMessageStartEvent StartMsgEvt)
	{
		return null;
	}
	
	private ChatEvent? ProcessMessageDelta(RawMessageDeltaEvent MsgDelta)
	{
		if (MsgDelta.Delta.StopReason is { } Reason)
			FinalStopReason = Reason;
		
		return null;
	}
	
	private ChatEvent? ProcessBlockStart(RawContentBlockStartEvent StartEvt)
	{
		if (StartEvt.ContentBlock.TryPickText(out _))
		{
			CurrentBlock = new ActiveBlock.Text();
		}
		else if (StartEvt.ContentBlock.TryPickThinking(out _))
		{
			CurrentBlock = new ActiveBlock.Thinking();
		}

		return null;
	}

	/// <summary>
	/// 스트리밍을 완료하고 AssistantSpan을 생성
	/// 반환값으로 다음 행동(도구 실행, 이어서 호출, 종료)을 결정
	/// </summary>
	public Result Complete()
	{
		AssistantSpan CompleteSpan = new()
		{
			AssistantBlocks = AssistantBlocks.ToList(),
		};
		
		return new Result.EndSpan(CompleteSpan);
	}
}