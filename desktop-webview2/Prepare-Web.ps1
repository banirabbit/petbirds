$ErrorActionPreference='Stop'
$projectRoot=$PSScriptRoot
$repositoryRoot=Split-Path -Parent $projectRoot
$webRoot=[IO.Path]::GetFullPath((Join-Path $projectRoot 'web'))
if((Split-Path -Parent $webRoot) -ne [IO.Path]::GetFullPath($projectRoot)){throw '生成资源目录必须位于项目内'}
if(Test-Path -LiteralPath $webRoot){Remove-Item -LiteralPath $webRoot -Recurse -Force}
New-Item -ItemType Directory -Force -Path (Join-Path $webRoot 'ui'),(Join-Path $webRoot 'shared') | Out-Null
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'shared/effects.js') -Destination (Join-Path $webRoot 'shared/effects.js')
foreach($name in @('overlay.html','overlay.css','overlay.js')) {
    $source=Get-Item -LiteralPath (Join-Path $projectRoot ('ui/'+$name))
    $destination=Join-Path $webRoot ('ui/'+$source.Name)
    if($source.Extension -eq '.html') {
        $html=[IO.File]::ReadAllText($source.FullName)
        $html=$html.Replace('<script src="../shared/effects.js">','<script src="bridge.js"></script><script src="../shared/effects.js">')
        [IO.File]::WriteAllText($destination,$html,(New-Object Text.UTF8Encoding($false)))
    } else {Copy-Item -LiteralPath $source.FullName -Destination $destination}
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'bridge.js') -Destination (Join-Path $webRoot 'ui/bridge.js')
