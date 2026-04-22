$ErrorActionPreference = "Stop"

$ppt = New-Object -ComObject PowerPoint.Application
$ppt.Visible = [Microsoft.Office.Core.MsoTriState]::msoFalse
$pptP = Convert-Path "..\Slide*.pptx"
# Note MsoTriState true/false: msoFalse = 0, msoTrue = -1
$pres = $ppt.Presentations.Open($pptP, 0, 0, 0)

$replacements = @{
    "<= 3 ngày" = "<= 14 ngày"
    "< 3 ngày"  = "<= 14 ngày"
    "<= 3"      = "<= 14"
}

foreach ($slide in $pres.Slides) {
  foreach ($shape in $slide.Shapes) {
    if ($shape.HasTextFrame -eq -1) {
      if ($shape.TextFrame.HasText -eq -1) {
        $txtRng = $shape.TextFrame.TextRange
        foreach ($key in $replacements.Keys) {
            $value = $replacements[$key]
            # Replace case-insensitive and finding substrings
            # Need to avoid TextRange.Replace COM method bugs in PS if any, just assign Text
            if ($txtRng.Text -match (\[regex\]::Escape($key))) {
                $txtRng.Text = $txtRng.Text.Replace($key, $value)
            }
        }
      }
    }
  }
}

$pres.Save()
Start-Sleep -Seconds 2
$pres.Close()
$ppt.Quit()
Write-Host "PowerPoint updated successfully."
