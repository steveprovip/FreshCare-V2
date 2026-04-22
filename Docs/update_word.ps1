$ErrorActionPreference = "Stop"

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$docP = Convert-Path "BaoCaoCongNghePhanMem_Nhom8_KTKN1_N08_V11_Final.docx"
$doc = $word.Documents.Open($docP)

$missing = [System.Type]::Missing
$replaceMode = 2 # wdReplaceAll
$matchCase = $false
$matchWholeWord = $false
$matchWildcards = $false
$matchSoundsLike = $false
$matchAllWordForms = $false
$forward = $true
$wrap = 1 # wdFindContinue
$format = $false

$replacements = @{
    "<= 3 ngày" = "<= 14 ngày"
    "< 3 ngày"  = "<= 14 ngày"
    "<= 3"      = "<= 14"
}

foreach ($key in $replacements.Keys) {
    $find = $doc.Content.Find
    $find.ClearFormatting()
    $find.Replacement.ClearFormatting()
    
    $findText = $key
    $replaceText = $replacements[$key]
    
    $result = $find.Execute([ref]$findText, [ref]$matchCase, [ref]$matchWholeWord, 
                  [ref]$matchWildcards, [ref]$matchSoundsLike, [ref]$matchAllWordForms, 
                  [ref]$forward, [ref]$wrap, [ref]$format, [ref]$replaceText, 
                  [ref]$replaceMode, [ref]$missing, [ref]$missing, [ref]$missing, [ref]$missing)
}

# Apply formatting
$doc.PageSetup.PaperSize = 7
$doc.PageSetup.TopMargin = $word.CentimetersToPoints(2)
$doc.PageSetup.BottomMargin = $word.CentimetersToPoints(2)
$doc.PageSetup.LeftMargin = $word.CentimetersToPoints(3)
$doc.PageSetup.RightMargin = $word.CentimetersToPoints(1.5)

# Update font and paragraphs
# Get Normal style and update it to apply to all normal text
$normalStyle = $doc.Styles.Item("Normal")
$normalStyle.Font.Name = "Times New Roman"
$normalStyle.Font.Size = 14
$normalStyle.ParagraphFormat.Alignment = 3
$normalStyle.ParagraphFormat.LineSpacingRule = 1

# update TOC
try {
   if ($doc.TablesOfContents.Count -gt 0) {
      $doc.TablesOfContents.Item(1).Update()
   }
} catch {
   Write-Host "Warning: Could not update TOC"
}

$doc.Save()
Start-Sleep -Seconds 2
$doc.Close([ref]$false)
$word.Quit()
Write-Host "Word document updated successfully."
