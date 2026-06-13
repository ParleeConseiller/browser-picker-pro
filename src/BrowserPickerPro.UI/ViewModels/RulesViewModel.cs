// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace BrowserPickerPro.UI.ViewModels;

public partial class RulesViewModel : ObservableObject
{
    private readonly IRulesEngine _rulesEngine;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<RulesViewModel> _logger;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private RuleEditorViewModel? _editingRule;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<RuleEntryViewModel> Rules { get; } = new();
    public ObservableCollection<Browser> AvailableBrowsers { get; } = new();

    public bool IsRulesEmpty => Rules.Count == 0;

    public IEnumerable<RuleType> RuleTypes => Enum.GetValues<RuleType>();
    public IEnumerable<RuleAction> RuleActions => Enum.GetValues<RuleAction>();

    public RulesViewModel(IRulesEngine rulesEngine, ISettingsService settingsService, ILogger<RulesViewModel> logger)
    {
        _rulesEngine = rulesEngine;
        _settingsService = settingsService;
        _logger = logger;
        Rules.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsRulesEmpty));
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var rules = await _rulesEngine.LoadRulesAsync();
            var browsers = await _settingsService.LoadBrowsersAsync();

            Rules.Clear();
            foreach (var rule in rules.OrderBy(r => r.Priority))
                Rules.Add(new RuleEntryViewModel(rule));

            AvailableBrowsers.Clear();
            foreach (var b in browsers.Where(b => b.IsEnabled))
                AvailableBrowsers.Add(b);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load rules");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddRule()
    {
        var rule = new BrowserRule { Name = "New Rule", Priority = Rules.Count * 10 + 10 };
        EditingRule = new RuleEditorViewModel(rule, AvailableBrowsers.ToList());
        IsEditing = true;
    }

    [RelayCommand]
    private void EditRule(RuleEntryViewModel entry)
    {
        EditingRule = new RuleEditorViewModel(entry.Rule, AvailableBrowsers.ToList());
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveRuleAsync()
    {
        if (EditingRule == null) return;
        var rule = EditingRule.ToRule();

        var existing = Rules.FirstOrDefault(r => r.Rule.Id == rule.Id);
        if (existing != null) Rules.Remove(existing);

        Rules.Add(new RuleEntryViewModel(rule));
        await PersistRulesAsync();
        IsEditing = false;
        EditingRule = null;
        StatusMessage = "Rule saved";
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditingRule = null;
    }

    [RelayCommand]
    private async Task DeleteRuleAsync(RuleEntryViewModel entry)
    {
        Rules.Remove(entry);
        await PersistRulesAsync();
        StatusMessage = "Rule deleted";
    }

    private Task PersistRulesAsync()
        => _rulesEngine.SaveRulesAsync(Rules.Select(r => r.Rule));
}

public partial class RuleEntryViewModel : ObservableObject
{
    public BrowserRule Rule { get; }

    [ObservableProperty] private bool _isEnabled;

    public string Name => Rule.Name;
    public string Pattern => Rule.Pattern;
    public string RuleTypeLabel => Rule.RuleType.ToString();
    public string BrowserLabel => Rule.TargetBrowserId?.Length > 0 ? "Custom" : "Ask";

    public RuleEntryViewModel(BrowserRule rule)
    {
        Rule = rule;
        _isEnabled = rule.IsEnabled;
    }

    partial void OnIsEnabledChanged(bool value) => Rule.IsEnabled = value;
}

public partial class RuleEditorViewModel : ObservableObject
{
    private readonly BrowserRule _rule;

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _pattern;
    [ObservableProperty] private RuleType _ruleType;
    [ObservableProperty] private RuleAction _action;
    [ObservableProperty] private string? _targetBrowserId;
    [ObservableProperty] private int _priority;
    [ObservableProperty] private string? _description;

    public List<Browser> AvailableBrowsers { get; }

    public RuleEditorViewModel(BrowserRule rule, List<Browser> browsers)
    {
        _rule = rule;
        _name = rule.Name;
        _pattern = rule.Pattern;
        _ruleType = rule.RuleType;
        _action = rule.Action;
        _targetBrowserId = rule.TargetBrowserId;
        _priority = rule.Priority;
        _description = rule.Description;
        AvailableBrowsers = browsers;
    }

    public BrowserRule ToRule()
    {
        _rule.Name = Name;
        _rule.Pattern = Pattern;
        _rule.RuleType = RuleType;
        _rule.Action = Action;
        _rule.TargetBrowserId = TargetBrowserId;
        _rule.Priority = Priority;
        _rule.Description = Description;
        return _rule;
    }
}
