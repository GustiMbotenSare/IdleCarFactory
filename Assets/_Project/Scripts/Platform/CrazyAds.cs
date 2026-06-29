using System;
using UnityEngine;

namespace CarFactoryIdle.Platform
{
    /// <summary>Placeholder bridge to the CrazyGames SDK (ads + lifecycle signals). The game logic
    /// only ever calls the methods below, so all the real platform work lives here in one place.
    ///
    /// FOR THE UNITY DEVELOPER: this is a stub. Every method is currently a no-op so the project
    /// builds and runs in the editor without the SDK. When you wire up the WebGL build, import the
    /// CrazyGames SDK (v3) and replace each body with the matching SDK call. Drop this component on
    /// the same GameObject as GameRoot and assign it to GameRoot's "ads" field in the inspector
    /// (it is optional, so the game still runs if it is left empty).</summary>
    public class CrazyAds : MonoBehaviour
    {
        /// <summary>Called once on boot. Initialize the CrazyGames SDK here.</summary>
        public void Init() { }

        /// <summary>Tell the platform that active gameplay has started (used for analytics and ad
        /// timing). Called after the game finishes loading.</summary>
        public void GameplayStart() { }

        /// <summary>Tell the platform that gameplay has paused or stopped (for example when a menu
        /// or modal opens). Pair this with GameplayStart.</summary>
        public void GameplayStop() { }

        /// <summary>Show a rewarded video ad. Call onComplete(true) when the player earned the
        /// reward, or onComplete(false) if the ad was skipped or failed. The stub reports failure so
        /// no reward is granted until the real SDK is wired in.</summary>
        public void ShowRewardedAd(Action<bool> onComplete) { onComplete?.Invoke(false); }

        /// <summary>Show a non-rewarded interstitial ad (for example between major actions).</summary>
        public void ShowMidgameAd() { }
    }
}
