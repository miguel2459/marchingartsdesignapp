# === injectBuildMetadata.ps1 ===
param(
    [string]$BuildDir = ""
)

# Resolve build directory: use parameter if given, else default to <projectRoot>/docs/app
$projectRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrEmpty($BuildDir)) {
    $BuildDir = Join-Path $projectRoot "docs\app"
}
$BuildDir  = [System.IO.Path]::GetFullPath($BuildDir)
$indexPath = Join-Path $BuildDir "index.html"

if (!(Test-Path $indexPath)) {
    Write-Host "❌ index.html not found at $indexPath"
    exit 1
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$html      = [System.IO.File]::ReadAllText($indexPath, [System.Text.UTF8Encoding]::new($false))

# Wait for build files to appear
$buildPath     = Join-Path $BuildDir "Build"
$maxRetries    = 10
$retryDelay    = 1
$dataFile      = $null
$frameworkFile = $null
$wasmFile      = $null
$loaderFile    = $null
$ready         = $false

Write-Host "⏳ Waiting for Unity build files to finalize..."

for ($i = 0; $i -lt $maxRetries; $i++) {
    $dataFile      = Get-ChildItem $buildPath -Filter *.data          | Select-Object -First 1
    $frameworkFile = Get-ChildItem $buildPath -Filter *.framework.js  | Select-Object -First 1
    $wasmFile      = Get-ChildItem $buildPath -Filter *.wasm          | Select-Object -First 1
    $loaderFile    = Get-ChildItem $buildPath -Filter *.loader.js     | Select-Object -First 1

    $ready = $dataFile -and $frameworkFile -and $wasmFile -and $loaderFile
    if ($ready) { break }

    Write-Host "  🔄 Not ready yet... retry $($i + 1)/$maxRetries"
    Start-Sleep -Seconds $retryDelay
}

if (-not $ready) {
    Write-Host "❌ Timeout waiting for Unity build files. Aborting injection."
    exit 1
}

# Replace loader <script> tag (handles existing ?v= query string)
$html = $html -replace '<script src="Build/.*?\.loader\.js(?:\?v=[^"]*)?"></script>', `
    "<script src=`"Build/$($loaderFile.Name)?v=$timestamp`"></script>"

# Replace createUnityInstance file URLs
$html = $html `
    -replace 'dataUrl:\s*"Build/[^"]*"[^,\n]*', `
        "dataUrl: `"Build/$($dataFile.Name)?v=$timestamp&cb=`" + Date.now()" `
    -replace 'frameworkUrl:\s*"[^"]*"', `
        "frameworkUrl: `"Build/$($frameworkFile.Name)?v=$timestamp`"" `
    -replace 'codeUrl:\s*"[^"]*"', `
        "codeUrl: `"Build/$($wasmFile.Name)?v=$timestamp`""

# Replace HTML comment timestamp
$html = $html -replace '<!-- Build Timestamp: .*?-->', "<!-- Build Timestamp: $timestamp -->"

# Replace JS constant timestamp
$html = $html -replace 'const buildTimestamp = ".*?";', "const buildTimestamp = `"$timestamp`";"

# Replace existing footer div or inject if absent (multiline-safe)
$footerDiv     = "<div style=`"position:fixed; top: 10px; left: 10px; font-size:12px; color:#aaa; z-index:9999`">`n    Build Timestamp: $timestamp`n</div>"
$footerPattern = '(?s)<div style="position:fixed; top: 10px; left: 10px[^"]*"[^>]*>.*?</div>'

if ([regex]::IsMatch($html, $footerPattern)) {
    $html = [regex]::Replace($html, $footerPattern, $footerDiv)
} else {
    $html = $html -replace "</body>", "$footerDiv`n</body>"
}

# Write UTF-8 without BOM
[System.IO.File]::WriteAllText($indexPath, $html, [System.Text.UTF8Encoding]::new($false))

Write-Host "✅ index.html updated with timestamp $timestamp"
