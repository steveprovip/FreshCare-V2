$word = New-Object -ComObject Word.Application
$word.Visible = $false
$docPath = "d:\HOC!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\phan mem\FreshCare-V3\Docs\BaoCaoCongNghePhanMem_Nhom8_KTKN1_N08_V9.docx"
$doc = $word.Documents.Open($docPath)
$text = $doc.Content.Text
$doc.Close($false)
$word.Quit()
$outPath = "d:\HOC!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\phan mem\FreshCare-V3\Docs\report_text.txt"
$text | Out-File -FilePath $outPath -Encoding UTF8
Write-Host "Done - extracted to report_text.txt"
