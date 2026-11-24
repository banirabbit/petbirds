$ErrorActionPreference='Stop'
$projectRoot=$PSScriptRoot
$repositoryRoot=Split-Path -Parent $projectRoot
$webRoot=Join-Path $projectRoot 'web'
New-Item -ItemType Directory -Force -Path (Join-Path $webRoot 'ui'),(Join-Path $webRoot 'shared') | Out-Null
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'shared/effects.js') -Destination (Join-Path $webRoot 'shared/effects.js')
Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'desktop/ui') -File | ForEach-Object {
    $destination=Join-Path $webRoot ('ui/'+$_.Name)
    if($_.Extension -eq '.html') {
        $html=[IO.File]::ReadAllText($_.FullName)
        $html=$html.Replace('<script src="../shared/effects.js">','<script src="bridge.js"></script><script src="../shared/effects.js">')
        [IO.File]::WriteAllText($destination,$html,(New-Object Text.UTF8Encoding($false)))
    } else {Copy-Item -LiteralPath $_.FullName -Destination $destination}
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'bridge.js') -Destination (Join-Path $webRoot 'ui/bridge.js')
