# Pre-renders every Devanagari phrase used in the Unity game to a PNG, with proper
# shaping (conjuncts, matras, reph) from Windows' WPF text engine and Nirmala UI.
# Unity's built-in text renderer cannot shape Indic scripts, so the game draws these images.
#   npm run unity:devanagari     (Windows PowerShell)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$root = Split-Path -Parent $PSScriptRoot
$scripts = Join-Path $root 'unity\PhewaValley\Assets\PhewaValley\Scripts'
$out = Join-Path $root 'unity\PhewaValley\Assets\StreamingAssets\PhewaValley\deva'
New-Item -ItemType Directory -Force $out | Out-Null
Get-ChildItem $out -Filter *.png | Remove-Item

# Must match DevaText.Runs in DevaText.cs: Devanagari letters, with inner spaces, "!" and the ellipsis.
$lo = [char]0x0900; $hi = [char]0x097F; $el = [char]0x2026; $d0 = [char]0x0966; $d9 = [char]0x096F
$pattern = "[$lo-$hi][$lo-$hi !$el]*[$lo-$hi!$el]|[$lo-$hi]"
$phrases = New-Object 'System.Collections.Generic.SortedSet[string]'
foreach ($file in Get-ChildItem $scripts -Recurse -Filter *.cs) {
  $text = [IO.File]::ReadAllText($file.FullName, [Text.Encoding]::UTF8)
  foreach ($m in [regex]::Matches($text, $pattern)) { $v = $m.Value.Trim(); if ($v -notmatch "^[$d0-$d9]+$") { [void]$phrases.Add($v) } }
}

$typeface = New-Object System.Windows.Media.Typeface((New-Object System.Windows.Media.FontFamily('Nirmala UI')), [System.Windows.FontStyles]::Normal, [System.Windows.FontWeights]::Bold, [System.Windows.FontStretches]::Normal)
$culture = [Globalization.CultureInfo]::GetCultureInfo('ne-NP')
$index = @(); $i = 0
foreach ($phrase in $phrases) {
  $ft = New-Object System.Windows.Media.FormattedText($phrase, $culture, [System.Windows.FlowDirection]::LeftToRight, $typeface, 72, [System.Windows.Media.Brushes]::White, 1.0)
  $w = [Math]::Ceiling($ft.WidthIncludingTrailingWhitespace) + 12; $h = [Math]::Ceiling($ft.Height) + 8
  $dv = New-Object System.Windows.Media.DrawingVisual; $dc = $dv.RenderOpen(); $dc.DrawText($ft, (New-Object System.Windows.Point(6, 4))); $dc.Close()
  $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($w, $h, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32); $bmp.Render($dv)
  $name = ('{0:D3}.png' -f $i); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
  $fs = [IO.File]::Create((Join-Path $out $name)); $enc.Save($fs); $fs.Close()
  $index += [pscustomobject]@{ text = $phrase; file = $name; width = $w; height = $h }; $i++
}
$json = @{ phrases = $index } | ConvertTo-Json -Depth 4
[IO.File]::WriteAllText((Join-Path $out 'index.json'), $json, (New-Object Text.UTF8Encoding($false)))
Write-Output "Rendered $($index.Count) Devanagari phrases -> $out"
