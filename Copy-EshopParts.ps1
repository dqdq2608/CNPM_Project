Param(
  [string]$EshopSrc = "$HOME\work\eShop",
  [string]$DestRoot = "$HOME\work\jojos-burger",
  [bool]$RewriteNamespace = $true,
  [string]$FromNs = "eShop",
  [string]$ToNs = "JoJosBurger"
)

$BeDir = Join-Path $DestRoot "jojos-burger-BE"
$OrderingSrc = Join-Path $EshopSrc "src\Services\Order­ing"
$BasketSrc   = Join-Path $EshopSrc "src\Services\Basket"

$OrderingApiSrc  = Join-Path $OrderingSrc "Ordering.API"
$OrderingDomSrc  = Join-Path $OrderingSrc "Ordering.Domain"
$OrderingInfraSrc= Join-Path $OrderingSrc "Ordering.Infrastructure"
$BasketApiSrc    = Join-Path $BasketSrc "Basket.API"

foreach ($p in @($OrderingApiSrc,$OrderingDomSrc,$OrderingInfraSrc,$BasketApiSrc)) {
  if (!(Test-Path $p)) { Write-Error "Không tìm thấy: $p"; exit 1 }
}

$OrderingDst = Join-Path $BeDir "services\ordering"
$BasketDst   = Join-Path $BeDir "services\basket"
New-Item -ItemType Directory -Force -Path $OrderingDst,$BasketDst | Out-Null

Write-Host "➡️  Copy Ordering.API ..."
robocopy $OrderingApiSrc (Join-Path $OrderingDst "Ordering.API") /MIR | Out-Null

Write-Host "➡️  Copy Ordering.Domain ..."
robocopy $OrderingDomSrc (Join-Path $OrderingDst "Ordering.Domain") /MIR | Out-Null

Write-Host "➡️  Copy Ordering.Infrastructure (bỏ Migrations/)..."
$OrderingInfraDst = Join-Path $OrderingDst "Ordering.Infrastructure"
robocopy $OrderingInfraSrc $OrderingInfraDst /MIR /XD Migrations | Out-Null

Write-Host "➡️  Copy Basket.API ..."
robocopy $BasketApiSrc (Join-Path $BasketDst "Basket.API") /MIR | Out-Null

# Xoá file testing nếu có
$testing = Join-Path $OrderingDst "Ordering.API\Program.Testing.cs"
if (Test-Path $testing) { Remove-Item $testing -Force }

if ($RewriteNamespace) {
  Write-Host "🔁 Đổi namespace: $FromNs → $ToNs ..."
  $files = Get-ChildItem $OrderingDst,$BasketDst -Recurse -Include *.cs,*.csproj
  foreach ($f in $files) {
    (Get-Content $f.FullName) -replace "\b$([regex]::Escape($FromNs))\b",$ToNs | Set-Content $f.FullName
  }
}

Write-Host "✅ Hoàn tất."
Write-Host "📁 Đã copy vào: $BeDir\services\{ordering,basket}"
Write-Host ""
Write-Host "👉 Tiếp theo (khuyến nghị):"
Write-Host "1) Cập nhật connection string trong Ordering.API\appsettings*.json"
Write-Host "2) Tạo migrations mới:"
Write-Host "   cd $($OrderingDst)\Ordering.Infrastructure"
Write-Host "   dotnet ef migrations add Init -s ..\Ordering.API\Order­ing.API.csproj"
Write-Host "   dotnet ef database update -s ..\Ordering.API\Order­ing.API.csproj"
Write-Host "3) Chạy thử API:"
Write-Host "   dotnet run -p $($OrderingDst)\Ordering.API\Order­ing.API.csproj"
