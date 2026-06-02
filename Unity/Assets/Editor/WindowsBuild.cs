using System.IO;
using UnityEditor;

namespace InsaneMonopoly.Editor
{
    public static class WindowsBuild
    {
        private const string OutputDirectory = "Builds/Windows";
        private const string ExecutableName = "InsaneMonopoly.exe";

        [MenuItem("Insane Monopoly/Build Windows EXE")]
        public static void BuildWindowsExe()
        {
            Directory.CreateDirectory(OutputDirectory);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = Path.Combine(OutputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildPipeline.BuildPlayer(options);
        }
    }
}
