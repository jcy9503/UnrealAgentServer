using Microsoft.AspNetCore.Components;

namespace UnrealAgent.Frontend.UI.Input;

public partial class ChatInput
{
	[Parameter] public EventCallback<string> OnSend { get; set; }

	private string InputText = "";

	private async Task HandleSubmit()
	{
		string Trimmed = InputText.Trim();

		if (string.IsNullOrEmpty(Trimmed))
			return;

		InputText = "";
		await OnSend.InvokeAsync(Trimmed);
	}
}
