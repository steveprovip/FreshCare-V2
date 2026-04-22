$ErrorActionPreference = "Stop"
$ppt = New-Object -ComObject PowerPoint.Application
$pptP = Convert-Path "..\Slide*.pptx"
$pres = $ppt.Presentations.Open($pptP, [Microsoft.Office.Core.MsoTriState]::msoFalse, [Microsoft.Office.Core.MsoTriState]::msoFalse, [Microsoft.Office.Core.MsoTriState]::msoFalse)
$outPath = Convert-Path "." | Join-Path -ChildPath "v11_ppt_text.txt"
$text = ""
foreach ($slide in $pres.Slides) {
  $text += "--- SLIDE $($slide.SlideIndex) ---`n"
  foreach ($shape in $slide.Shapes) {
    if ($shape.HasTextFrame -eq [Microsoft.Office.Core.MsoTriState]::msoTrue) {
      if ($shape.TextFrame.HasText -eq [Microsoft.Office.Core.MsoTriState]::msoTrue) {
         $text += $shape.TextFrame.TextRange.Text + "`n"
      }
    }
  }
}
$text | Out-File $outPath -Encoding UTF8
$pres.Close()
$ppt.Quit()
