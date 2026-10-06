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
        string webBuildsDir = Path.Combine(projectRoot, "WebBuilds");
        string docsDir     = Path.Combine(projectRoot, "docs");
        string appDir      = Path.Combine(docsDir, "app");

        // Guard: appDir must be exactly <docsDir>/app — refuse to delete anything else
        string normalizedApp  = Path.GetFullPath(appDir);
        string expectedApp    = Path.Combine(Path.GetFullPath(docsDir), "app");
        if (!string.Equals(normalizedApp, expectedApp, System.StringComparison.OrdinalIgnoreCase))
        {
            UnityEngine.Debug.LogError($"❌ PostBuildWebGLProcessor: Unexpected app path '{normalizedApp}'. Aborting to protect docs/.");
            return;
        }

        // Guard: only proceed if WebBuilds/Build/ contains a .loader.js
        string webBuildsBuildDir = Path.Combine(webBuildsDir, "Build");
        if (!Directory.Exists(webBuildsBuildDir) || Directory.GetFiles(webBuildsBuildDir, "*.loader.js").Length == 0)
        {
            UnityEngine.Debug.LogError("❌ PostBuildWebGLProcessor: No .loader.js found in WebBuilds/Build/. Aborting to protect docs/.");
            return;
        }

        // Step 1: Clear only docs/app/
        if (Directory.Exists(appDir))
        {
            Directory.Delete(appDir, true);
            UnityEngine.Debug.Log("🧹 Cleared old docs/app folder.");
        }

        // Step 2: Copy WebGL build into docs/app/
        CopyDirectory(webBuildsDir, appDir);
        UnityEngine.Debug.Log("📦 Copied WebGL build to docs/app.");

        // Step 3: Copy WebBuilds/favicon/ → docs/favicon/ (replace only that folder)
        string srcFavicon = Path.Combine(webBuildsDir, "favicon");
        string dstFavicon = Path.Combine(docsDir, "favicon");
        if (Directory.Exists(srcFavicon))
        {
            if (Directory.Exists(dstFavicon))
                Directory.Delete(dstFavicon, true);
            CopyDirectory(srcFavicon, dstFavicon);
            UnityEngine.Debug.Log("🖼️ Updated docs/favicon from build output.");
        }

        // Step 4: Write CNAME for GitHub Pages custom domain
        try
        {
            string cnamePath = Path.Combine(docsDir, "CNAME");
            File.WriteAllText(cnamePath, "mada.mprstudios.com");
            UnityEngine.Debug.Log("✅ CNAME written to: " + cnamePath);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("❌ Failed to write CNAME: " + e.Message);
        }

        // Step 5: Run metadata injection script, passing docs/app as the target
        string scriptPath = Path.Combine(projectRoot, "BuildTools", "injectBuildMetadata.ps1");

        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogWarning($"⚠️ Post-build script not found at: {scriptPath}");
            return;
        }

        UnityEngine.Debug.Log($"🔧 Running post-build script: {scriptPath}");

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName  = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -BuildDir \"{appDir}\"",
            CreateNoWindow       = true,
            UseShellExecute      = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };

        using (Process p = Process.Start(psi))
        {
            string output = p.StandardOutput.ReadToEnd();
            string error  = p.StandardError.ReadToEnd();
            p.WaitForExit();

            if (!string.IsNullOrEmpty(output))
                UnityEngine.Debug.Log($"📝 PowerShell Output:\n{output}");

            if (!string.IsNullOrEmpty(error))
                UnityEngine.Debug.LogError($"❌ PowerShell Error:\n{error}");
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDir, file);
            string destPath     = Path.Combine(destinationDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            File.Copy(file, destPath, true);
        }
    }
}
