using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Model;

namespace UnrealAgent.Frontend.UI.Input;

public partial class ModelSelector
{
	[Inject] private ModelSettings Settings { get; set; } = null!;

	[Inject] private ModelRegistry Registry { get; set; } = null!;

	private bool bIsOpen;
	
	private void ToggleDropdown() => bIsOpen = !bIsOpen;

	private string ModelIcon => Settings.DisplayName.Length > 0
		? Settings.DisplayName[0].ToString()
		: "U";

	private void SelectModel(IModel Model)
	{
		Settings.Select(Model);
		bIsOpen = false;
	}

	private static string GetIconBg(IModel Model) => Model.DisplayName[0] switch
	{
		'O' => "bg-[#ff8c00]",
		'F' => "bg-[#dc143c]",
		'S' => "bg-[#7fff00]",
		_   => "bg-[#7fffd4]"
	};

	private static string GetIconColor(IModel Model) => Model.DisplayName[0] switch
	{
		'O' => "text-[#f0f8ff]",
		'F' => "text-[#f0f8ff]", 
		_ => "text-[#000000]"
	};
}
