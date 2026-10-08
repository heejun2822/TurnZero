$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$artDirectory = Join-Path $PSScriptRoot '../../TurnZero/Assets/Resources/PrototypeUI'
[System.IO.Directory]::CreateDirectory($artDirectory) | Out-Null
foreach ($iconName in @('move', 'attack', 'skill', 'item', 'wait', 'castle', 'undo', 'blade', 'bow', 'badge')) {
    $bitmap = [System.Drawing.Bitmap]::new(64, 64)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, 4)
    $brush = [System.Drawing.Brushes]::White
    function Points($coordinates) {
        ,[System.Drawing.PointF[]]@(for ($i = 0; $i -lt $coordinates.Length; $i += 2) {
            [System.Drawing.PointF]::new($coordinates[$i], $coordinates[$i + 1])
        })
    }
    switch ($iconName) {
        move { $graphics.FillPolygon($brush, (Points @(7,24,34,24,34,12,57,32,34,52,34,40,7,40))) }
        attack {
            $graphics.DrawEllipse($pen, 13, 13, 38, 38)
            $graphics.DrawLine($pen, 32, 5, 32, 21); $graphics.DrawLine($pen, 32, 43, 32, 59)
            $graphics.DrawLine($pen, 5, 32, 21, 32); $graphics.DrawLine($pen, 43, 32, 59, 32)
        }
        skill {
            $graphics.FillPolygon($brush, (Points @(28,6,35,22,51,29,35,36,28,52,21,36,5,29,21,22)))
            $graphics.FillPolygon($brush, (Points @(51,39,54,46,61,49,54,52,51,59,48,52,41,49,48,46)))
        }
        item {
            $graphics.DrawLines($pen, (Points @(24,8,40,8,40,15,36,15,36,29,48,43,48,50,40,57,24,57,16,50,16,43,28,29,28,15,24,15,24,8)))
            $graphics.FillPolygon($brush, (Points @(22,43,42,43,40,50,24,50)))
        }
        wait {
            $graphics.DrawLines($pen, (Points @(17,9,47,9,47,17,35,32,47,47,47,55,17,55,17,47,29,32,17,17,17,9)))
            $graphics.DrawLine($pen, 21, 13, 43, 13); $graphics.DrawLine($pen, 21, 51, 43, 51)
        }
        castle {
            $graphics.FillPolygon($brush, (Points @(8,56,11,13,20,13,20,6,27,6,27,16,37,16,37,6,44,6,44,13,53,13,56,56,37,56,37,38,27,38,27,56)))
        }
        undo {
            $graphics.DrawArc($pen, 14, 15, 38, 38, 205, 290)
            $graphics.FillPolygon($brush, (Points @(8,26,24,28,18,10)))
        }
        blade {
            $graphics.FillPolygon($brush, (Points @(15,47,43,8,55,6,52,19,22,50)))
            $graphics.DrawLine($pen, 9, 39, 30, 54); $graphics.DrawLine($pen, 11, 59, 21, 47)
        }
        bow {
            $graphics.DrawArc($pen, 4, 6, 45, 52, 270, 180)
            $graphics.DrawLine($pen, 27, 6, 27, 58); $graphics.DrawLine($pen, 17, 42, 52, 16)
            $graphics.DrawLines($pen, (Points @(42,17,52,16,50,26)))
        }
        badge {
            $graphics.FillEllipse([System.Drawing.Brushes]::Black, 3, 3, 58, 58)
            $graphics.DrawEllipse($pen, 4, 4, 56, 56)
        }
    }
    $bitmap.Save((Join-Path $artDirectory "$iconName.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $pen.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
