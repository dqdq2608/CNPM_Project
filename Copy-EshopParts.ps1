$ErrorActionPreference='Stop'
$EshopSrc = "D:\eShop\src"
$DestRoot = "D:\CNPM_Project\jojos-burger-BE"

$OrderingApiSrc   = Join-Path $EshopSrc "Ordering.API"
$OrderingDomSrc   = Join-Path $EshopSrc "Ordering.Domain"
$OrderingInfraSrc = Join-Path $EshopSrc "Ordering.Infrastructure"
$BasketApiSrc     = Join-Path $EshopSrc "Basket.API"

$OrderingDst = Join-Path $DestRoot "services\ordering"
$BasketDst   = Join-Path $DestRoot "services\basket"

if (!(Test-Path $OrderingApiSrc))   { throw "Not found: $OrderingApiSrc" }
if (!(Test-Path $OrderingDomSrc))   { throw "Not found: $OrderingDomSrc" }
if (!(Test-Path $OrderingInfraSrc)) { throw "Not found: $OrderingInfraSrc" }
if (!(Test-Path $BasketApiSrc))     { throw "Not found: $BasketApiSrc" }

New-Item -ItemType Directory -Force -Path $OrderingDst,$BasketDst | Out-Null

robocopy $OrderingApiSrc   (Join-Path $OrderingDst "Ordering.API")            /MIR | Out-Null
robocopy $OrderingDomSrc   (Join-Path $OrderingDst "Ordering.Domain")         /MIR | Out-Null
robocopy $OrderingInfraSrc (Join-Path $OrderingDst "Ordering.Infrastructure") /MIR /XD Migrations | Out-Null
robocopy $BasketApiSrc     (Join-Path $BasketDst   "Basket.API")              /MIR | Out-Null

$testFile = Join-Path $OrderingDst "Ordering.API\Program.Testing.cs"
if (Test-Path $testFile) { Remove-Item $testFile -Force }

"Done. Copied to $DestRoot\services\{ordering,basket}"
