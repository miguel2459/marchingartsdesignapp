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

$html = $html `
    -replace 'dataUrl:\s*"Build/(.*?\.data)"', "dataUrl: `"Build/`$1?v=$timestamp`"" `
    -replace 'frameworkUrl:\s*"Build/(.*?\.framework\.js)"', "frameworkUrl: `"Build/`$1?v=$timestamp`"" `
    -replace 'codeUrl:\s*"Build/(.*?\.wasm)"', "codeUrl: `"Build/`$1?v=$timestamp`""

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
