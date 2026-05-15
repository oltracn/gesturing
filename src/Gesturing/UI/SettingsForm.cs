using System.Reflection;
using Gesturing.Models;
using Gesturing.Services;

namespace Gesturing.UI;

public partial class SettingsForm : Form
{
    private readonly AppConfig _config;
    private readonly Action? _onConfigChanged;
    private readonly List<string> _currentSequence = new(4);

    // ── Navigation ──
    private Panel _sidebar = null!;
    private Label _rulesNavItem = null!;
    private Label _scopesNavItem = null!;
    private Panel _rulesIndicator = null!;
    private Panel _scopesIndicator = null!;

    // ── Rules Tab ──
    private Panel _rulesPanel = null!;
    private Label _sequenceDisplay = null!;
    private Label _statusLabel = null!;
    private HotkeyTextBox _hotkeyBox = null!;
    private Panel _activeRulesContainer = null!;
    private Label _ruleCountLabel = null!;
    private Label _emptyRulesHint = null!;

    // ── Scopes Tab ──
    private Panel _scopesPanel = null!;
    private StyledTextBox _processInput = null!;
    private TableLayoutPanel _processGrid = null!;
    private Label _scopeCountLabel = null!;
    private Label _emptyScopesHint = null!;

    private static readonly Dictionary<string, string> ArrowMap = new()
    {
        ["U"] = "↑", ["D"] = "↓", ["L"] = "←", ["R"] = "→"
    };

    // ── Typography ──
    private static readonly Font NavFont = new("Segoe UI Semibold", 11f);
    private static readonly Font TitleFont = new("Segoe UI Semibold", 15f);
    private static readonly Font SubtitleFont = new("Segoe UI", 9.5f);
    private static readonly Font BodyFont = new("Segoe UI", 10.5f);
    private static readonly Font DirFont = new("Segoe UI", 20f);
    private static readonly Font SeqFont = new("Segoe UI Semibold", 14f);
    private static readonly Font PillFont = new("Segoe UI Semibold", 10.5f);
    private static readonly Font BadgeFont = new("Segoe UI", 9f);
    private static readonly Font DeleteFont = new("Segoe UI", 14f);
    private static readonly Font ArrowFont = new("Segoe UI", 11f);
    private static readonly Font SectionFont = new("Segoe UI Semibold", 12f);
    private static readonly Font LogoFont = new("Segoe UI Semibold", 13f);

    // ── Palette: Dark gray sidebar + blue accent ──
    private static Color SidebarBg => Color.FromArgb(56, 56, 56);       // #383838
    private static Color SidebarText => Color.FromArgb(160, 160, 168);
    private static Color SidebarActive => Color.FromArgb(240, 240, 242);
    private static Color Accent => Color.FromArgb(42, 137, 190);        // #2a89be
    private static Color AccentHover => Color.FromArgb(52, 157, 210);
    private static Color AccentPressed => Color.FromArgb(32, 117, 170);
    private static Color SurfaceBg => Color.FromArgb(250, 250, 252);
    private static Color CardBg => Color.FromArgb(242, 242, 244);
    private static Color DarkText => Color.FromArgb(28, 28, 32);
    private static Color MutedText => Color.FromArgb(120, 120, 128);
    private static Color BorderColor => Color.FromArgb(220, 220, 224);
    private static Color Danger => Color.FromArgb(220, 56, 56);
    private static Color DirBtnBg => Color.FromArgb(232, 232, 236);
    private static Color DirBtnHover => Color.FromArgb(212, 212, 216);
    private static Color PillBg => Color.FromArgb(228, 228, 232);

    public SettingsForm(AppConfig config, Action? onConfigChanged = null)
    {
        _config = config;
        _onConfigChanged = onConfigChanged;

        Text = "Gesturing";
        Icon = LoadAppIcon();
        Size = new Size(1400, 860);
        MinimumSize = new Size(1100, 640);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = SurfaceBg;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = BodyFont;

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceBg
        };
        Controls.Add(content);

        _sidebar = BuildSidebar();
        Controls.Add(_sidebar);

        _rulesPanel = BuildRulesPanel();
        _scopesPanel = BuildScopesPanel();
        content.Controls.Add(_rulesPanel);
        content.Controls.Add(_scopesPanel);

        ShowRulesTab();
        RefreshRuleList();
        RefreshProcessList();
    }

    // ═══════ Sidebar Navigation ═══════

    private Panel BuildSidebar()
    {
        var side = new Panel
        {
            Dock = DockStyle.Left,
            Width = 200,
            BackColor = SidebarBg
        };

        // ── Logo area ──
        var logo = new Label
        {
            Text = "GESTURING",
            Font = LogoFont,
            ForeColor = Accent,
            AutoSize = true,
            Location = new Point(20, 18)
        };
        side.Controls.Add(logo);

        var logoLine = new Panel
        {
            Height = 1,
            Width = 160,
            BackColor = Color.FromArgb(50, 50, 56),
            Location = new Point(20, 44)
        };
        side.Controls.Add(logoLine);

        // ── Nav items ──
        _rulesNavItem = new Label
        {
            Text = "   Rules",
            Font = NavFont,
            ForeColor = SidebarActive,
            AutoSize = true,
            Cursor = Cursors.Hand,
            Location = new Point(0, 56),
            Size = new Size(200, 40),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _rulesNavItem.Click += (_, _) => ShowRulesTab();
        side.Controls.Add(_rulesNavItem);

        _rulesIndicator = new Panel
        {
            Height = 40,
            Width = 3,
            BackColor = Accent,
            Location = new Point(0, 56)
        };
        side.Controls.Add(_rulesIndicator);

        _scopesNavItem = new Label
        {
            Text = "   App Scopes",
            Font = NavFont,
            ForeColor = SidebarText,
            AutoSize = true,
            Cursor = Cursors.Hand,
            Location = new Point(0, 96),
            Size = new Size(200, 40),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _scopesNavItem.Click += (_, _) => ShowScopesTab();
        side.Controls.Add(_scopesNavItem);

        _scopesIndicator = new Panel
        {
            Height = 40,
            Width = 3,
            BackColor = Color.Transparent,
            Location = new Point(0, 96)
        };
        side.Controls.Add(_scopesIndicator);

        // ── Bottom info ──
        var version = new Label
        {
            Text = "v1.0",
            Font = BadgeFont,
            ForeColor = Color.FromArgb(80, 80, 88),
            AutoSize = true,
            Location = new Point(20, side.Height - 30)
        };
        side.Controls.Add(version);
        side.Resize += (_, _) => version.Location = new Point(20, side.Height - 30);

        return side;
    }

    private void ShowRulesTab()
    {
        _rulesPanel.Visible = true;
        _scopesPanel.Visible = false;
        _rulesPanel.BringToFront();

        _rulesNavItem.ForeColor = SidebarActive;
        _rulesIndicator.BackColor = Accent;
        _scopesNavItem.ForeColor = SidebarText;
        _scopesIndicator.BackColor = Color.Transparent;
    }

    private void ShowScopesTab()
    {
        _rulesPanel.Visible = false;
        _scopesPanel.Visible = true;
        _scopesPanel.BringToFront();

        _scopesNavItem.ForeColor = SidebarActive;
        _scopesIndicator.BackColor = Accent;
        _rulesNavItem.ForeColor = SidebarText;
        _rulesIndicator.BackColor = Color.Transparent;
    }

    // ═══════ Rules Tab ═══════

    private Panel BuildRulesPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceBg };

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = SurfaceBg,
            Padding = new Padding(28, 20, 28, 20)
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));

        split.Controls.Add(BuildRulesLeftPanel(), 0, 0);
        split.Controls.Add(BuildRulesRightPanel(), 1, 0);

        panel.Controls.Add(split);
        return panel;
    }

    private Panel BuildRulesLeftPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceBg,
            Padding = new Padding(0, 0, 16, 0)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            BackColor = SurfaceBg
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // ── Title ──
        layout.Controls.Add(new Label
        {
            Text = "New Gesture Rule",
            Font = TitleFont,
            ForeColor = DarkText,
            Dock = DockStyle.Fill
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text = "Record Sequence",
            Font = SubtitleFont,
            ForeColor = MutedText,
            Dock = DockStyle.Fill
        }, 0, 1);

        // ── Direction pad ──
        var dirWrapper = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceBg };
        var dirLayout = BuildDirectionLayout();
        dirWrapper.Controls.Add(dirLayout);
        CenterControl(dirWrapper, dirLayout);
        dirWrapper.Resize += (_, _) => CenterControl(dirWrapper, dirLayout);
        layout.Controls.Add(dirWrapper, 0, 2);

        // ── Current Sequence ──
        layout.Controls.Add(new Label
        {
            Text = "Current Sequence",
            Font = SubtitleFont,
            ForeColor = MutedText,
            Dock = DockStyle.Fill
        }, 0, 3);

        var seqPanel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceBg };
        _sequenceDisplay = new Label
        {
            Text = "",
            Font = SeqFont,
            ForeColor = DarkText,
            AutoSize = true,
            BackColor = PillBg,
            Padding = new Padding(10, 6, 10, 6),
            Location = new Point(0, 10)
        };
        seqPanel.Controls.Add(_sequenceDisplay);
        layout.Controls.Add(seqPanel, 0, 4);

        // ── Map to Shortcut ──
        layout.Controls.Add(new Label
        {
            Text = "Map to Shortcut",
            Font = SubtitleFont,
            ForeColor = MutedText,
            Dock = DockStyle.Fill
        }, 0, 5);

        var inputRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = SurfaceBg
        };
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));

        _hotkeyBox = new HotkeyTextBox
        {
            Dock = DockStyle.Fill,
            Font = BodyFont,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 2, 8, 2)
        };
        inputRow.Controls.Add(_hotkeyBox, 0, 0);

        var addBtn = new Button
        {
            Text = "+  Add Rule",
            Dock = DockStyle.Fill,
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = PillFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 0, 2)
        };
        addBtn.FlatAppearance.BorderSize = 0;
        addBtn.FlatAppearance.MouseOverBackColor = AccentHover;
        addBtn.FlatAppearance.MouseDownBackColor = AccentPressed;
        addBtn.Click += AddRule_Click;
        inputRow.Controls.Add(addBtn, 1, 0);

        layout.Controls.Add(inputRow, 0, 6);

        _statusLabel = new Label
        {
            Text = "",
            ForeColor = Danger,
            Font = SubtitleFont,
            Dock = DockStyle.Fill
        };
        layout.Controls.Add(_statusLabel, 0, 7);

        panel.Controls.Add(layout);
        return panel;
    }

    private static void CenterControl(Panel parent, Control child)
    {
        child.Location = new Point(
            (parent.ClientSize.Width - child.Width) / 2,
            (parent.ClientSize.Height - child.Height) / 2);
    }

    private Panel BuildDirectionLayout()
    {
        var panel = new Panel { Width = 186, Height = 186, BackColor = SurfaceBg };

        var upBtn = CreateDirButton("↑", "U");
        upBtn.Location = new Point(66, 0);
        panel.Controls.Add(upBtn);

        var leftBtn = CreateDirButton("←", "L");
        leftBtn.Location = new Point(0, 66);
        panel.Controls.Add(leftBtn);

        var centerDot = new Panel
        {
            Size = new Size(12, 12),
            BackColor = Accent,
            Location = new Point(87, 87)
        };
        panel.Controls.Add(centerDot);

        var rightBtn = CreateDirButton("→", "R");
        rightBtn.Location = new Point(132, 66);
        panel.Controls.Add(rightBtn);

        var downBtn = CreateDirButton("↓", "D");
        downBtn.Location = new Point(66, 132);
        panel.Controls.Add(downBtn);

        return panel;
    }

    private Button CreateDirButton(string text, string dir)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(54, 54),
            Font = DirFont,
            Tag = dir,
            FlatStyle = FlatStyle.Flat,
            BackColor = DirBtnBg,
            ForeColor = DarkText,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = BorderColor;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = DirBtnHover;
        btn.Click += DirectionButton_Click;
        return btn;
    }

    private Panel BuildRulesRightPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceBg,
            Padding = new Padding(16, 0, 0, 0)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = SurfaceBg
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // ── Header ──
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = SurfaceBg,
            WrapContents = false
        };
        var title = new Label
        {
            Text = "Active Rules",
            Font = TitleFont,
            ForeColor = DarkText,
            AutoSize = true,
            Margin = new Padding(0, 0, 10, 0)
        };
        header.Controls.Add(title);

        _ruleCountLabel = new Label
        {
            Text = "0",
            Font = BadgeFont,
            ForeColor = Color.White,
            BackColor = Accent,
            AutoSize = true,
            Padding = new Padding(6, 2, 6, 2),
            Margin = new Padding(0, 4, 0, 0)
        };
        header.Controls.Add(_ruleCountLabel);
        layout.Controls.Add(header, 0, 0);

        // ── Cards container ──
        _activeRulesContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceBg,
            AutoScroll = true
        };

        _emptyRulesHint = new Label
        {
            Text = "No rules yet. Create one on the left.",
            Font = SubtitleFont,
            ForeColor = MutedText,
            AutoSize = true,
            Location = new Point(20, 20),
            Visible = true
        };
        _activeRulesContainer.Controls.Add(_emptyRulesHint);

        layout.Controls.Add(_activeRulesContainer, 0, 1);

        panel.Controls.Add(layout);
        return panel;
    }

    // ═══════ Scopes Tab ═══════

    private Panel BuildScopesPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceBg };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            BackColor = SurfaceBg,
            Padding = new Padding(28, 20, 28, 20)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        // ── Title ──
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = SurfaceBg,
            WrapContents = false
        };
        var title = new Label
        {
            Text = "App Scopes",
            Font = TitleFont,
            ForeColor = DarkText,
            AutoSize = true,
            Margin = new Padding(0, 0, 10, 0)
        };
        header.Controls.Add(title);

        _scopeCountLabel = new Label
        {
            Text = "0",
            Font = BadgeFont,
            ForeColor = Color.White,
            BackColor = Accent,
            AutoSize = true,
            Padding = new Padding(6, 2, 6, 2),
            Margin = new Padding(0, 4, 0, 0)
        };
        header.Controls.Add(_scopeCountLabel);
        layout.Controls.Add(header, 0, 0);

        // ── Add Application ──
        layout.Controls.Add(BuildAddAppSection(), 0, 1);

        // ── Section label ──
        layout.Controls.Add(new Label
        {
            Text = "Enabled Applications",
            Font = SectionFont,
            ForeColor = DarkText,
            Dock = DockStyle.Fill
        }, 0, 2);

        // ── Process grid ──
        _processGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = SurfaceBg
        };
        _processGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _processGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _emptyScopesHint = new Label
        {
            Text = "No scopes defined — rules apply globally.",
            Font = SubtitleFont,
            ForeColor = MutedText,
            AutoSize = true,
            Location = new Point(20, 20),
            Visible = true
        };
        _processGrid.Controls.Add(_emptyScopesHint, 0, 0);
        layout.Controls.Add(_processGrid, 0, 3);

        // ── Footer note ──
        var notePanel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceBg };
        var note = new Label
        {
            Text = "Gestures only activate when these apps are in foreground. No scopes = global.",
            Font = BadgeFont,
            ForeColor = MutedText,
            AutoSize = true,
            Location = new Point(0, 6)
        };
        notePanel.Controls.Add(note);
        layout.Controls.Add(notePanel, 0, 4);

        panel.Controls.Add(layout);
        return panel;
    }

    private Panel BuildAddAppSection()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Padding = new Padding(16, 12, 16, 12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = CardBg
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        var addAppLabel = new Label
        {
            Text = "Add Application",
            Font = SubtitleFont,
            ForeColor = MutedText,
            Dock = DockStyle.Fill
        };
        layout.Controls.Add(addAppLabel, 0, 0);
        layout.SetColumnSpan(addAppLabel, 2);

        _processInput = new StyledTextBox
        {
            Dock = DockStyle.Fill,
            Font = BodyFont,
            PlaceholderText = "e.g., code.exe",
            Margin = new Padding(0, 4, 8, 4)
        };
        layout.Controls.Add(_processInput, 0, 1);

        var addBtn = new Button
        {
            Text = "+  Add",
            Dock = DockStyle.Fill,
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = PillFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 0, 4)
        };
        addBtn.FlatAppearance.BorderSize = 0;
        addBtn.FlatAppearance.MouseOverBackColor = AccentHover;
        addBtn.FlatAppearance.MouseDownBackColor = AccentPressed;
        addBtn.Click += AddProcess_Click;
        layout.Controls.Add(addBtn, 1, 1);

        panel.Controls.Add(layout);
        return panel;
    }

    // ═══════ Gesture Entry ═══════

    private void DirectionButton_Click(object? sender, EventArgs e)
    {
        _statusLabel.Text = "";

        if (_currentSequence.Count >= 4)
        {
            _statusLabel.Text = "Maximum 4 gestures per sequence";
            return;
        }

        if (sender is Button { Tag: string dir })
        {
            _currentSequence.Add(dir);
            UpdateGestureDisplay();
        }
    }

    private void ClearGesture()
    {
        _currentSequence.Clear();
        _sequenceDisplay.Text = "";
        _hotkeyBox.ResetCapture();
        _statusLabel.Text = "";
    }

    private void UpdateGestureDisplay()
    {
        var display = string.Join("", _currentSequence.Select(d =>
            ArrowMap.TryGetValue(d, out var arrow) ? arrow : d));
        _sequenceDisplay.Text = display;
    }

    private void AddRule_Click(object? sender, EventArgs e)
    {
        _statusLabel.Text = "";

        if (_currentSequence.Count == 0)
        {
            _statusLabel.Text = "Please record a gesture sequence first";
            return;
        }

        if (!_hotkeyBox.HasCapture)
        {
            _statusLabel.Text = "Please capture a shortcut first";
            return;
        }

        var seqKey = string.Join(",", _currentSequence);
        foreach (var existing in _config.Rules)
        {
            if (string.Join(",", existing.GestureSequence).Equals(seqKey, StringComparison.OrdinalIgnoreCase))
            {
                _statusLabel.Text = "This gesture sequence already exists";
                return;
            }
        }

        var rule = new GestureRule
        {
            GestureSequence = new List<string>(_currentSequence),
            Hotkey = new HotkeyDef
            {
                Modifiers = new List<string>(_hotkeyBox.CapturedModifiers),
                Key = _hotkeyBox.CapturedKey
            }
        };

        _config.Rules.Add(rule);
        ConfigManager.Save(_config);
        _onConfigChanged?.Invoke();

        RefreshRuleList();
        ClearGesture();
    }

    // ═══════ Rule List ═══════

    private void RefreshRuleList()
    {
        DisposeControls(_activeRulesContainer);
        _activeRulesContainer.Controls.Clear();

        if (_config.Rules.Count == 0)
        {
            _emptyRulesHint = new Label
            {
                Text = "No rules yet. Create one on the left.",
                Font = SubtitleFont,
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            _activeRulesContainer.Controls.Add(_emptyRulesHint);
        }
        else
        {
            int available = _activeRulesContainer.ClientSize.Width;
            if (available < 100) available = 400;
            int cardWidth = available - SystemInformation.VerticalScrollBarWidth - 8;
            int y = 0;

            foreach (var rule in _config.Rules)
            {
                var card = CreateRuleCard(rule, cardWidth);
                card.Location = new Point(0, y);
                _activeRulesContainer.Controls.Add(card);
                y += card.Height + 8;
            }
        }

        _ruleCountLabel.Text = $"{_config.Rules.Count}";
    }

    private Panel CreateRuleCard(GestureRule rule, int width)
    {
        var card = new Panel
        {
            Width = width,
            Height = 56,
            BackColor = CardBg
        };

        var gestureDisplay = string.Join("", rule.GestureSequence.Select(d =>
            ArrowMap.TryGetValue(d, out var arrow) ? arrow : d));

        string hotkeyDisplay = rule.Hotkey.Modifiers.Count == 0
            ? rule.Hotkey.Key
            : string.Join(" + ", rule.Hotkey.Modifiers) + " + " + rule.Hotkey.Key;

        // ── Gesture pill ──
        var gesturePill = new Label
        {
            Text = gestureDisplay,
            Font = SeqFont,
            ForeColor = DarkText,
            AutoSize = true,
            BackColor = PillBg,
            Padding = new Padding(8, 4, 8, 4),
            Location = new Point(14, 14)
        };
        card.Controls.Add(gesturePill);

        // ── Arrow connector ──
        var arrow = new Label
        {
            Text = "→",
            Font = ArrowFont,
            ForeColor = Accent,
            AutoSize = true,
            Location = new Point(gesturePill.Right + 10, 19)
        };
        card.Controls.Add(arrow);

        // ── Hotkey pill ──
        var hotkeyPill = new Label
        {
            Text = hotkeyDisplay,
            Font = PillFont,
            ForeColor = DarkText,
            AutoSize = true,
            BackColor = PillBg,
            Padding = new Padding(8, 4, 8, 4),
            Location = new Point(arrow.Right + 10, 14)
        };
        card.Controls.Add(hotkeyPill);

        // ── Delete ──
        var deleteBtn = new Label
        {
            Text = "×",
            Font = DeleteFont,
            ForeColor = MutedText,
            AutoSize = true,
            Cursor = Cursors.Hand,
            Location = new Point(card.Width - 36, 18)
        };
        deleteBtn.Click += (_, _) => DeleteRule(rule);
        deleteBtn.MouseEnter += (_, _) => deleteBtn.ForeColor = Danger;
        deleteBtn.MouseLeave += (_, _) => deleteBtn.ForeColor = MutedText;
        card.Controls.Add(deleteBtn);

        return card;
    }

    private void DeleteRule(GestureRule rule)
    {
        _config.Rules.Remove(rule);
        ConfigManager.Save(_config);
        _onConfigChanged?.Invoke();
        RefreshRuleList();
    }

    // ═══════ Scope ═══════

    private void RefreshProcessList()
    {
        DisposeControls(_processGrid);
        _processGrid.Controls.Clear();
        _processGrid.RowCount = 0;
        while (_processGrid.RowStyles.Count > 0)
            _processGrid.RowStyles.RemoveAt(0);

        if (_config.TargetProcesses.Count == 0)
        {
            _processGrid.RowCount = 1;
            _processGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            _emptyScopesHint = new Label
            {
                Text = "No scopes defined — rules apply globally.",
                Font = SubtitleFont,
                ForeColor = MutedText,
                AutoSize = true
            };
            _processGrid.Controls.Add(_emptyScopesHint, 0, 0);
        }
        else
        {
            int row = 0, col = 0;
            foreach (var proc in _config.TargetProcesses)
            {
                if (col == 0)
                {
                    _processGrid.RowCount++;
                    _processGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
                }

                var card = CreateProcessCard(proc);
                _processGrid.Controls.Add(card, col, row);

                col++;
                if (col >= 2) { col = 0; row++; }
            }
        }

        _scopeCountLabel.Text = $"{_config.TargetProcesses.Count}";
    }

    private Panel CreateProcessCard(string proc)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Margin = new Padding(0, 0, 8, 8),
            Padding = new Padding(12, 8, 12, 8)
        };

        var dot = new Panel
        {
            Size = new Size(8, 8),
            BackColor = GetProcessColor(proc),
            Location = new Point(12, 16)
        };
        card.Controls.Add(dot);

        var nameLabel = new Label
        {
            Text = proc,
            Font = PillFont,
            ForeColor = DarkText,
            AutoSize = true,
            Location = new Point(dot.Right + 10, 16)
        };
        card.Controls.Add(nameLabel);

        var deleteBtn = new Label
        {
            Text = "×",
            Font = DeleteFont,
            ForeColor = MutedText,
            AutoSize = true,
            Cursor = Cursors.Hand,
            Location = new Point(card.Width - 36, 15)
        };
        card.Resize += (_, _) => deleteBtn.Location = new Point(card.Width - 36, 15);
        deleteBtn.Click += (_, _) =>
        {
            _config.TargetProcesses.Remove(proc);
            ConfigManager.Save(_config);
            _onConfigChanged?.Invoke();
            RefreshProcessList();
        };
        deleteBtn.MouseEnter += (_, _) => deleteBtn.ForeColor = Danger;
        deleteBtn.MouseLeave += (_, _) => deleteBtn.ForeColor = MutedText;
        card.Controls.Add(deleteBtn);

        return card;
    }

    private static Color GetProcessColor(string proc)
    {
        var colors = new[]
        {
            Color.FromArgb(232, 160, 8),     // amber
            Color.FromArgb(56, 189, 248),     // sky
            Color.FromArgb(52, 211, 153),     // emerald
            Color.FromArgb(251, 146, 60),     // orange
            Color.FromArgb(167, 139, 250),    // violet
            Color.FromArgb(244, 114, 182)     // pink
        };
        return colors[Math.Abs(proc.GetHashCode()) % colors.Length];
    }

    private void AddProcess_Click(object? sender, EventArgs e)
    {
        var name = _processInput.Text.Trim();
        if (name.Length == 0) return;

        if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            name += ".exe";
        }

        if (_config.TargetProcesses.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            _processInput.Text = "";
            return;
        }

        _config.TargetProcesses.Add(name);
        ConfigManager.Save(_config);
        _onConfigChanged?.Invoke();
        _processInput.Text = "";
        RefreshProcessList();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    private static Icon LoadAppIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("app.ico");
        if (stream != null)
            return new Icon(stream);
        return Icon.ExtractAssociatedIcon(Application.ExecutablePath)!;
    }

    private static void DisposeControls(Control container)
    {
        for (int i = container.Controls.Count - 1; i >= 0; i--)
        {
            var c = container.Controls[i];
            if (c.Controls.Count > 0)
                DisposeControls(c);
            container.Controls.RemoveAt(i);
            c.Dispose();
        }
    }

    private void InitializeComponent() { }
}