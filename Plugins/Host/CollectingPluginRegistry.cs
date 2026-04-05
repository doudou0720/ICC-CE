using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using InkCanvasForClass.PluginSdk;

namespace InkCanvasForClass.PluginHost
{
    /// <summary>
    /// 收集插件登记的菜单 / 工具栏 / 设置页，供宿主窗口在启动后统一挂载。
    /// </summary>
    public sealed class CollectingPluginRegistry : IPluginRegistry
    {
        private string _currentPluginId = "";

        public string CurrentPluginId => _currentPluginId;

        public ObservableCollection<MenuItemRegistration> MenuItems { get; } =
            new ObservableCollection<MenuItemRegistration>();

        public ObservableCollection<ToolbarButtonRegistration> ToolbarButtons { get; } =
            new ObservableCollection<ToolbarButtonRegistration>();

        public ObservableCollection<SettingsPageRegistration> SettingsPages { get; } =
            new ObservableCollection<SettingsPageRegistration>();

        public void SetCurrentPluginId(string pluginId)
        {
            _currentPluginId = pluginId ?? "";
        }

        public void RegisterMenuItem(string groupKey, MenuItem item)
        {
            if (item == null) return;
            MenuItems.Add(new MenuItemRegistration(_currentPluginId, groupKey ?? "", item));
        }

        public void RegisterToolbarButton(Button button)
        {
            if (button == null) return;
            ToolbarButtons.Add(new ToolbarButtonRegistration(_currentPluginId, button));
        }

        public void RegisterSettingsPage(string pageId, string displayName, Func<UserControl> createView)
        {
            if (string.IsNullOrWhiteSpace(pageId) || createView == null) return;
            SettingsPages.Add(new SettingsPageRegistration(
                _currentPluginId,
                pageId,
                displayName ?? pageId,
                createView));
        }

        public void Clear()
        {
            MenuItems.Clear();
            ToolbarButtons.Clear();
            SettingsPages.Clear();
            _currentPluginId = "";
        }
    }

    public sealed class MenuItemRegistration
    {
        public MenuItemRegistration(string pluginId, string groupKey, MenuItem item)
        {
            PluginId = pluginId ?? "";
            GroupKey = groupKey ?? "";
            Item = item ?? throw new ArgumentNullException(nameof(item));
        }

        public string PluginId { get; }
        public string GroupKey { get; }
        public MenuItem Item { get; }
    }

    public sealed class ToolbarButtonRegistration
    {
        public ToolbarButtonRegistration(string pluginId, Button button)
        {
            PluginId = pluginId ?? "";
            Button = button ?? throw new ArgumentNullException(nameof(button));
        }

        public string PluginId { get; }
        public Button Button { get; }
    }

    public sealed class SettingsPageRegistration
    {
        public SettingsPageRegistration(string pluginId, string pageId, string displayName, Func<UserControl> createView)
        {
            PluginId = pluginId ?? "";
            PageId = pageId ?? "";
            DisplayName = displayName ?? "";
            CreateView = createView ?? throw new ArgumentNullException(nameof(createView));
        }

        public string PluginId { get; }
        public string PageId { get; }
        public string DisplayName { get; }
        public Func<UserControl> CreateView { get; }
    }
}
