$ErrorActionPreference = "Stop"
$word = New-Object -ComObject Word.Application
$word.Visible = $false
try {
    $docP = Convert-Path "BaoCaoCongNghePhanMem_Nhom8_KTKN1_N08_V11_Final.docx"
    $doc = $word.Documents.Open($docP)
    
    if ($doc.TablesOfContents.Count -gt 0) {
        $doc.TablesOfContents.Item(1).Update()
    }
    
    $doc.Save()
    $doc.Close()
} finally {
    $word.Quit()
}
