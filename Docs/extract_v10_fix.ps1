$ErrorActionPreference = "Stop"

$word = New-Object -ComObject Word.Application
$word.Visible = $false

$docPath = Join-Path $PSScriptRoot "BaoCaoCongNghePhanMem_Nhom8_KTKN1_N08_V10.docx"
$outTextPath = Join-Path $PSScriptRoot "report_v10_content.txt"
$outTablesPath = Join-Path $PSScriptRoot "report_v10_tables.txt"

try {
    $doc = $word.Documents.Open($docPath)

    # Extract all text
    $text = $doc.Content.Text
    $text | Out-File -FilePath $outTextPath -Encoding UTF8

    # Extract tables
    $tableData = @()
    $tableCount = $doc.Tables.Count
    for ($i = 1; $i -le $tableCount; $i++) {
        $tableData += "--- TABLE $i ---"
        $table = $doc.Tables.Item($i)
        $rows = $table.Rows.Count
        $cols = $table.Columns.Count

        for ($r = 1; $r -le $rows; $r++) {
            $rowData = @()
            for ($c = 1; $c -le $cols; $c++) {
                try {
                    $cellText = $table.Cell($r, $c).Range.Text.Replace("`r`a", "").Trim()
                    $rowData += $cellText
                } catch {
                    $rowData += "[ERROR/MERGED]"
                }
            }
            $tableData += ($rowData -join " | ")
        }
        $tableData += "`n"
    }
    $tableData | Out-File -FilePath $outTablesPath -Encoding UTF8

    $doc.Close($false)
} finally {
    $word.Quit()
}

Write-Host "Extraction Complete."
