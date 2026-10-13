using System.Reflection;
using UnrealAgent.Backend.Model.Attributes;

namespace UnrealAgent.Backend.Model;

/// <summary>
/// 어셈블리에서 [AgentModel] 어트리뷰트가 붙은 클래스를 스캔하여 모델 목록을 관리
/// </summary>
public sealed class ModelRegistry
{
	private readonly List<IModel> Models = [];

	public IReadOnlyList<IModel> CurrentModels => Models;
	
	public IModel? FindById(string Id) => Models.FirstOrDefault(M => M.Id == Id);

	/// <summary>
	/// 지정된 어셈블리에서 [AgentModel] 클래스를 스캔
	/// Order 속성 기준으로 정렬하고 같은 모델 ID는 한 번만 등록
	/// </summary>
	public void DiscoverModels(params Assembly[] Assemblies)
	{
		List<(IModel Model, int Order)> Discovered = [];
		HashSet<string> ModelIds = Models.Select(M => M.Id).ToHashSet(StringComparer.Ordinal);

		foreach (Assembly Asm in Assemblies)
		{
			foreach (Type Type in Asm.GetTypes())
			{
				AgentModelAttribute? Attr = Type.GetCustomAttribute<AgentModelAttribute>();

				if (Attr is null || Attr.bIsLegacy)
					continue;

				if (!typeof(IModel).IsAssignableFrom(Type))
					continue;

				if (Activator.CreateInstance(Type) is IModel Model)
					Discovered.Add((Model, Attr.Order));
			}
		}
		
		Discovered.Sort((A, B) => A.Order.CompareTo(B.Order));
		Models.AddRange(Discovered.Select(E => E.Model));
	}
}
