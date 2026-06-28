namespace UnrealAgent.Backend.Conversation;

/// <summary>
/// 사용자 입력 메시지. 텍스트와 첨부 이미지 포함.
/// </summary>
/// <param name="Text"></param>
public sealed record UserInput(string Text)
{
	public static implicit operator UserInput(string Text) => new(Text);
}
