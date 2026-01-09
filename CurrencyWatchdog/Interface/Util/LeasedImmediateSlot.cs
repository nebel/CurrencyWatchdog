using System;
using System.Collections.Generic;

namespace CurrencyWatchdog.Interface.Util;

public sealed class LeasedImmediateSlot<TKey, T>
    where TKey : notnull {
    private TKey? owner;
    private int generation;
    private bool assertedThisFrame;
    private bool hasValue;
    private T? value;

    public event Action? Invalidated;

    public ResponseToken Acquire(TKey ownerKey) {
        InvalidateInternal();

        owner = ownerKey;
        assertedThisFrame = true;
        generation++;

        return new ResponseToken(this, ownerKey, generation);
    }

    public T? TryConsume(TKey ownerKey) {
        if (owner is null || !EqualityComparer<TKey>.Default.Equals(owner, ownerKey)) {
            return default;
        }

        assertedThisFrame = true;

        if (hasValue) {
            var result = value;
            Clear();
            return result;
        }

        return default;
    }

    public void EndFrame() {
        if (owner is not null && !assertedThisFrame) {
            InvalidateInternal();
        }

        assertedThisFrame = false;
    }

    public void Invalidate() {
        InvalidateInternal();
    }

    private void SetValueInternal(TKey ownerKey, int generationParam, T valueParam) {
        if (owner is null || generationParam != generation || !EqualityComparer<TKey>.Default.Equals(owner, ownerKey))
            return;

        value = valueParam;
        hasValue = true;
    }

    private void InvalidateInternal() {
        if (owner is not null) {
            Invalidated?.Invoke();
        }
        Clear();
    }

    private void Clear() {
        owner = default;
        assertedThisFrame = false;
        hasValue = false;
        value = default;
    }

    public readonly struct ResponseToken {
        private readonly LeasedImmediateSlot<TKey, T>? parent;
        private readonly TKey owner;
        private readonly int generation;

        internal ResponseToken(LeasedImmediateSlot<TKey, T> parent, TKey owner, int generation) {
            this.parent = parent;
            this.owner = owner;
            this.generation = generation;
        }

        public void Supply(T value) {
            parent?.SetValueInternal(owner, generation, value);
        }
    }
}
