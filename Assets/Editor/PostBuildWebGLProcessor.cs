using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using System.Diagnostics;
using System.IO;

public static class PostBuildWebGLProcessor
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.WebGL)
            return;

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string buildOutputDir = Path.Combine(projectRoot, "WebBuilds");
        string docsDir = Path.Combine(projectRoot, "docs");

        // Step 1: Clear docs folder
        if (Directory.Exists(docsDir))
        {
            Directory.Delete(docsDir, true);
            UnityEngine.Debug.Log("🧹 Cleared old docs folder.");
        }

        // Step 2: Copy new WebGL build into docs
        CopyDirectory(buildOutputDir, docsDir);
        UnityEngine.Debug.Log("📦 Copied WebGL build to docs folder.");

        // === Step 5: Write CNAME file for GitHub Pages domain ===
        try
        {
            string cnamePath = Path.Combine(docsDir, "CNAME");
            File.WriteAllText(cnamePath, "mada.mprstudios.com");
            UnityEngine.Debug.Log("✅ CNAME file successfully written to: " + cnamePath);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("❌ Failed to write CNAME file: " + e.Message);
        }

        // Step 3: Run metadata injection script
        string scriptPath = Path.Combine(projectRoot, "BuildTools", "injectBuildMetadata.ps1");

        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogWarning($"⚠️ Post-build script not found at: {scriptPath}");
            return;
        }

        UnityEngine.Debug.Log($"🔧 Running post-build script: {scriptPath}");

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process p = Process.Start(psi);
        string output = p.StandardOutput.ReadToEnd();
        string error = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (!string.IsNullOrEmpty(output))
            UnityEngine.Debug.Log($"📝 PowerShell Output:\n{output}");

        if (!string.IsNullOrEmpty(error))
            UnityEngine.Debug.LogError($"❌ PowerShell Error:\n{error}");
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDir, file);
            string destPath = Path.Combine(destinationDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            File.Copy(file, destPath, true);
        }
    }
}
