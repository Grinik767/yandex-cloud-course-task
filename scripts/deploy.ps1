$PSScriptRoot = Get-Location
if (Test-Path "$PSScriptRoot\init.ps1") { . "$PSScriptRoot\init.ps1" }

$ImageTag = "v$((Get-Date).Ticks)"
$ImageUri = "cr.yandex/$env:REGISTRY_ID/$env:CONTAINER_NAME`:$ImageTag"

$HtmlPath = "..\front\index.html"
$FrontVersion = "v$(((Get-Item $HtmlPath).LastWriteTime).Ticks)";

docker build --provenance=false -t $ImageUri ..\src\YandexCloudCourseTask.API
docker push $ImageUri

yc serverless container revision deploy `
  --container-name $env:CONTAINER_NAME `
  --image $ImageUri `
  --cores 1 `
  --memory 256MB `
  --concurrency 1 `
  --execution-timeout 30s `
  --service-account-id $env:SERVICE_ACCOUNT_ID `
  --environment "DB_ENDPOINT=$env:DB_ENDPOINT,DB_NAME=$env:DB_NAME,BACKEND_VERSION=$ImageTag,ASPNETCORE_URLS=http://+:8080" `
  --folder-id $env:FOLDER_ID

$HtmlContent = Get-Content $HtmlPath -Raw -Encoding UTF8
$HtmlContent = $HtmlContent.Replace('${FRONT_VERSION}', $FrontVersion)
$TempHtmlPath = "..\front\index_rendered.html"
Set-Content -Path $TempHtmlPath -Value $HtmlContent -Encoding UTF8

yc storage s3 cp $TempHtmlPath "s3://$env:BUCKET_NAME/index.html" --content-type="text/html"
Remove-Item $TempHtmlPath

$ContainerId = (yc serverless container get $env:CONTAINER_NAME --format json | ConvertFrom-Json).id

$OpenApiContent = Get-Content .\openapi.yaml -Raw -Encoding UTF8
$OpenApiContent = $OpenApiContent.Replace('${CONTAINER_ID}', $ContainerId).Replace('${SERVICE_ACCOUNT_ID}', $env:SERVICE_ACCOUNT_ID).Replace('${BUCKET_NAME}', $env:BUCKET_NAME)
Set-Content -Path .\openapi_rendered.yaml -Value $OpenApiContent -Encoding UTF8

yc serverless api-gateway update --name $env:GATEWAY_NAME --spec=openapi_rendered.yaml --folder-id $env:FOLDER_ID
Remove-Item "openapi_rendered.yaml"

$Gateway = yc serverless api-gateway get $env:GATEWAY_NAME --format json | ConvertFrom-Json

$AllDomains = @()
$AllDomains += $Gateway.domain
if ($Gateway.attached_domains) {
    $AllDomains += $Gateway.attached_domains.domain
}

foreach ($d in $AllDomains) {
    Write-Host "URL: https://$d" -ForegroundColor Green
}