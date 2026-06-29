using System;
using UnityEngine;

namespace CarFactoryIdle.Events
{
    /// <summary>Base ScriptableObject event channel. UI/audio/mascot listeners subscribe in code
    /// or wire the asset in the inspector; the simulation only ever calls Raise(). This is the
    /// single, controlled boundary between logic and presentation (no global SignalBus).</summary>
    public abstract class EventChannel<T> : ScriptableObject
    {
        private event Action<T> _listeners;
        public void Subscribe(Action<T> cb) => _listeners += cb;
        public void Unsubscribe(Action<T> cb) => _listeners -= cb;
        public void Raise(T payload) => _listeners?.Invoke(payload);
    }

    public abstract class EventChannel : ScriptableObject
    {
        private event Action _listeners;
        public void Subscribe(Action cb) => _listeners += cb;
        public void Unsubscribe(Action cb) => _listeners -= cb;
        public void Raise() => _listeners?.Invoke();
    }
}
