$ErrorActionPreference = "Stop"

$word = New-Object -ComObject Word.Application
$word.Visible = $false

$docPath = Join-Path $PSScriptRoot "BaoCaoCongNghePhanMem_Nhom8_KTKN1_N08_V10.docx"
$savePath = Join-Path $PSScriptRoot "BaoCaoCongNghePhanMem_Nhom8_KTKN1_N08_V11_Final.docx"
$evalPath = Join-Path $PSScriptRoot "evaluation.txt"

# Read evaluation text
$evalLines = Get-Content -Path $evalPath -Encoding UTF8
$evalTextTitle = $evalLines[0] + "`n"
$evalTextBody = ""
for ($i = 1; $i -lt $evalLines.Count; $i++) {
    $evalTextBody += $evalLines[$i] + "`n"
}

try {
    Write-Host "Opening document: $docPath"
    $doc = $word.Documents.Open($docPath)

    # 1. Format Margins (Decree 30/2020)
    Write-Host "Adjusting margins..."
    $doc.PageSetup.TopMargin = $word.CentimetersToPoints(2)
    $doc.PageSetup.BottomMargin = $word.CentimetersToPoints(2)
    $doc.PageSetup.LeftMargin = $word.CentimetersToPoints(3)
    $doc.PageSetup.RightMargin = $word.CentimetersToPoints(1.5)

    # 2. Set Font Times New Roman 14 for Normal Style, Justified
    Write-Host "Applying Font Times New Roman, Size 14, Justified to Normal Style."
    $normalStyle = $doc.Styles.Item("Normal")
    $normalStyle.Font.Name = "Times New Roman"
    $normalStyle.Font.Size = 14
    $normalStyle.ParagraphFormat.Alignment = 3 # wdAlignParagraphJustify
    $normalStyle.ParagraphFormat.LineSpacingRule = 1 # wdLineSpace1pt5

    # 3. Swap Tasks based on content keywords instead of names (to avoid encoding issues)
    Write-Host "Swapping tasks..."
    if ($doc.Tables.Count -ge 1) {
        $table = $doc.Tables.Item(1)
        $uiRow = -1; $testRow = -1;
        
        for ($r = 1; $r -le $table.Rows.Count; $r++) {
            $taskText = $table.Cell($r, 2).Range.Text
            if ($taskText -match "UI/UX") { $uiRow = $r }
            if ($taskText -match "Test Case") { $testRow = $r }
        }
        
        if ($uiRow -gt 0 -and $testRow -gt 0) {
            $uiTaskCell = $table.Cell($uiRow, 2)
            $testTaskCell = $table.Cell($testRow, 2)
            
            $uiText = $uiTaskCell.Range.Text.Replace("`a", "").Replace("`r", "")
            $testText = $testTaskCell.Range.Text.Replace("`a", "").Replace("`r", "")
            
            $table.Cell($uiRow, 2).Range.Text = $testText
            $table.Cell($testRow, 2).Range.Text = $uiText
            Write-Host "-> Swapped row $uiRow (UI/UX) and $testRow (Test Case)"
        } else {
            Write-Host "-> Could not find UI/UX or Test Case tasks in Table 1"
        }
    }

    # 4. Append Evaluation Section at the end
    Write-Host "Appending system evaluation..."
    $endRng = $doc.Range()
    $endRng.Collapse(0) # wdCollapseEnd

    $endRng.InsertBreak(2) # wdPageBreak
    
    $endRng = $doc.Range()
    $endRng.Collapse(0)

    $endRng.Text = $evalTextTitle
    $endRng.Style = $doc.Styles.Item(-2) # wdStyleHeading1
    
    $endRng = $doc.Range()
    $endRng.Collapse(0)

    $endRng.Text = $evalTextBody
    $endRng.Style = $doc.Styles.Item("Normal")

    # 5. Table of Contents Update
    Write-Host "Updating Table of Contents..."
    if ($doc.TablesOfContents.Count -gt 0) {
        $doc.TablesOfContents.Item(1).Update()
        Write-Host "-> TOC Updated."
    } else {
        Write-Host "-> No existing TOC found to update."
    }

    Write-Host "Saving document..."
    $doc.SaveAs([string]$savePath) # Without [ref] for object conversion error
    Write-Host "ALL DONE!"

} catch {
    Write-Host "ERROR: $_"
} finally {
    if ($doc) { $doc.Close([ref]$false) }
    $word.Quit()
}
