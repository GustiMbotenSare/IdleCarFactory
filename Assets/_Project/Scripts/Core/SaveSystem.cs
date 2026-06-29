using System;
using UnityEngine;
using CarFactoryIdle.Platform;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Core
{
    /// <summary>JSON save/load via PlayerPrefs. On WebGL, PlayerPrefs persists to IndexedDB; we
    /// flush it so a refresh/tab-close keeps progress. Incompatible saves (version mismatch or
    /// corrupt JSON) are discarded so the caller falls through to NewGame().</summary>
    public static class SaveSystem
    {
        private const string Key = "cfi_save_v2";
        private const string LegacyKey = "cfi_save_v1";
        private const int SaveVersion = 2;

        public static void Save(GameState state)
        {
            state.lastSaveUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            state.saveVersion = SaveVersion;
            string json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGlSync.FlushFileSystem();
#endif
        }

        public static GameState Load()
        {
            // Wipe any leftover legacy-key save so it doesn't linger in storage.
            if (PlayerPrefs.HasKey(LegacyKey))
            {
                PlayerPrefs.DeleteKey(LegacyKey);
                PlayerPrefs.Save();
            }

            if (!PlayerPrefs.HasKey(Key)) return null;

            GameState state;
            try
            {
                state = JsonUtility.FromJson<GameState>(PlayerPrefs.GetString(Key));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] Corrupt save discarded: " + e.Message);
                Wipe();
                return null;
            }

            if (state == null || state.saveVersion != SaveVersion)
            {
                Debug.LogWarning($"[SaveSystem] Incompatible save version ({state?.saveVersion ?? 0} != {SaveVersion}). Starting fresh.");
                Wipe();
                return null;
            }

            return state;
        }

        public static bool HasSave() => PlayerPrefs.HasKey(Key);
        public static void Wipe() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }

        /// <summary>Returns the raw save JSON (empty string if none) for clipboard export.</summary>
        public static string ExportJson() => PlayerPrefs.GetString(Key, "");

        /// <summary>Validates and stores an imported save JSON. Returns false if it can't be
        /// parsed or has an incompatible version.</summary>
        public static bool ImportJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var s = JsonUtility.FromJson<GameState>(json);
                if (s == null || s.saveVersion != SaveVersion) return false;
                PlayerPrefs.SetString(Key, json);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
