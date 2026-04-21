using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules;
using NibSphere.Data.Database;
using System.Reflection;

namespace NibSphere.Modules
{
	public sealed class ModuleCatalog
	{
		private readonly IReadOnlyList<IAppModuleDefinition> _moduleDefinitions;
		private readonly IReadOnlyList<IModuleDatabaseInitializer> _databaseInitializers;

		public IReadOnlyList<IAppModuleDefinition> ModuleDefinitions => _moduleDefinitions;

		public IReadOnlyList<IModuleDatabaseInitializer> DatabaseInitializers => _databaseInitializers;

		private ModuleCatalog(
			IReadOnlyList<IAppModuleDefinition> moduleDefinitions,
			IReadOnlyList<IModuleDatabaseInitializer> databaseInitializers)
		{
			_moduleDefinitions = moduleDefinitions;
			_databaseInitializers = databaseInitializers;
		}

		public static ModuleCatalog CreateDefault()
		{
			Assembly[] assemblies = GetDefaultAssemblies();

			List<IAppModuleDefinition> moduleDefinitions = CreateInstances<IAppModuleDefinition>(assemblies)
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
				.ToList();

			List<IModuleDatabaseInitializer> databaseInitializers = CreateInstances<IModuleDatabaseInitializer>(assemblies)
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.ModuleKey, StringComparer.OrdinalIgnoreCase)
				.ToList();

			return new ModuleCatalog(moduleDefinitions, databaseInitializers);
		}

		public async Task InitializeDatabasesAsync(
			IAppPaths appPaths,
			CancellationToken cancellationToken = default)
		{
			foreach (IModuleDatabaseInitializer initializer in _databaseInitializers)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await initializer.InitializeAsync(appPaths, cancellationToken);
			}
		}

		private static Assembly[] GetDefaultAssemblies()
		{
			return new[]
			{
				typeof(App).Assembly,
				typeof(DatabaseInitializer).Assembly,
				typeof(IAppModuleDefinition).Assembly
			}
			.Distinct()
			.ToArray();
		}

		private static List<TContract> CreateInstances<TContract>(
			IEnumerable<Assembly> assemblies)
			where TContract : class
		{
			List<TContract> instances = new();
			HashSet<string> seenTypes = new(StringComparer.Ordinal);

			foreach (Assembly assembly in assemblies.Distinct())
			{
				foreach (Type type in GetLoadableTypes(assembly))
				{
					if (!typeof(TContract).IsAssignableFrom(type) ||
						!type.IsClass ||
						type.IsAbstract)
					{
						continue;
					}

					if (type.GetConstructor(Type.EmptyTypes) == null)
					{
						continue;
					}

					string typeKey = type.FullName ?? type.Name;

					if (!seenTypes.Add(typeKey))
					{
						continue;
					}

					if (Activator.CreateInstance(type) is TContract instance)
					{
						instances.Add(instance);
					}
				}
			}

			return instances;
		}

		private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				return ex.Types.Where(x => x != null)!;
			}
		}
	}
}