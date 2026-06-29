using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using DesktopGarden.Core;

namespace DesktopGarden;

internal sealed class GardenForm : Form
{
    private const int HotKeyId = 7419;
    private const int DragThreshold = 5;
    private const int AmbientFrameCount = 24;
    private static readonly TimeSpan ReminderInterval = TimeSpan.FromMinutes(15);
    private readonly GardenState _state;
    private readonly AssetCatalog _catalog;
    private readonly GardenRenderer _renderer;
    private readonly Dictionary<Guid, PotAnimation> _animations = [];
    private readonly Dictionary<Guid, TemporaryExpression> _temporaryExpressions = [];
    private readonly System.Windows.Forms.Timer _animationTimer;
    private readonly System.Windows.Forms.Timer _hoverTimer;
    private readonly Stopwatch _sessionStopwatch = Stopwatch.StartNew();
    private readonly Stopwatch _reminderStopwatch = Stopwatch.StartNew();
    private GardenRenderResult? _render;
    private LayeredSurface? _surface;
    private MouseButtons _pressedButton;
    private int _pressedIndex = -1;
    private Point _pressClient;
    private Point _pressScreen;
    private int _startOffsetX;
    private int _startOffsetY;
    private float _dragPointerOffset;
    private float _dragVisualOffset;
    private bool _draggingPot;
    private bool _draggingGarden;
    private int _dragSourceIndex = -1;
    private int _dragTargetIndex = -1;
    private int _hoveredIndex = -1;
    private int _pendingHoverIndex = -1;
    private readonly List<byte[]> _ambientFrames = [];
    private string _ambientSignature = string.Empty;
    private int _ambientFrameIndex;
    private IReadOnlyDictionary<FooterControlKind, Rectangle> _footerControls = new Dictionary<FooterControlKind, Rectangle>();
    private FooterControlKind? _pressedFooter;
    private TimeSpan? _pomodoroDuration;
    private DateTime? _pomodoroEndsUtc;
    private bool _pomodoroExpired;

    public GardenForm(GardenState state, AssetCatalog catalog, ImageCache imageCache)
    {
        _state = state;
        _catalog = catalog;
        _renderer = new GardenRenderer(catalog, imageCache);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = AppMode.QaWindow;
        StartPosition = FormStartPosition.Manual;
        TopMost = state.Settings.AlwaysOnTop;
        Text = "Lovely Plants";

        _animationTimer = new System.Windows.Forms.Timer { Interval = 167 };
        _animationTimer.Tick += (_, _) => OnAnimationFrame();
        _hoverTimer = new System.Windows.Forms.Timer();
        _hoverTimer.Tick += (_, _) => CommitPendingHover();
        MouseDown += HandleMouseDown;
        MouseMove += HandleMouseMove;
        MouseUp += HandleMouseUp;
        MouseLeave += (_, _) => ScheduleHover(-1, 120);
    }

    public event Action<PotInstance, Rectangle>? InspectorRequested;
    public event Action<PotInstance?, Rectangle?>? HoverChanged;
    public event Action? StateChanged;
    public event Action? ToggleRequested;
    public event Action? ReminderDue;
    public event Action? PomodoroConfigureRequested;
    public event Action? PomodoroCompleted;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WsExLayered;
            if (!AppMode.QaWindow)
            {
                parameters.ExStyle |= NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate;
            }
            return parameters;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeMethods.RegisterHotKey(Handle, HotKeyId, 0x0001 | 0x0002, (uint)Keys.G);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        NativeMethods.UnregisterHotKey(Handle, HotKeyId);
        base.OnHandleDestroyed(e);
    }

    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(value);
        if (value && IsHandleCreated)
        {
            _animationTimer.Start();
            BeginInvoke(RenderNow);
        }
        else if (!value)
        {
            _animationTimer.Stop();
            HideHover();
        }
    }

    public void RenderNow()
    {
        if (!IsHandleCreated || _state.Pots.Count == 0)
        {
            Hide();
            return;
        }

        var screen = SelectedScreen();
        var effectiveScale = LayoutCalculator.FitScale(screen.WorkingArea.Width, _state.Pots.Count, _state.Settings.Scale, _state.Settings.GapScale);
        EnsureSurface(_renderer.Measure(_state, effectiveScale));
        var draggedId = _draggingPot && _pressedIndex >= 0 && _pressedIndex < _state.Pots.Count
            ? _state.Pots[_pressedIndex].Id
            : (Guid?)null;
        var next = _renderer.Render(
            _state,
            effectiveScale,
            _animations,
            _temporaryExpressions.ToDictionary(item => item.Key, item => item.Value.ExpressionId),
            BuildOverlayState(),
            null,
            draggedId,
            _dragVisualOffset,
            _surface!.Bitmap);
        _render?.Dispose();
        _render = next;
        _footerControls = next.FooterControls;

        var destination = GetDestination(screen.WorkingArea, next.Bitmap.Size);
        SetBounds(destination.X, destination.Y, next.Bitmap.Width, next.Bitmap.Height);
        ApplyLayeredSurface(destination);
        TopMost = _state.Settings.AlwaysOnTop;
    }

    public void StartAnimation(Guid potId, PotAnimationKind kind)
    {
        _animations[potId] = new PotAnimation(kind, DateTime.UtcNow);
        var expressionIndex = 3;
        var expressions = _catalog.ExpressionIds;
        _temporaryExpressions[potId] = new TemporaryExpression(
            expressions[Math.Min(expressionIndex, expressions.Count - 1)],
            DateTime.UtcNow.AddSeconds(2));
        _animationTimer.Interval = 50;
        if (Visible) _animationTimer.Start();
        RenderNow();
    }

    public void SetPomodoro(TimeSpan duration)
    {
        _pomodoroDuration = duration;
        _pomodoroEndsUtc = DateTime.UtcNow + duration;
        _pomodoroExpired = false;
        RenderNow();
    }

    public void ResetPomodoro()
    {
        _pomodoroDuration = null;
        _pomodoroEndsUtc = null;
        _pomodoroExpired = false;
        RenderNow();
    }

    public int Capacity
    {
        get
        {
            var screen = SelectedScreen();
            return LayoutCalculator.GetCapacity(screen.WorkingArea.Width, _state.Settings.Scale, _state.Settings.GapScale);
        }
    }

    public TimeSpan? CurrentPomodoroDuration => _pomodoroDuration;

    public Rectangle? AnchorForRandomPot()
    {
        if (_state.Pots.Count == 0) return null;
        var index = Random.Shared.Next(_state.Pots.Count);
        var bounds = SlotScreenBounds(index);
        return bounds.IsEmpty ? null : bounds;
    }

    public Rectangle? AnchorForPot(Guid potId)
    {
        return TryGetPotScreenBounds(potId, out var bounds) ? bounds : null;
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WmHotKey && message.WParam.ToInt32() == HotKeyId)
        {
            ToggleRequested?.Invoke();
            return;
        }

        if (message.Msg == NativeMethods.ShowExistingMessage)
        {
            if (!Visible) Show();
            RenderNow();
            return;
        }

        if (message.Msg == NativeMethods.WmNcHitTest)
        {
            if (_state.Settings.InteractionLocked || !IsOpaqueAtCursor())
            {
                message.Result = new IntPtr(NativeMethods.HtTransparent);
                return;
            }

            message.Result = new IntPtr(NativeMethods.HtClient);
            return;
        }

        base.WndProc(ref message);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Dispose();
            _hoverTimer.Dispose();
            _sessionStopwatch.Stop();
            _reminderStopwatch.Stop();
            _render?.Dispose();
            _surface?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void HandleMouseDown(object? sender, MouseEventArgs e)
    {
        if (_render is null) return;

        var footer = FooterAt(e.Location);
        if (footer is not null)
        {
            _pressedButton = e.Button;
            _pressedFooter = footer;
            return;
        }

        if (e.Button is not MouseButtons.Left and not MouseButtons.Right) return;
        var index = SlotAt(e.Location);
        if (index < 0) return;

        _pressedButton = e.Button;
        _pressedIndex = index;
        _pressClient = e.Location;
        _pressScreen = Cursor.Position;
        _startOffsetX = _state.Settings.GardenOffsetX;
        _startOffsetY = _state.Settings.GardenOffsetY;
        _dragPointerOffset = e.X - (_render.Slots[index].Left + _render.Slots[index].Width / 2f);
        _dragVisualOffset = 0;
        _draggingPot = false;
        _draggingGarden = false;
        _dragSourceIndex = index;
        _dragTargetIndex = index;
        HideHover();
        Capture = true;
    }

    private void HandleMouseMove(object? sender, MouseEventArgs e)
    {
        if (_pressedFooter is not null)
        {
            return;
        }

        if (_pressedButton == MouseButtons.None || _pressedIndex < 0 || _render is null)
        {
            ScheduleHover(SlotAt(e.Location), 180);
            return;
        }

        var distance = Math.Abs(e.X - _pressClient.X) + Math.Abs(e.Y - _pressClient.Y);
        if (distance < DragThreshold) return;

        if (_pressedButton == MouseButtons.Left)
        {
            _draggingPot = true;
            var desiredCenter = e.X - _dragPointerOffset;
            _dragTargetIndex = Math.Max(0, IndexForCenter(desiredCenter));
            var currentSlot = _render.Slots[Math.Clamp(_dragSourceIndex, 0, _render.Slots.Count - 1)];
            _dragVisualOffset = desiredCenter - (currentSlot.Left + currentSlot.Width / 2f);
            RenderNow();
        }
        else
        {
            _draggingGarden = true;
            var current = Cursor.Position;
            _state.Settings.GardenOffsetX = _startOffsetX + current.X - _pressScreen.X;
            _state.Settings.GardenOffsetY = _startOffsetY + current.Y - _pressScreen.Y;
            RenderNow();
        }
    }

    private void HandleMouseUp(object? sender, MouseEventArgs e)
    {
        Capture = false;

        if (_pressedFooter is { } footer)
        {
            var releasedOnSame = FooterAt(e.Location) == footer;
            if (releasedOnSame)
            {
                HandleFooterClick(footer, e.Button);
            }
            _pressedFooter = null;
            _pressedButton = MouseButtons.None;
            return;
        }

        var clickedIndex = _pressedIndex;
        var wasPotDrag = _draggingPot;
        var wasGardenDrag = _draggingGarden;

        if (_pressedButton == MouseButtons.Right && !wasGardenDrag && clickedIndex >= 0 && clickedIndex < _state.Pots.Count && _render is not null)
        {
            InspectorRequested?.Invoke(_state.Pots[clickedIndex], SlotScreenBounds(clickedIndex));
        }

        _pressedButton = MouseButtons.None;
        _pressedIndex = -1;
        _dragVisualOffset = 0;
        _draggingPot = false;
        _draggingGarden = false;

        if (wasPotDrag)
        {
            CommitPotReorder();
        }

        _dragSourceIndex = -1;
        _dragTargetIndex = -1;

        if (wasPotDrag || wasGardenDrag)
        {
            GardenStateFactory.Normalize(_state);
            StateChanged?.Invoke();
            RenderNow();
        }
    }

    private void HandleFooterClick(FooterControlKind footer, MouseButtons button)
    {
        if (footer != FooterControlKind.Pomodoro) return;

        if (button == MouseButtons.Right)
        {
            ResetPomodoro();
            return;
        }

        PomodoroConfigureRequested?.Invoke();
    }

    private int SlotAt(Point point)
    {
        if (_render is null) return -1;
        for (var index = 0; index < _render.Slots.Count; index++)
        {
            if (_render.Slots[index].Contains(point)) return index;
        }
        return -1;
    }

    private FooterControlKind? FooterAt(Point point)
    {
        foreach (var item in _footerControls)
        {
            if (item.Value.Contains(point)) return item.Key;
        }
        return null;
    }

    private int IndexForCenter(float x)
    {
        if (_render is null || _render.Slots.Count == 0) return -1;
        for (var index = 0; index < _render.Slots.Count; index++)
        {
            var center = _render.Slots[index].Left + _render.Slots[index].Width / 2f;
            if (x < center) return index;
        }
        return _render.Slots.Count - 1;
    }

    private Rectangle SlotScreenBounds(int index)
    {
        if (_render is null || index < 0 || index >= _render.Slots.Count) return Rectangle.Empty;
        var slot = _render.Slots[index];
        return new Rectangle(Left + slot.Left, Top + slot.Top, slot.Width, slot.Height);
    }

    public bool TryGetPotScreenBounds(Guid potId, out Rectangle bounds)
    {
        var index = _state.Pots.FindIndex(item => item.Id == potId);
        bounds = SlotScreenBounds(index);
        return !bounds.IsEmpty;
    }

    private void ScheduleHover(int index, int delay)
    {
        if (_pressedButton != MouseButtons.None || index == _hoveredIndex && !_hoverTimer.Enabled) return;
        if (index == _pendingHoverIndex && _hoverTimer.Enabled) return;
        _pendingHoverIndex = index;
        _hoverTimer.Stop();
        _hoverTimer.Interval = delay;
        _hoverTimer.Start();
    }

    private void CommitPendingHover()
    {
        _hoverTimer.Stop();
        if (_pendingHoverIndex == _hoveredIndex) return;
        _hoveredIndex = _pendingHoverIndex;
        if (_hoveredIndex >= 0 && _hoveredIndex < _state.Pots.Count)
        {
            HoverChanged?.Invoke(_state.Pots[_hoveredIndex], SlotScreenBounds(_hoveredIndex));
        }
        else
        {
            HoverChanged?.Invoke(null, null);
        }
    }

    private void HideHover()
    {
        _hoverTimer.Stop();
        _pendingHoverIndex = -1;
        if (_hoveredIndex == -1) return;
        _hoveredIndex = -1;
        HoverChanged?.Invoke(null, null);
    }

    private bool IsOpaqueAtCursor()
    {
        if (_render is null) return false;
        var local = PointToClient(Cursor.Position);
        if (local.X < 0 || local.Y < 0 || local.X >= _render.Bitmap.Width || local.Y >= _render.Bitmap.Height) return false;
        return _render.Bitmap.GetPixel(local.X, local.Y).A > 12;
    }

    private void OnAnimationFrame()
    {
        var now = DateTime.UtcNow;
        foreach (var id in _animations.Where(item => item.Value.IsFinished(now)).Select(item => item.Key).ToList()) _animations.Remove(id);
        foreach (var id in _temporaryExpressions.Where(item => item.Value.ExpiresUtc <= now).Select(item => item.Key).ToList()) _temporaryExpressions.Remove(id);
        UpdatePomodoro(now);
        UpdateReminderSchedule();

        if (!Visible || _pressedButton != MouseButtons.None) return;
        if (_animations.Count > 0 || _temporaryExpressions.Count > 0)
        {
            RenderNow();
            return;
        }

        if (_animationTimer.Interval != 167) _animationTimer.Interval = 167;
        RenderAmbientFrame();
    }

    private void UpdatePomodoro(DateTime now)
    {
        if (_pomodoroEndsUtc is null || _pomodoroExpired) return;
        if (now < _pomodoroEndsUtc.Value) return;

        _pomodoroExpired = true;
        _pomodoroEndsUtc = null;
        PomodoroCompleted?.Invoke();
    }

    private void UpdateReminderSchedule()
    {
        if (_reminderStopwatch.Elapsed < ReminderInterval) return;
        _reminderStopwatch.Restart();
        ReminderDue?.Invoke();
    }

    private void RenderAmbientFrame()
    {
        var screen = SelectedScreen();
        var effectiveScale = LayoutCalculator.FitScale(screen.WorkingArea.Width, _state.Pots.Count, _state.Settings.Scale, _state.Settings.GapScale);
        var signature = BuildAmbientSignature(effectiveScale);
        if (!string.Equals(signature, _ambientSignature, StringComparison.Ordinal))
        {
            BuildAmbientFrames(effectiveScale, signature);
        }
        if (_surface is null || _ambientFrames.Count == 0) return;

        var frame = _ambientFrames[_ambientFrameIndex % _ambientFrames.Count];
        Marshal.Copy(frame, 0, _surface.Bits, frame.Length);
        using (var graphics = Graphics.FromImage(_surface.Bitmap))
        {
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            _renderer.DrawFooterOverlay(graphics, _surface.Size, BuildOverlayState());
        }
        _footerControls = _renderer.GetFooterControls(_surface.Size);
        _ambientFrameIndex = (_ambientFrameIndex + 1) % _ambientFrames.Count;
        var destination = GetDestination(screen.WorkingArea, _surface.Size);
        SetBounds(destination.X, destination.Y, _surface.Size.Width, _surface.Size.Height);
        ApplyLayeredSurface(destination);
    }

    private void BuildAmbientFrames(float effectiveScale, string signature)
    {
        EnsureSurface(_renderer.Measure(_state, effectiveScale));
        _ambientFrames.Clear();
        _ambientFrameIndex = 0;
        for (var index = 0; index < AmbientFrameCount; index++)
        {
            var frameTime = DateTime.UnixEpoch.AddSeconds(index * 4d / AmbientFrameCount);
            _render?.Dispose();
            _render = _renderer.Render(
                _state,
                effectiveScale,
                new Dictionary<Guid, PotAnimation>(),
                new Dictionary<Guid, string>(),
                BuildOverlayState(),
                null,
                target: _surface!.Bitmap,
                renderTimeUtc: frameTime);
            var frame = new byte[_surface.ByteLength];
            Marshal.Copy(_surface.Bits, frame, 0, frame.Length);
            _ambientFrames.Add(frame);
        }
        _ambientSignature = signature;
    }

    private string BuildAmbientSignature(float effectiveScale) => string.Join(
        '|',
        effectiveScale.ToString("F3", System.Globalization.CultureInfo.InvariantCulture),
        _state.Settings.GapScale.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
        string.Join(';', _state.Pots.Select(pot => $"{pot.Id}:{pot.PlantId}:{pot.GrowthStage}:{pot.PotId}:{pot.ExpressionId}:{pot.Scale:F2}")));

    private GardenOverlayState BuildOverlayState()
    {
        var work = _sessionStopwatch.Elapsed;
        var workText = $"{(int)work.TotalHours:00}:{work.Minutes:00}:{work.Seconds:00}";

        string pomodoroText;
        if (_pomodoroExpired)
        {
            pomodoroText = "00:00:00";
        }
        else if (_pomodoroEndsUtc is { } endsUtc)
        {
            var remaining = endsUtc - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            pomodoroText = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
        else
        {
            pomodoroText = _pomodoroDuration is { } duration
                ? $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
                : "00:00:00";
        }

        return new GardenOverlayState(workText, pomodoroText, _pomodoroExpired, _pomodoroDuration is not null);
    }

    private void CommitPotReorder()
    {
        if (_dragSourceIndex < 0 || _dragSourceIndex >= _state.Pots.Count)
        {
            return;
        }

        var targetIndex = Math.Clamp(_dragTargetIndex, 0, _state.Pots.Count - 1);
        if (targetIndex == _dragSourceIndex)
        {
            return;
        }

        var moving = _state.Pots[_dragSourceIndex];
        _state.Pots.RemoveAt(_dragSourceIndex);
        _state.Pots.Insert(targetIndex, moving);
        for (var index = 0; index < _state.Pots.Count; index++)
        {
            _state.Pots[index].SortOrder = index;
        }
    }

    private Screen SelectedScreen()
    {
        var screens = Screen.AllScreens;
        return screens[Math.Clamp(_state.Settings.MonitorIndex, 0, screens.Length - 1)];
    }

    private Point GetDestination(Rectangle workArea, Size size)
    {
        var baseX = workArea.Left + (workArea.Width - size.Width) / 2;
        var baseY = workArea.Bottom - size.Height - 8;
        var x = baseX + _state.Settings.GardenOffsetX;
        var y = baseY + _state.Settings.GardenOffsetY;
        const int visibleEdge = 80;
        x = Math.Clamp(x, workArea.Left - size.Width + visibleEdge, workArea.Right - visibleEdge);
        y = Math.Clamp(y, workArea.Top - size.Height + visibleEdge, workArea.Bottom - visibleEdge);
        _state.Settings.GardenOffsetX = x - baseX;
        _state.Settings.GardenOffsetY = y - baseY;
        return new Point(x, y);
    }

    private void EnsureSurface(Size size)
    {
        if (_surface is not null && _surface.Size == size) return;
        _render?.Dispose();
        _render = null;
        _surface?.Dispose();
        _surface = new LayeredSurface(size);
    }

    private void ApplyLayeredSurface(Point destination)
    {
        if (_surface is null) return;
        var screenDc = NativeMethods.GetDC(IntPtr.Zero);
        try
        {
            var topLeft = new NativeMethods.PointNative(destination.X, destination.Y);
            var size = new NativeMethods.SizeNative(_surface.Size.Width, _surface.Size.Height);
            var source = new NativeMethods.PointNative(0, 0);
            var blend = new NativeMethods.BlendFunction
            {
                BlendOp = NativeMethods.SrcOver,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AcSrcAlpha
            };
            NativeMethods.UpdateLayeredWindow(Handle, screenDc, ref topLeft, ref size, _surface.MemoryDc, ref source, 0, ref blend, NativeMethods.UlwAlpha);
        }
        finally
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private static class NativeGdi
    {
        [DllImport("gdi32.dll")]
        internal static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr value);
        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr value);
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC(IntPtr deviceContext);
        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern IntPtr CreateDIBSection(IntPtr deviceContext, ref BitmapInfo bitmapInfo, uint usage, out IntPtr bits, IntPtr section, uint offset);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ColorsUsed;
        public uint ColorsImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }

    private sealed class LayeredSurface : IDisposable
    {
        private readonly IntPtr _bitmapHandle;
        private readonly IntPtr _oldBitmap;

        internal LayeredSurface(Size size)
        {
            Size = size;
            var screenDc = NativeMethods.GetDC(IntPtr.Zero);
            MemoryDc = NativeGdi.CreateCompatibleDC(screenDc);
            var info = new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = size.Width,
                    Height = -size.Height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = 0
                }
            };
            _bitmapHandle = NativeGdi.CreateDIBSection(screenDc, ref info, 0, out var bits, IntPtr.Zero, 0);
            Bits = bits;
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
            if (_bitmapHandle == IntPtr.Zero || bits == IntPtr.Zero) throw new InvalidOperationException("Unable to create layered drawing surface.");
            _oldBitmap = NativeGdi.SelectObject(MemoryDc, _bitmapHandle);
            Bitmap = new Bitmap(size.Width, size.Height, size.Width * 4, PixelFormat.Format32bppPArgb, bits);
        }

        internal Size Size { get; }
        internal IntPtr MemoryDc { get; }
        internal IntPtr Bits { get; private set; }
        internal int ByteLength => Size.Width * Size.Height * 4;
        internal Bitmap Bitmap { get; }

        public void Dispose()
        {
            Bitmap.Dispose();
            NativeGdi.SelectObject(MemoryDc, _oldBitmap);
            NativeGdi.DeleteObject(_bitmapHandle);
            NativeGdi.DeleteDC(MemoryDc);
            Bits = IntPtr.Zero;
        }
    }

    private sealed record TemporaryExpression(string ExpressionId, DateTime ExpiresUtc);
}
