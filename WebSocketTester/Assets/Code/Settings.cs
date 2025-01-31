using System.IO;
using UnityEngine;

namespace WebSocketTester
{
    public static class Settings
    {
        #region Private Fields
        private const string FileName = "Settings.json";
        #endregion

        #region Private Properties
        private static string DirectoryPath { get; set; }
        private static string FilePath { get; set; }
        private static SettingsState State { get; set; } = new SettingsState();
        #endregion

        #region Public Properties
        public static string ServerUrl
        {
            get => State._ServerUrl;
            set
            {
                State._ServerUrl = value;
                Save();
            }
        }
        #endregion

        #region Private Methods
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            InitFilePath();
            Load();
        }

        private static void InitFilePath()
        {
            var intermediatePath = Application.isEditor ? "Editor" : "";
            DirectoryPath = Path.Combine(Application.persistentDataPath, intermediatePath);
            FilePath = Path.Combine(DirectoryPath, FileName);
        }

        private static void Load()
        {
            if (!File.Exists(FilePath))
                return;

            var json = File.ReadAllText(FilePath);
            JsonUtility.FromJsonOverwrite(json, State);
        }

        private static void Save()
        {
            Directory.CreateDirectory(DirectoryPath!);

            var json = JsonUtility.ToJson(State, true);
            File.WriteAllText(FilePath, json);
        }
        #endregion
    }
}
