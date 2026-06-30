using System.Diagnostics;
using DesktopGarden.Core;

namespace DesktopGarden;

internal sealed class GardenApplicationContext : ApplicationContext
{
    private static readonly string[] ReminderMessages =
    [
        "记得起来活动一下，肩膀放松一点。",
        "要不要去一趟洗手间，顺便接杯水。",
        "今天也在稳稳推进，别忘了照顾自己。",
        "外卖想好了吗，按时吃饭比赶进度更重要。",
        "屏幕看久了，眨眨眼休息十秒。",
        "先伸个懒腰，我们继续往前做。",
        "任务很多也没关系，一点点做完就很棒。",
        "如果卡住了，先深呼吸一下再看。",
        "辛苦了，给自己一个小小的暂停。",
        "今天要努力，也要记得温柔一点。"
    ];

    private readonly AssetCatalog _catalog;
    private readonly ImageCache _images = new();
    private readonly JsonStateStore _store;
    private readonly Stopwatch _runtime = Stopwatch.StartNew();
    private readonly System.Windows.Forms.Timer _saveTimer;
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _quickActionsMenu;
    private readonly PlantInfoForm _plantInfo = new();
    private readonly ReminderBubbleForm _reminderBubble = new();
    private readonly Random _random = new();
    private GardenState _state;
    private GardenForm _garden;
    private ToolStripMenuItem _visibilityItem = null!;
    private ToolStripMenuItem _lockItem = null!;
    private PotInspectorForm? _inspector;
    private bool _exiting;

    public GardenApplicationContext()
    {
        _catalog = new AssetCatalog(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets"));
        var statePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LovelyPlants", "state.json");
        _store = new JsonStateStore(statePath);
        _state = _store.Load() ?? CreateDefaultState();
        _catalog.RepairState(_state);
        GardenStateFactory.Normalize(_state);

        _garden = CreateGardenForm();
        MainForm = _garden;

        _quickActionsMenu = BuildQuickActionsMenu();
        _tray = new NotifyIcon
        {
            Text = "Lovely Plants",
            Icon = LoadApplicationIcon(),
            Visible = true,
            ContextMenuStrip = _quickActionsMenu
        };
        _tray.DoubleClick += (_, _) => ToggleGarden();

        _saveTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _saveTimer.Tick += (_, _) => SaveNow();
        _saveTimer.Start();

        if (_state.Settings.GardenVisible && _state.Pots.Count > 0)
        {
            _garden.Show();
            _garden.RenderNow();
        }

        UpdateMenuText();
    }

    protected override void ExitThreadCore()
    {
        if (_exiting)
        {
            base.ExitThreadCore();
            return;
        }

        _exiting = true;
        SaveNow();
        _saveTimer.Stop();
        _saveTimer.Dispose();
        _quickActionsMenu.Dispose();
        _inspector?.Dispose();
        _plantInfo.Dispose();
        _reminderBubble.Dispose();
        _garden.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _images.Dispose();
        base.ExitThreadCore();
    }

    private GardenForm CreateGardenForm()
    {
        var form = new GardenForm(_state, _catalog, _images);
        form.InspectorRequested += ShowInspector;
        form.HoverChanged += UpdateHover;
        form.StateChanged += SaveNow;
        form.ToggleRequested += ToggleGarden;
        form.ReminderDue += ShowRandomReminder;
        form.PomodoroConfigureRequested += OpenPomodoroSettings;
        form.PomodoroCompleted += ShowPomodoroReminder;
        form.QuickMenuRequested += ShowQuickActionsMenu;
        return form;
    }

    private ContextMenuStrip BuildQuickActionsMenu()
    {
        var menu = new ContextMenuStrip();
        FluentTheme.StyleMenu(menu);

        var add = new ToolStripMenuItem("添加花盆", null, (_, _) => AddPot());
        var spacing = new ToolStripMenuItem("调整间距", null, (_, _) => OpenSpacing());
        var catalog = new ToolStripMenuItem("植物图鉴", null, (_, _) => OpenCatalog());
        _visibilityItem = new ToolStripMenuItem("隐藏花园", null, (_, _) => ToggleGarden());
        _lockItem = new ToolStripMenuItem("锁定交互", null, (_, _) => ToggleLock());
        var settings = new ToolStripMenuItem("设置", null, (_, _) => OpenSettings());
        var exit = new ToolStripMenuItem("退出", null, (_, _) => ExitThread());

        menu.Items.AddRange([add, spacing, catalog, _visibilityItem, _lockItem, new ToolStripSeparator(), settings, new ToolStripSeparator(), exit]);
        foreach (ToolStripItem item in menu.Items)
        {
            item.Padding = new Padding(12, 8, 12, 8);
        }

        return menu;
    }

    private void ShowQuickActionsMenu(Rectangle anchor)
    {
        HideTransientUi();
        UpdateMenuText();
        var x = anchor.Left;
        var y = Math.Max(0, anchor.Top - _quickActionsMenu.GetPreferredSize(Size.Empty).Height - 8);
        _quickActionsMenu.Show(x, y);
    }

    private void ShowInspector(PotInstance pot, Rectangle anchor)
    {
        _plantInfo.Hide();
        _reminderBubble.Hide();
        _inspector?.Close();
        var inspector = new PotInspectorForm(pot, _catalog, anchor);
        _inspector = inspector;

        inspector.WaterRequested += item =>
        {
            _garden.StartAnimation(item.Id, PotAnimationKind.Water);
            PlayFeedbackSound();
        };
        inspector.PlantChangeRequested += item =>
        {
            if (PickAsset("选择植物", _catalog.PlantIds, id => _catalog.PlantPath(id, 3), FirstPlantIdOr(item.PlantId), _catalog.PlantName, id =>
                {
                    if (string.Equals(item.PlantId, id, StringComparison.OrdinalIgnoreCase)) return;
                    item.PlantId = id;
                    item.ElapsedRunSeconds = 0;
                }))
            {
                inspector.RefreshState();
            }
        };
        inspector.PotChangeRequested += item =>
        {
            if (PickAsset("选择花盆", _catalog.PotIds, _catalog.PotPath, item.PotId, _catalog.PotName, id => item.PotId = id))
            {
                inspector.RefreshState();
            }
        };
        inspector.ExpressionChangeRequested += item =>
        {
            if (PickAsset("选择表情", _catalog.ExpressionIds, _catalog.ExpressionPath, item.ExpressionId, _catalog.ExpressionName, id => item.ExpressionId = id))
            {
                inspector.RefreshState();
            }
        };
        inspector.ScaleChanged += (item, scale) =>
        {
            item.Scale = scale;
            _garden.RenderNow();
        };
        inspector.RemoveRequested += item =>
        {
            inspector.Close();
            DeletePot(item);
        };
        inspector.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_inspector, inspector)) _inspector = null;
            SaveNow();
        };
        inspector.Show(_garden);
    }

    private void UpdateHover(PotInstance? pot, Rectangle? anchor)
    {
        if (pot is null || anchor is null || _inspector is not null)
        {
            _plantInfo.Hide();
            return;
        }

        _plantInfo.UpdatePot(pot, _catalog);
        _plantInfo.ShowNear(anchor.Value, _garden);
    }

    private bool PickAsset(
        string title,
        IReadOnlyList<string> ids,
        Func<string, string> pathForId,
        string selected,
        Func<string, string> displayName,
        Action<string> apply)
    {
        using var picker = new AssetPickerForm(title, ids, pathForId, selected, displayName);
        var result = _inspector is { IsDisposed: false } ? picker.ShowDialog(_inspector) : picker.ShowDialog();
        if (result == DialogResult.OK && picker.SelectedId is { } id)
        {
            apply(id);
            _garden.RenderNow();
            SaveNow();
            return true;
        }

        return false;
    }

    private void AddPot()
    {
        if (_state.Pots.Count >= _garden.Capacity)
        {
            _tray.ShowBalloonTip(2500, "花园已经放满", "当前屏幕宽度无法再放置新的花盆。", ToolTipIcon.Info);
            return;
        }

        var index = _state.Pots.Count;
        var pot = new PotInstance
        {
            PlantId = string.Empty,
            PotId = _catalog.PotIds[index % _catalog.PotIds.Count],
            ExpressionId = _catalog.ExpressionIds[index % _catalog.ExpressionIds.Count],
            SortOrder = index,
            Scale = 0.97f
        };
        _state.Pots.Add(pot);
        _state.Settings.GardenVisible = true;
        if (!_garden.Visible) _garden.Show();
        _garden.RenderNow();

        PickAsset("选择植物", _catalog.PlantIds, id => _catalog.PlantPath(id, 3), FirstPlantIdOr(string.Empty), _catalog.PlantName, id =>
        {
            pot.PlantId = id;
            pot.ElapsedRunSeconds = 0;
        });

        SaveNow();
        UpdateMenuText();
    }

    private void DeletePot(PotInstance pot)
    {
        if (MessageBox.Show("确定移除这盆植物吗？成长时间也会一起删除。", "移除花盆", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
        {
            return;
        }

        _state.Pots.RemoveAll(item => item.Id == pot.Id);
        GardenStateFactory.Normalize(_state);
        if (_state.Pots.Count == 0)
        {
            _state.Settings.GardenVisible = false;
            _garden.Hide();
        }
        else
        {
            _garden.RenderNow();
        }

        SaveNow();
        UpdateMenuText();
    }

    private void ToggleGarden()
    {
        _state.Settings.GardenVisible = !_garden.Visible;
        if (_state.Settings.GardenVisible && _state.Pots.Count > 0)
        {
            _garden.Show();
            _garden.RenderNow();
        }
        else
        {
            HideTransientUi();
            _garden.Hide();
        }

        SaveNow();
        UpdateMenuText();
    }

    private void ToggleLock()
    {
        _state.Settings.InteractionLocked = !_state.Settings.InteractionLocked;
        SaveNow();
        UpdateMenuText();
        _tray.ShowBalloonTip(1800, "Lovely Plants", _state.Settings.InteractionLocked ? "花园已锁定，鼠标会穿过植物。" : "花园已解锁，可以再次互动。", ToolTipIcon.None);
    }

    private void OpenSettings()
    {
        HideTransientUi();
        using var settings = new SettingsForm(_state.Settings);
        var originalSettings = CloneSettings(_state.Settings);
        var reset = false;
        settings.PreviewChanged += preview =>
        {
            _state.Settings.MonitorIndex = preview.MonitorIndex;
            _state.Settings.Scale = preview.Scale;
            _state.Settings.GapScale = preview.GapScale;
            _state.Settings.AlwaysOnTop = preview.AlwaysOnTop;
            _state.Settings.InteractionLocked = preview.InteractionLocked;
            _state.Settings.ShowGrassBackground = preview.ShowGrassBackground;
            _state.Settings.SoundEnabled = preview.SoundEnabled;
            _state.Settings.StartWithWindows = preview.StartWithWindows;
            _garden.RenderNow();
        };
        settings.ResetRequested += () =>
        {
            if (MessageBox.Show("这会删除所有成长时间，并恢复三盆初始植物。", "重置花园", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                reset = true;
                settings.DialogResult = DialogResult.Cancel;
                settings.Close();
            }
        };

        var result = settings.ShowDialog();
        if (reset)
        {
            ResetGarden();
            return;
        }
        if (result != DialogResult.OK)
        {
            RestoreSettings(originalSettings);
            _garden.RenderNow();
            UpdateMenuText();
            return;
        }

        settings.ApplyTo(_state.Settings);
        StartupManager.SetEnabled(_state.Settings.StartWithWindows);
        _garden.RenderNow();
        SaveNow();
        UpdateMenuText();
    }

    private void OpenSpacing()
    {
        HideTransientUi();
        using var spacing = new SpacingForm(_state.Settings.GapScale);
        if (spacing.ShowDialog() != DialogResult.OK) return;

        _state.Settings.GapScale = spacing.SelectedGapScale;
        _garden.RenderNow();
        SaveNow();
    }

    private void OpenCatalog()
    {
        HideTransientUi();
        using var catalog = new PlantCatalogForm(_catalog);
        catalog.ShowDialog();
    }

    private void OpenPomodoroSettings()
    {
        _inspector?.Close();
        _plantInfo.Hide();
        using var timer = new PomodoroForm();
        timer.SetDuration(_garden.CurrentPomodoroDuration);
        var result = _garden.Visible ? timer.ShowDialog(_garden) : timer.ShowDialog();
        if (result != DialogResult.OK) return;
        _garden.SetPomodoro(timer.SelectedDuration);
    }

    private void ResetGarden()
    {
        HideTransientUi();
        var defaults = CreateDefaultState();
        _state.Pots = defaults.Pots;
        _state.Settings = defaults.Settings;
        _garden.Dispose();
        _garden = CreateGardenForm();
        MainForm = _garden;
        _garden.Show();
        _garden.RenderNow();
        SaveNow();
        UpdateMenuText();
    }

    private GardenState CreateDefaultState() => GardenStateFactory.CreateDefault(_catalog.PlantIds, _catalog.PotIds, _catalog.ExpressionIds);

    private void RestoreSettings(AppSettings snapshot)
    {
        _state.Settings.MonitorIndex = snapshot.MonitorIndex;
        _state.Settings.Scale = snapshot.Scale;
        _state.Settings.GapScale = snapshot.GapScale;
        _state.Settings.AlwaysOnTop = snapshot.AlwaysOnTop;
        _state.Settings.InteractionLocked = snapshot.InteractionLocked;
        _state.Settings.ShowGrassBackground = snapshot.ShowGrassBackground;
        _state.Settings.SoundEnabled = snapshot.SoundEnabled;
        _state.Settings.StartWithWindows = snapshot.StartWithWindows;
        _state.Settings.GardenVisible = snapshot.GardenVisible;
        _state.Settings.GardenOffsetX = snapshot.GardenOffsetX;
        _state.Settings.GardenOffsetY = snapshot.GardenOffsetY;
    }

    private static AppSettings CloneSettings(AppSettings source) => new()
    {
        MonitorIndex = source.MonitorIndex,
        Scale = source.Scale,
        GapScale = source.GapScale,
        AlwaysOnTop = source.AlwaysOnTop,
        InteractionLocked = source.InteractionLocked,
        ShowGrassBackground = source.ShowGrassBackground,
        SoundEnabled = source.SoundEnabled,
        StartWithWindows = source.StartWithWindows,
        GardenVisible = source.GardenVisible,
        GardenOffsetX = source.GardenOffsetX,
        GardenOffsetY = source.GardenOffsetY
    };

    private void SaveNow()
    {
        GrowthPolicy.Accumulate(_state, _runtime.Elapsed);
        _runtime.Restart();
        _store.Save(_state);
    }

    private void UpdateMenuText()
    {
        _visibilityItem.Text = _garden.Visible ? "隐藏花园" : "显示花园";
        _lockItem.Text = _state.Settings.InteractionLocked ? "解锁交互" : "锁定交互";
    }

    private void ShowRandomReminder()
    {
        if (!_garden.Visible || _state.Pots.Count == 0) return;
        var anchor = _garden.AnchorForRandomPot();
        if (anchor is null) return;
        var message = ReminderMessages[_random.Next(ReminderMessages.Length)];
        _reminderBubble.ShowReminder(message, anchor.Value, _garden, 0);
        PlayFeedbackSound();
    }

    private void ShowPomodoroReminder()
    {
        if (!_garden.Visible || _state.Pots.Count == 0) return;
        var anchor = _garden.AnchorForRandomPot();
        if (anchor is null) return;
        _reminderBubble.ShowReminder("番茄钟结束了，休息一下再继续。", anchor.Value, _garden, 3000);
        PlayFeedbackSound();
    }

    private void HideTransientUi()
    {
        _inspector?.Close();
        _plantInfo.Hide();
        _reminderBubble.Hide();
    }

    private void PlayFeedbackSound()
    {
        if (_state.Settings.SoundEnabled) System.Media.SystemSounds.Asterisk.Play();
    }

    private string FirstPlantIdOr(string fallback) => _catalog.PlantIds.Count > 0 ? _catalog.PlantIds[0] : fallback;

    private static Icon LoadApplicationIcon()
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "lovely-plants.ico");
        return File.Exists(path) ? new Icon(path) : SystemIcons.Application;
    }
}
