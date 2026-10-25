using System.Collections.ObjectModel;
using System.Windows;
using ComplianceServiceManager.Models.RuntimeProfiles;

namespace ComplianceServiceManager.Views.Development;

public partial class RuntimeProfileEditorWindow : Window
{
    private readonly RuntimeProfileItem source;
    private readonly ObservableCollection<EditableServiceRow> rows = [];

    public RuntimeProfileItem? Result { get; private set; }

    public RuntimeProfileEditorWindow(RuntimeProfileItem profile, IReadOnlyList<string> discoveredServiceNames)
    {
        InitializeComponent();
        source = profile;
        NameBox.Text = profile.Name;
        DescriptionBox.Text = profile.Description;
        MaxParallelBox.Text = Math.Max(1, profile.MaxParallelStarts).ToString();

        var existing = profile.Services
            .ToDictionary(entry => entry.ServiceName, entry => entry, StringComparer.OrdinalIgnoreCase);

        var allNames = discoveredServiceNames
            .Concat(existing.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var name in allNames)
        {
            existing.TryGetValue(name, out var entry);
            rows.Add(new EditableServiceRow
            {
                ServiceName = name,
                IsIncluded = entry?.Enabled == true || (entry is null && false),
                DependsOnText = entry is null ? string.Empty : string.Join(", ", entry.DependsOn)
            });

            // Seed profiles may list services not yet in discovered list — keep them included.
            if (entry is not null)
            {
                rows[^1].IsIncluded = entry.Enabled;
            }
        }

        // Ensure every persisted member appears even if disabled/not discovered.
        foreach (var entry in profile.Services)
        {
            if (rows.Any(row => string.Equals(row.ServiceName, entry.ServiceName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            rows.Add(new EditableServiceRow
            {
                ServiceName = entry.ServiceName,
                IsIncluded = entry.Enabled,
                DependsOnText = string.Join(", ", entry.DependsOn)
            });
        }

        ServicesGrid.ItemsSource = rows;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show(this, "Profile name is required.", "Runtime Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(MaxParallelBox.Text.Trim(), out var maxParallel) || maxParallel < 1)
        {
            maxParallel = 2;
        }

        var services = rows
            .Where(row => row.IsIncluded)
            .Select(row => new RuntimeProfileServiceEntry
            {
                ServiceName = row.ServiceName.Trim(),
                Enabled = true,
                DependsOn = ParseDependsOn(row.DependsOnText)
            })
            .ToList();

        Result = new RuntimeProfileItem
        {
            Id = source.Id,
            Name = NameBox.Text.Trim(),
            Description = DescriptionBox.Text.Trim(),
            Enabled = true,
            MaxParallelStarts = maxParallel,
            Services = services,
            ServiceCount = services.Count
        };

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static List<string> ParseDependsOn(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class EditableServiceRow
    {
        public string ServiceName { get; set; } = string.Empty;

        public bool IsIncluded { get; set; }

        public string DependsOnText { get; set; } = string.Empty;
    }
}
