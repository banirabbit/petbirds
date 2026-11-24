$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$assets=Join-Path (Split-Path -Parent $PSScriptRoot) 'assets'
$image=[Drawing.Image]::FromFile((Join-Path $assets 'robin-source.png'))
try {
    $frames=@()
    foreach($size in @(16,24,32,48,64,128,256)) {
        $bitmap=New-Object Drawing.Bitmap($size,$size,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics=[Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([Drawing.Color]::Transparent)
            $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
            $graphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
            $graphics.DrawImage($image,(New-Object Drawing.Rectangle(0,0,$size,$size)))
            $stream=New-Object IO.MemoryStream
            try {
                $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
                $frames+=@{Size=$size;Bytes=$stream.ToArray()}
                if($size -eq 256){[IO.File]::WriteAllBytes((Join-Path $assets 'robin.png'),$stream.ToArray())}
                if($size -eq 32){[IO.File]::WriteAllBytes((Join-Path $assets 'robin-32.png'),$stream.ToArray())}
            } finally {$stream.Dispose()}
        } finally {$graphics.Dispose();$bitmap.Dispose()}
    }
    $file=[IO.File]::Create((Join-Path $assets 'robin.ico'))
    $writer=New-Object IO.BinaryWriter($file)
    try {
        $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$frames.Count)
        $offset=6+16*$frames.Count
        foreach($frame in $frames) {
            $dimension=if($frame.Size -eq 256){0}else{$frame.Size}
            $writer.Write([byte]$dimension);$writer.Write([byte]$dimension)
            $writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32)
            $writer.Write([uint32]$frame.Bytes.Length);$writer.Write([uint32]$offset)
            $offset+=$frame.Bytes.Length
        }
        foreach($frame in $frames){$writer.Write([byte[]]$frame.Bytes)}
    } finally {$writer.Dispose();$file.Dispose()}
} finally {$image.Dispose()}
