Add-Type -AssemblyName System.Drawing

$color = [System.Drawing.Color]::FromArgb(255, 0x1d, 0xb0, 0xad)

function Make-Bitmap([int]$sz) {
    $bmp = New-Object System.Drawing.Bitmap($sz, $sz)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.Dispose()
    # 8-bit style play triangle defined at 32x32 scale (2-pixel steps)
    # Each segment: rowStart, rowEnd, colStart, colEnd (in 32px coords)
    $segs = @(
        @(4,5,10,11), @(6,7,10,13), @(8,9,10,15), @(10,11,10,17),
        @(12,13,10,19), @(14,15,10,21), @(16,17,10,21), @(18,19,10,19),
        @(20,21,10,17), @(22,23,10,15), @(24,25,10,13), @(26,27,10,11)
    )
    $scale = $sz / 32.0
    foreach ($seg in $segs) {
        $y0 = [int]([Math]::Round($seg[0] * $scale))
        $y1 = [int]([Math]::Round(($seg[1]+1) * $scale)) - 1
        $x0 = [int]([Math]::Round($seg[2] * $scale))
        $x1 = [int]([Math]::Round(($seg[3]+1) * $scale)) - 1
        for ($y = $y0; $y -le [Math]::Min($y1, $sz-1); $y++) {
            for ($x = $x0; $x -le [Math]::Min($x1, $sz-1); $x++) {
                $bmp.SetPixel($x, $y, $color)
            }
        }
    }
    return $bmp
}

function Get-PngBytes($bitmap) {
    $ms = New-Object System.IO.MemoryStream
    $bitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    return , $ms.ToArray()
}

$bmp32 = Make-Bitmap 32
$bmp16 = Make-Bitmap 16
$png32 = Get-PngBytes $bmp32
$png16 = Get-PngBytes $bmp16
$bmp32.Dispose(); $bmp16.Dispose()

# Write .ico with two PNG images (32x32 and 16x16)
$ico = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter($ico)

# ICONDIR (6 bytes)
$w.Write([uint16]0)   # reserved
$w.Write([uint16]1)   # type = ICO
$w.Write([uint16]2)   # 2 images

# Offsets: header(6) + 2 entries(32) = 38
$off32 = [uint32]38
$off16 = [uint32]($off32 + $png32.Length)

# ICONDIRENTRY 32x32
$w.Write([byte]32);  $w.Write([byte]32);  $w.Write([byte]0); $w.Write([byte]0)
$w.Write([uint16]1); $w.Write([uint16]32)
$w.Write([uint32]$png32.Length); $w.Write($off32)

# ICONDIRENTRY 16x16
$w.Write([byte]16);  $w.Write([byte]16);  $w.Write([byte]0); $w.Write([byte]0)
$w.Write([uint16]1); $w.Write([uint16]32)
$w.Write([uint32]$png16.Length); $w.Write($off16)

$w.Write([byte[]]$png32)
$w.Write([byte[]]$png16)
$w.Flush()

[System.IO.File]::WriteAllBytes("$PSScriptRoot\PunyPlayer\app.ico", $ico.ToArray())
$ico.Dispose()
Write-Host "app.ico generated ($($png32.Length + $png16.Length + 38) bytes)"
