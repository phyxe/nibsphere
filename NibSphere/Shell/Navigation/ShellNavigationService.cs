using NibSphere.Core.Modules;
using NibSphere.Modules;
using System.Collections.ObjectModel;

namespace NibSphere.Shell.Navigation
{
	public sealed class ShellNavigationService
	{
		private readonly ObservableCollection<ShellNavigationItem> _rootItems = new();

		public ReadOnlyObservableCollection<ShellNavigationItem> RootItems { get; }

		public ShellNavigationItem? ActiveItem { get; private set; }

		public string? ActiveItemKey => ActiveItem?.ItemKey;

		public ShellNavigationService(ModuleCatalog moduleCatalog)
		{
			RootItems = new ReadOnlyObservableCollection<ShellNavigationItem>(_rootItems);

			foreach (ShellNavigationItem item in BuildRootItems(moduleCatalog.ModuleDefinitions))
			{
				_rootItems.Add(item);
			}
		}

		public object? ActivateDefault()
		{
			ShellNavigationItem? defaultItem = FindDefaultItem();

			if (defaultItem == null)
			{
				return null;
			}

			return Activate(defaultItem);
		}

		public object? ActivateByKey(string itemKey)
		{
			if (string.IsNullOrWhiteSpace(itemKey))
			{
				return null;
			}

			ShellNavigationItem? item = FindByKey(itemKey);

			return item == null
				? null
				: Activate(item);
		}

		public object? Activate(ShellNavigationItem item)
		{
			if (item.HasChildren && item.ContentFactory == null)
			{
				item.IsExpanded = !item.IsExpanded;
				return null;
			}

			if (!item.CanActivate)
			{
				return null;
			}

			ClearState();

			item.IsActive = true;
			ActiveItem = item;

			ShellNavigationItem? parent = item.Parent;

			while (parent != null)
			{
				parent.HasActiveChild = true;
				parent.IsExpanded = true;
				parent = parent.Parent;
			}

			return item.ContentFactory?.Invoke();
		}

		public void CollapseAll()
		{
			foreach (ShellNavigationItem item in _rootItems)
			{
				CollapseRecursive(item);
			}
		}

		public ShellNavigationItem? FindByKey(string itemKey)
		{
			foreach (ShellNavigationItem root in _rootItems)
			{
				ShellNavigationItem? match = FindByKeyRecursive(root, itemKey);

				if (match != null)
				{
					return match;
				}
			}

			return null;
		}

		private static List<ShellNavigationItem> BuildRootItems(
			IReadOnlyList<IAppModuleDefinition> moduleDefinitions)
		{
			List<ShellNavigationItem> items = new();

			foreach (IAppModuleDefinition module in moduleDefinitions
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
			{
				foreach (ModuleNavItemDefinition navItem in module.NavigationItems
					.OrderBy(x => x.SortOrder)
					.ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase))
				{
					items.Add(CreateShellItem(module.ModuleKey, navItem, null));
				}
			}

			return items
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static ShellNavigationItem CreateShellItem(
			string moduleKey,
			ModuleNavItemDefinition definition,
			ShellNavigationItem? parent)
		{
			ShellNavigationItem item = new()
			{
				ModuleKey = moduleKey,
				ItemKey = definition.ItemKey,
				Title = definition.Title,
				IconPath = definition.IconPath,
				SortOrder = definition.SortOrder,
				IsDefault = definition.IsDefault,
				ContentFactory = definition.ContentFactory,
				Parent = parent
			};

			foreach (ModuleNavItemDefinition child in definition.Children
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase))
			{
				item.Children.Add(CreateShellItem(moduleKey, child, item));
			}

			return item;
		}

		private ShellNavigationItem? FindDefaultItem()
		{
			foreach (ShellNavigationItem root in _rootItems)
			{
				ShellNavigationItem? match = FindDefaultRecursive(root);

				if (match != null)
				{
					return match;
				}
			}

			foreach (ShellNavigationItem root in _rootItems)
			{
				ShellNavigationItem? firstActivatable = FindFirstActivatableRecursive(root);

				if (firstActivatable != null)
				{
					return firstActivatable;
				}
			}

			return null;
		}

		private static ShellNavigationItem? FindDefaultRecursive(ShellNavigationItem item)
		{
			if (item.IsDefault && item.CanActivate)
			{
				return item;
			}

			foreach (ShellNavigationItem child in item.Children)
			{
				ShellNavigationItem? match = FindDefaultRecursive(child);

				if (match != null)
				{
					return match;
				}
			}

			return null;
		}

		private static ShellNavigationItem? FindFirstActivatableRecursive(ShellNavigationItem item)
		{
			if (item.CanActivate)
			{
				return item;
			}

			foreach (ShellNavigationItem child in item.Children)
			{
				ShellNavigationItem? match = FindFirstActivatableRecursive(child);

				if (match != null)
				{
					return match;
				}
			}

			return null;
		}

		private static ShellNavigationItem? FindByKeyRecursive(ShellNavigationItem item, string itemKey)
		{
			if (string.Equals(item.ItemKey, itemKey, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}

			foreach (ShellNavigationItem child in item.Children)
			{
				ShellNavigationItem? match = FindByKeyRecursive(child, itemKey);

				if (match != null)
				{
					return match;
				}
			}

			return null;
		}

		private void ClearState()
		{
			foreach (ShellNavigationItem item in _rootItems)
			{
				ClearStateRecursive(item);
			}

			ActiveItem = null;
		}

		private static void ClearStateRecursive(ShellNavigationItem item)
		{
			item.IsActive = false;
			item.HasActiveChild = false;

			foreach (ShellNavigationItem child in item.Children)
			{
				ClearStateRecursive(child);
			}
		}

		private static void CollapseRecursive(ShellNavigationItem item)
		{
			item.IsExpanded = false;

			foreach (ShellNavigationItem child in item.Children)
			{
				CollapseRecursive(child);
			}
		}
	}
}