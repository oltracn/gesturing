using System.Threading.Channels;
using Gesturing.Models;

namespace Gesturing.Services;

public class GestureStateMachine : IDisposable
{
    private enum State { Idle, Collecting }

    private readonly ChannelReader<HookEvent> _reader;
    private readonly List<string> _directions = new(4);
    private readonly CancellationTokenSource _cts = new();
    private Thread? _workerThread;
    private State _state = State.Idle;
    private int _startX, _startY;
    private int _lastDirX, _lastDirY;
    private DateTime _collectStartTime;
    private List<GestureRule> _rules;
    private bool _disposed;

    private const int DebounceThreshold = 10;
    private const int ShortDistanceThreshold = 15;
    private const int GestureTimeoutMs = 3000;

    public List<GestureRule> Rules
    {
        get => _rules;
        set => _rules = value ?? new List<GestureRule>();
    }

    public GestureStateMachine(ChannelReader<HookEvent> reader, List<GestureRule> rules)
    {
        _reader = reader;
        _rules = rules ?? new List<GestureRule>();
    }

    public void Start()
    {
        _workerThread = new Thread(Run)
        {
            IsBackground = true,
            Name = "GestureEngine"
        };
        _workerThread.Start();
    }

    private void Run()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                int timeoutMs = _state == State.Collecting
                    ? Math.Max(1, GestureTimeoutMs - (int)(DateTime.UtcNow - _collectStartTime).TotalMilliseconds)
                    : Timeout.Infinite;

                var waitTask = _reader.WaitToReadAsync(_cts.Token).AsTask();

                if (waitTask.Wait(timeoutMs, _cts.Token))
                {
                    while (_reader.TryRead(out var evt))
                    {
                        ProcessEvent(evt);
                    }
                }
                else if (_state == State.Collecting)
                {
                    HandleTimeout();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }

    private void ProcessEvent(HookEvent evt)
    {
        switch (_state)
        {
            case State.Idle:
                if (evt.Type == HookEventType.Down)
                {
                    _state = State.Collecting;
                    _startX = evt.X;
                    _startY = evt.Y;
                    _lastDirX = evt.X;
                    _lastDirY = evt.Y;
                    _collectStartTime = DateTime.UtcNow;
                    _directions.Clear();
                }
                break;

            case State.Collecting:
                switch (evt.Type)
                {
                    case HookEventType.Move:
                        HandleMove(evt);
                        break;
                    case HookEventType.Up:
                        HandleUp(evt);
                        break;
                    case HookEventType.Down:
                        // spurious — reset
                        HandleTimeout();
                        break;
                }
                break;
        }
    }

    private void HandleMove(HookEvent evt)
    {
        int dx = evt.X - _lastDirX;
        int dy = evt.Y - _lastDirY;
        int absDx = Math.Abs(dx);
        int absDy = Math.Abs(dy);

        if (absDx < DebounceThreshold && absDy < DebounceThreshold)
        {
            return;
        }

        string dir;
        if (absDx >= absDy)
        {
            dir = dx > 0 ? "R" : "L";
        }
        else
        {
            dir = dy > 0 ? "D" : "U";
        }

        _lastDirX = evt.X;
        _lastDirY = evt.Y;

        // dedup: only add if different from last direction
        if (_directions.Count == 0 || _directions[^1] != dir)
        {
            if (_directions.Count >= 4)
            {
                // exceeded max steps — discard this gesture
                ResetToIdle();
                return;
            }

            _directions.Add(dir);
        }
    }

    private void HandleUp(HookEvent evt)
    {
        // check short distance
        int totalDx = evt.X - _startX;
        int totalDy = evt.Y - _startY;
        double distance = Math.Sqrt(totalDx * totalDx + totalDy * totalDy);

        if (distance < ShortDistanceThreshold || _directions.Count == 0)
        {
            InputSender.SendRightClick();
            ResetToIdle();
            return;
        }

        if (_directions.Count > 4)
        {
            ResetToIdle();
            return;
        }

        var match = GestureMatcher.Match(_directions, _rules);
        if (match != null)
        {
            InputSender.SendHotkey(match.Hotkey);
        }

        ResetToIdle();
    }

    private void HandleTimeout()
    {
        InputSender.SendRightClick();
        ResetToIdle();
    }

    private void ResetToIdle()
    {
        _state = State.Idle;
        _directions.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }
}
