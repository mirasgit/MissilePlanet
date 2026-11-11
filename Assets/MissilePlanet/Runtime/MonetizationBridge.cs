using System;
using UnityEngine;

namespace MissilePlanet
{
    /// <summary>
    /// Implement this in a separate integration script for LevelPlay/AdMob + Unity IAP or another store.
    /// Gameplay and HUD remain SDK-agnostic.
    /// </summary>
    public abstract class MonetizationProviderBehaviour : MonoBehaviour
    {
        public abstract bool RewardedAvailable { get; }
        public abstract bool PurchasesAvailable { get; }
        public abstract void ShowRewarded(string placement, Action onReward, Action onUnavailable = null);
        public abstract void PurchaseCorePack(string productId, int cores, Action onSuccess, Action onUnavailable = null);
        public virtual string GetLocalizedPrice(string productId) => string.Empty;
    }

    public sealed class MonetizationBridge : MonoBehaviour
    {
        [SerializeField] private MonetizationProviderBehaviour _provider;
        [SerializeField] private bool _simulateInEditor = true;

        public bool RewardedAvailable
        {
            get
            {
                if (_provider != null) return _provider.RewardedAvailable;
#if UNITY_EDITOR
                return _simulateInEditor;
#else
                return false;
#endif
            }
        }

        public bool PurchasesAvailable
        {
            get
            {
                if (_provider != null) return _provider.PurchasesAvailable;
#if UNITY_EDITOR
                return _simulateInEditor;
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_provider == null)
                _provider = GetComponent<MonetizationProviderBehaviour>();
        }

        public void ShowRewarded(string placement, Action onReward, Action onUnavailable = null)
        {
            if (_provider != null)
            {
                _provider.ShowRewarded(placement, onReward, onUnavailable);
                return;
            }

#if UNITY_EDITOR
            if (_simulateInEditor)
            {
                Debug.Log($"[Monetization Mock] Rewarded completed: {placement}");
                onReward?.Invoke();
                return;
            }
#endif
            Debug.LogWarning($"Rewarded provider is not connected yet. Placement: {placement}");
            onUnavailable?.Invoke();
        }


        public string GetLocalizedPrice(string productId, string editorFallback)
        {
            if (_provider != null)
            {
                string price = _provider.GetLocalizedPrice(productId);
                if (!string.IsNullOrWhiteSpace(price)) return price;
            }
#if UNITY_EDITOR
            return _simulateInEditor ? editorFallback : "SDK";
#else
            return "STORE";
#endif
        }

        public void PurchaseCorePack(string productId, int cores, Action onSuccess, Action onUnavailable = null)
        {
            if (_provider != null)
            {
                _provider.PurchaseCorePack(
                    productId,
                    cores,
                    () =>
                    {
                        MetaProgression.AddCores(cores);
                        onSuccess?.Invoke();
                    },
                    onUnavailable);
                return;
            }

#if UNITY_EDITOR
            if (_simulateInEditor)
            {
                Debug.Log($"[Monetization Mock] IAP completed: {productId} -> {cores} cores");
                MetaProgression.AddCores(cores);
                onSuccess?.Invoke();
                return;
            }
#endif
            Debug.LogWarning($"IAP provider is not connected yet. Product: {productId}");
            onUnavailable?.Invoke();
        }
    }
}
