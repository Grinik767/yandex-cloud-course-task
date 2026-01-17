$PSScriptRoot = Get-Location
if (Test-Path "$PSScriptRoot\init.ps1") { . "$PSScriptRoot\init.ps1" }

$ImageTag = "migration-$((Get-Date).Ticks)"
$ImageUri = "cr.yandex/$env:REGISTRY_ID/$env:CONTAINER_NAME`:$ImageTag"

docker build -q --provenance=false -t $ImageUri ..\src\YandexCloudCourseTask.API
docker push $ImageUri

yc serverless container revision deploy `
  --container-name $env:CONTAINER_NAME `
  --image $ImageUri `
  --service-account-id $env:SERVICE_ACCOUNT_ID `
  --environment "DB_ENDPOINT=$env:DB_ENDPOINT,DB_NAME=$env:DB_NAME" `
  --command "dotnet" `
  --args "YandexCloudCourseTask.API.dll" `
  --args "--migrate" `
  --folder-id $env:FOLDER_ID

$Domain = (yc serverless api-gateway get $env:GATEWAY_NAME --format json | ConvertFrom-Json).domain
$Url = "https://$Domain/api/initial-state"
try { Invoke-WebRequest -Uri $Url -Method Get -UseBasicParsing -TimeoutSec 15 >> $null} catch { }

Write-Host "Migration started"