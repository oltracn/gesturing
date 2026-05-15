namespace Gesturing.Models;

public enum HookEventType
{
    Down,
    Move,
    Up
}

public readonly record struct HookEvent(
    HookEventType Type,
    int X,
    int Y,
    long Timestamp
);
