using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    // Reusable, listener-specific teardown. It intentionally removes only callbacks
    // installed by the current row binding and never calls RemoveAllListeners.
    internal sealed class ListenerScope : IDisposable
    {
        private readonly List<Action> _removeListeners = new List<Action>();

        internal void Listen(Button button, UnityAction listener)
        {
            button.onClick.AddListener(listener);
            _removeListeners.Add(() => button.onClick.RemoveListener(listener));
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRegistrations);
        }

        internal void Listen(Toggle toggle, UnityAction<bool> listener)
        {
            toggle.onValueChanged.AddListener(listener);
            _removeListeners.Add(() => toggle.onValueChanged.RemoveListener(listener));
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRegistrations);
        }

        internal void Listen(Dropdown dropdown, UnityAction<int> listener)
        {
            dropdown.onValueChanged.AddListener(listener);
            _removeListeners.Add(() => dropdown.onValueChanged.RemoveListener(listener));
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRegistrations);
        }

        internal void Listen(InputField input, UnityAction<string> listener)
        {
            input.onEndEdit.AddListener(listener);
            _removeListeners.Add(() => input.onEndEdit.RemoveListener(listener));
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRegistrations);
        }

        internal void Listen(Slider slider, UnityAction<float> listener)
        {
            slider.onValueChanged.AddListener(listener);
            _removeListeners.Add(() => slider.onValueChanged.RemoveListener(listener));
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRegistrations);
        }

        internal void OnDispose(Action removeListener)
        {
            _removeListeners.Add(removeListener);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRegistrations);
        }

        internal void Clear()
        {
            for (var i = _removeListeners.Count - 1; i >= 0; i--)
            {
                _removeListeners[i]();
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.ListenerRemovals);
            }
            _removeListeners.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
