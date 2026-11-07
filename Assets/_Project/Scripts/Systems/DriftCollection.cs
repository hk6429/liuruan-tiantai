using System;
using UnityEngine;

namespace Liuruan
{
    public sealed class DriftCollection : MonoBehaviour
    {
        private DriftCollectionCore core;
        public event Action<int> OnCollected;
        public event Action OnAllCollected;
        public int Count => Core.Count;
        public DriftCollectionCore Core
        {
            get
            {
                if (core == null)
                {
                    core = new DriftCollectionCore();
                    core.OnCollected += id => OnCollected?.Invoke(id);
                    core.OnAllCollected += () => OnAllCollected?.Invoke();
                }
                return core;
            }
        }
    }
}
