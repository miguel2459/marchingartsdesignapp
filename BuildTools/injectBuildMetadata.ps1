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

if (!$dataFile -or !$frameworkFile -or !$wasmFile) {
    Write-Host "❌ Missing one or more build files."
    exit 1
}

$html = $html `
    -replace 'dataUrl:\s*".*?"', "dataUrl: `"Build/$($dataFile.Name)?v=$timestamp`"" `
    -replace 'frameworkUrl:\s*".*?"', "frameworkUrl: `"Build/$($frameworkFile.Name)?v=$timestamp`"" `
    -replace 'codeUrl:\s*".*?"', "codeUrl: `"Build/$($wasmFile.Name)?v=$timestamp`""


# Inject footer
$footerHtml = @"
    <div style='position:fixed; bottom: 10px; right: 10px; font-size:12px; color:#aaa; z-index:9999'>
        Build Timestamp: $timestamp
    </div>
"@

$html = $html -replace "</body>", "$footerHtml`n</body>"

# Write updated HTML
Set-Content -Path $indexPath -Value $html

Write-Host "✅ index.html updated with timestamp and version query strings."
