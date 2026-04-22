$ErrorActionPreference = "Stop"
$word = New-Object -ComObject Word.Application
$word.Visible = $false

try {
    $docP = Convert-Path "BaoCaoCongNghePhanMem_Nhom8_KTKN1_N08_V11_Final.docx"
    $doc = $word.Documents.Open($docP)
    
    # 1. Formatting Margins (Nghị định 30/2020)
    $doc.PageSetup.PaperSize = 7 # wdPaperA4
    $doc.PageSetup.TopMargin = $word.CentimetersToPoints(2)
    $doc.PageSetup.BottomMargin = $word.CentimetersToPoints(2)
    $doc.PageSetup.LeftMargin = $word.CentimetersToPoints(3)
    $doc.PageSetup.RightMargin = $word.CentimetersToPoints(1.5)

    # 2. Formatting Font & Paragraph for Normal Style
    $normalStyle = $doc.Styles.Item("Normal")
    $normalStyle.Font.Name = "Times New Roman"
    $normalStyle.Font.Size = 14
    $normalStyle.ParagraphFormat.Alignment = 3 # wdAlignParagraphJustify
    $normalStyle.ParagraphFormat.LineSpacingRule = 1 # wdLineSpace1pt5
    
    # Optional: Apply Times New Roman directly to whole document to ensure consistency, 
    # but don't touch size so we don't ruin Headings
    $doc.Content.Font.Name = "Times New Roman"

    # 3. Update TOC
    if ($doc.TablesOfContents.Count -gt 0) {
        $doc.TablesOfContents.Item(1).Update()
    }

    $doc.Save()
    Write-Host "Formatting applied successfully."
} catch {
    Write-Host "Error occurred: $_"
} finally {
    if ($doc) { $doc.Close([ref]$false) }
    $word.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
}
