$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$pngPath = Join-Path $repoRoot "frontend\src\assets\brand\bazarflow-icon.png"
$icoDir = Join-Path $repoRoot "packaging\assets"
$icoPath = Join-Path $icoDir "bazarflow-icon.ico"

if (-not (Test-Path $icoDir)) {
    New-Item -ItemType Directory -Path $icoDir -Force | Out-Null
}

if (-not (Test-Path $pngPath)) {
    Write-Error "Source PNG not found at $pngPath"
}

Write-Host "Converting PNG to a high-quality 256x256 ICO..."

try {
    Add-Type -AssemblyName System.Drawing
    
    # Load original image
    $original = [System.Drawing.Bitmap]::FromFile($pngPath)
    
    # Create 256x256 destination bitmap
    $resized = [System.Drawing.Bitmap]::new(256, 256)
    $graphics = [System.Drawing.Graphics]::FromImage($resized)
    
    # Set high quality rendering settings
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    
    # Draw original image into resized bitmap
    $graphics.DrawImage($original, 0, 0, 256, 256)
    
    # Save resized bitmap as PNG to a memory stream
    $ms = [System.IO.MemoryStream]::new()
    $resized.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes = $ms.ToArray()
    
    # Clean up Drawing resources
    $graphics.Dispose()
    $resized.Dispose()
    $original.Dispose()
    $ms.Dispose()
    
    # Write ICO file
    $fileStream = [System.IO.FileStream]::new($icoPath, [System.IO.FileMode]::Create)
    $binaryWriter = [System.IO.BinaryWriter]::new($fileStream)
    
    # 1. ICONDIR Header (6 bytes)
    $binaryWriter.Write([UInt16]0)      # Reserved
    $binaryWriter.Write([UInt16]1)      # Type (1 = Icon)
    $binaryWriter.Write([UInt16]1)      # Count (1 image)
    
    # 2. ICONDIRENTRY (16 bytes)
    $binaryWriter.Write([Byte]0)        # Width (0 means 256)
    $binaryWriter.Write([Byte]0)        # Height (0 means 256)
    $binaryWriter.Write([Byte]0)        # Color count (0 for >= 256 colors)
    $binaryWriter.Write([Byte]0)        # Reserved
    $binaryWriter.Write([UInt16]1)      # Color planes (1)
    $binaryWriter.Write([UInt16]32)     # Bits per pixel (32)
    $binaryWriter.Write([UInt32]$pngBytes.Length) # Image size in bytes
    $binaryWriter.Write([UInt32]22)     # Image offset (6 + 16 = 22)
    
    # 3. PNG Image Data
    $binaryWriter.Write($pngBytes)
    
    # Close writers and stream
    $binaryWriter.Close()
    $fileStream.Close()
    
    Write-Host "Success! Icon saved to $icoPath"
}
catch {
    Write-Warning "Could not convert icon automatically: $_"
    Write-Host "Please manually convert $pngPath to a .ico file and place it at $icoPath"
}
