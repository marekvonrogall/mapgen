$folders = @("block", "item")
$jsonData = @{}

foreach ($folder in $folders) {
    if (-not (Test-Path -Path $folder)) {
        Write-Host "Folder '$folder' does not exist. Skipping..." -ForegroundColor Yellow
        continue
    }

    $jsonData[$folder] = @()

    $files = Get-ChildItem -Path $folder -Filter "*.png" -Recurse | Where-Object {
        $_.Name -notmatch "bottom|top|fence|particle"
    }

    foreach ($file in $files) {
        $jsonData[$folder] += [PSCustomObject]@{
            name       = $file.BaseName
            difficulty = "easy"
        }
    }
}

$jsonString = $jsonData | ConvertTo-Json -Depth 10
$outputFile = "output.json"
$jsonString | Set-Content -Path $outputFile

Write-Host "JSON file has been generated: $outputFile" -ForegroundColor Green