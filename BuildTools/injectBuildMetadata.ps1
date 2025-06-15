# === injectBuildMetadata.ps1 ===
$buildDir = "docs"
$indexPath = Join-Path $buildDir "index.html"

# Ensure file exists
if (!(Test-Path $indexPath)) {
    Write-Host "❌ index.html not found in $buildDir"
    exit 1
}

# Get timestamp
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

# Update .js/.wasm/.data file references with version string
$html = Get-Content $indexPath -Raw

# Detect actual filenames
$dataFile = Get-ChildItem "$buildDir/Build" -Filter *.data | Select-Object -First 1
$frameworkFile = Get-ChildItem "$buildDir/Build" -Filter *.framework.js | Select-Object -First 1
$wasmFile = Get-ChildItem "$buildDir/Build" -Filter *.wasm | Select-Object -First 1
$loaderFile = Get-ChildItem "$buildDir/Build" -Filter *.loader.js | Select-Object -First 1

if (!$dataFile -or !$frameworkFile -or !$wasmFile -or !$loaderFile) {
    Write-Host "❌ Missing one or more build files."
    exit 1
}

# Replace Unity loader script tag
$html = $html -replace '<script src="Build/.*?\.loader\.js"></script>', "<script src=`"Build/$($loaderFile.Name)?v=$timestamp`"></script>"

# Replace file references inside createUnityInstance
$html = $html `
    -replace 'dataUrl:\s*".*?"', "dataUrl: `"Build/$($dataFile.Name)?v=$timestamp`"" `
    -replace 'frameworkUrl:\s*".*?"', "frameworkUrl: `"Build/$($frameworkFile.Name)?v=$timestamp`"" `
    -replace 'codeUrl:\s*".*?"', "codeUrl: `"Build/$($wasmFile.Name)?v=$timestamp`""

# Replace build timestamp comment
$html = $html -replace '<!-- Build Timestamp: .*?-->', "<!-- Build Timestamp: $timestamp -->"

# Inject footer
$footerHtml = @"
<div style="position:fixed; top: 10px; left: 10px; font-size:12px; color:#aaa; z-index:9999">
    Build Timestamp: $timestamp
</div>
"@

$html = $html -replace "</body>", "$footerHtml`n</body>"

# Inject or replace JavaScript build timestamp constant
if ($html -match 'const buildTimestamp = ".*?";') {
    $html = $html -replace 'const buildTimestamp = ".*?";', "const buildTimestamp = `"$timestamp`";"
}

# Write updated HTML
Set-Content -Path $indexPath -Value $html

Write-Host "✅ index.html updated with timestamp and version query strings."
