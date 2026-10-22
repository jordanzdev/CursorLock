<#
.SYNOPSIS
    Generates the CursorLock branding in assets\: CursorLock.ico (16–256 px), logo.svg and the
    installer bitmaps (installer-banner.bmp, installer-dialog.bmp).

.DESCRIPTION
    The logo is a white mouse cursor inside red corner brackets (a confined cursor) on a dark
    rounded square. All outputs come from the same geometry, defined below on a 256-unit grid.
    Sizes up to 24 px use a bolder variant so the shape stays readable in the taskbar.
    Requires Windows PowerShell 5.1 (System.Drawing).
#>

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$assets = Join-Path (Split-Path $PSScriptRoot -Parent) 'assets'
New-Item -ItemType Directory -Force $assets | Out-Null

$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$smallSizeLimit = 24

$colors = @{
    BackgroundTop    = '#2B313D'
    BackgroundBottom = '#161A21'
    Bracket          = '#E53935'
    Cursor           = '#FFFFFF'
    CursorOutline    = '#161A21'
}

# Classic arrow cursor, tip at (0, 0), 72 x 112 units.
$cursorShape = @(@(0, 0), @(0, 100), @(24, 76), @(40, 112), @(56, 104), @(40, 70), @(72, 70))

$variants = @{
    Regular = @{ Inset = 8; Radius = 48; Frame = 56; FrameEnd = 200; Arm = 40; Stroke = 16; CursorX = 98; CursorY = 80; CursorScale = 0.95; Outline = 6 }
    Bold    = @{ Inset = 0; Radius = 44; Frame = 38; FrameEnd = 218; Arm = 54; Stroke = 26; CursorX = 92; CursorY = 70; CursorScale = 1.1; Outline = 8 }
}

function Get-CursorPoints($v) {
    foreach ($point in $cursorShape) {
        , @(($v.CursorX + $point[0] * $v.CursorScale), ($v.CursorY + $point[1] * $v.CursorScale))
    }
}

function Get-BracketLines($v) {
    $a = $v.Frame; $b = $v.FrameEnd; $arm = $v.Arm
    , @(@($a, ($a + $arm)), @($a, $a), @(($a + $arm), $a))   # top left
    , @(@(($b - $arm), $a), @($b, $a), @($b, ($a + $arm)))   # top right
    , @(@($b, ($b - $arm)), @($b, $b), @(($b - $arm), $b))   # bottom right
    , @(@(($a + $arm), $b), @($a, $b), @($a, ($b - $arm)))   # bottom left
}

function New-RoundedSquare([float] $inset, [float] $radius) {
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $size = 256 - 2 * $inset
    $diameter = 2 * $radius
    $far = $inset + $size - $diameter
    $path.AddArc($inset, $inset, $diameter, $diameter, 180, 90)
    $path.AddArc($far, $inset, $diameter, $diameter, 270, 90)
    $path.AddArc($far, $far, $diameter, $diameter, 0, 90)
    $path.AddArc($inset, $far, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    $path
}

function ConvertTo-PointArray($points) {
    [Drawing.PointF[]] ($points | ForEach-Object { New-Object Drawing.PointF ([float] $_[0]), ([float] $_[1]) })
}

function New-LogoBitmap([int] $size) {
    $v = if ($size -le $smallSizeLimit) { $variants.Bold } else { $variants.Regular }
    $bitmap = New-Object Drawing.Bitmap $size, $size, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.ScaleTransform($size / 256, $size / 256)

    $background = New-RoundedSquare $v.Inset $v.Radius
    $gradient = New-Object Drawing.Drawing2D.LinearGradientBrush (New-Object Drawing.PointF 0, 0), (New-Object Drawing.PointF 0, 256),
        ([Drawing.ColorTranslator]::FromHtml($colors.BackgroundTop)), ([Drawing.ColorTranslator]::FromHtml($colors.BackgroundBottom))
    $g.FillPath($gradient, $background)

    $bracketPen = New-Object Drawing.Pen ([Drawing.ColorTranslator]::FromHtml($colors.Bracket)), $v.Stroke
    $bracketPen.StartCap = 'Round'; $bracketPen.EndCap = 'Round'; $bracketPen.LineJoin = 'Round'
    foreach ($line in Get-BracketLines $v) { $g.DrawLines($bracketPen, (ConvertTo-PointArray $line)) }

    $cursor = ConvertTo-PointArray (Get-CursorPoints $v)
    $outlinePen = New-Object Drawing.Pen ([Drawing.ColorTranslator]::FromHtml($colors.CursorOutline)), ($v.Outline * 2)
    $outlinePen.LineJoin = 'Round'
    $g.DrawPolygon($outlinePen, $cursor)
    $g.FillPolygon((New-Object Drawing.SolidBrush ([Drawing.ColorTranslator]::FromHtml($colors.Cursor))), $cursor)

    $g.Dispose()
    $bitmap
}

function New-LogoPng([int] $size) {
    $bitmap = New-LogoBitmap $size
    $stream = New-Object IO.MemoryStream
    $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    , $stream.ToArray()
}

function Write-Ico([string] $path, [int[]] $iconSizes) {
    $images = foreach ($size in $iconSizes) { , (New-LogoPng $size) }
    $stream = [IO.File]::Create($path)
    $writer = New-Object IO.BinaryWriter $stream
    $writer.Write([uint16] 0)                  # reserved
    $writer.Write([uint16] 1)                  # type: icon
    $writer.Write([uint16] $iconSizes.Count)
    $offset = 6 + 16 * $iconSizes.Count
    for ($i = 0; $i -lt $iconSizes.Count; $i++) {
        $dimension = if ($iconSizes[$i] -ge 256) { 0 } else { $iconSizes[$i] } # 0 means 256
        $writer.Write([byte] $dimension)
        $writer.Write([byte] $dimension)
        $writer.Write([byte] 0)                # palette colors
        $writer.Write([byte] 0)                # reserved
        $writer.Write([uint16] 1)              # planes
        $writer.Write([uint16] 32)             # bits per pixel
        $writer.Write([uint32] $images[$i].Length)
        $writer.Write([uint32] $offset)
        $offset += $images[$i].Length
    }
    foreach ($image in $images) { $writer.Write($image) }
    $writer.Dispose()
}

function Write-Svg([string] $path) {
    $v = $variants.Regular
    $culture = [Globalization.CultureInfo]::InvariantCulture
    $format = { param($points) ($points | ForEach-Object { [string]::Format($culture, '{0:0.##},{1:0.##}', $_[0], $_[1]) }) -join ' ' }
    $brackets = (Get-BracketLines $v | ForEach-Object { "    <polyline points=`"$(& $format $_)`" />" }) -join "`n"
    $side = 256 - 2 * $v.Inset
    $svg = @"
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">
  <title>CursorLock</title>
  <defs>
    <linearGradient id="background" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="$($colors.BackgroundTop)" />
      <stop offset="1" stop-color="$($colors.BackgroundBottom)" />
    </linearGradient>
  </defs>
  <rect x="$($v.Inset)" y="$($v.Inset)" width="$side" height="$side" rx="$($v.Radius)" fill="url(#background)" />
  <g fill="none" stroke="$($colors.Bracket)" stroke-width="$($v.Stroke)" stroke-linecap="round" stroke-linejoin="round">
$brackets
  </g>
  <polygon points="$(& $format (Get-CursorPoints $v))" fill="$($colors.Cursor)" stroke="$($colors.CursorOutline)" stroke-width="$($v.Outline * 2)" stroke-linejoin="round" paint-order="stroke" />
</svg>
"@
    [IO.File]::WriteAllText($path, $svg + "`n", (New-Object Text.UTF8Encoding $false))
}

# Installer (WixUI) bitmaps: 24-bit BMP at the sizes WiX expects.
function New-InstallerCanvas([int] $width, [int] $height) {
    $bitmap = New-Object Drawing.Bitmap $width, $height, ([Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear([Drawing.Color]::White)
    $bitmap, $g
}

function Save-Bmp($bitmap, $g, [string] $path) {
    $g.Dispose()
    $bitmap.Save($path, [Drawing.Imaging.ImageFormat]::Bmp)
    $bitmap.Dispose()
}

# Top banner of the progress and files-in-use pages: white, logo on the right.
function Write-InstallerBanner([string] $path) {
    $bitmap, $g = New-InstallerCanvas 493 58
    $logo = New-LogoBitmap 46
    $g.DrawImage($logo, 493 - 46 - 8, 6, 46, 46)
    $logo.Dispose()
    Save-Bmp $bitmap $g $path
}

# Side panel of the final page: dark strip with the logo and the name; the text area stays white.
function Write-InstallerDialog([string] $path) {
    $panelWidth = 164
    $bitmap, $g = New-InstallerCanvas 493 312
    $panel = New-Object Drawing.Drawing2D.LinearGradientBrush (New-Object Drawing.PointF 0, 0), (New-Object Drawing.PointF 0, 312),
        ([Drawing.ColorTranslator]::FromHtml($colors.BackgroundTop)), ([Drawing.ColorTranslator]::FromHtml($colors.BackgroundBottom))
    $g.FillRectangle($panel, 0, 0, $panelWidth, 312)

    $logoSize = 104
    $logo = New-LogoBitmap $logoSize
    $g.DrawImage($logo, ($panelWidth - $logoSize) / 2, 56, $logoSize, $logoSize)
    $logo.Dispose()

    $font = New-Object Drawing.Font 'Segoe UI Semibold', 13, ([Drawing.FontStyle]::Regular), ([Drawing.GraphicsUnit]::Point)
    $format = New-Object Drawing.StringFormat
    $format.Alignment = 'Center'
    $g.DrawString('CursorLock', $font, [Drawing.Brushes]::White, (New-Object Drawing.RectangleF 0, 176, $panelWidth, 40), $format)
    Save-Bmp $bitmap $g $path
}

Write-Ico (Join-Path $assets 'CursorLock.ico') $sizes
Write-Svg (Join-Path $assets 'logo.svg')
Write-InstallerBanner (Join-Path $assets 'installer-banner.bmp')
Write-InstallerDialog (Join-Path $assets 'installer-dialog.bmp')
Write-Host "Written to ${assets}: CursorLock.ico ($($sizes -join ', ') px), logo.svg, installer-banner.bmp, installer-dialog.bmp"
