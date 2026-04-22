$ErrorActionPreference = "Stop"
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$docP = Convert-Path "BaoCaoCongNghePhanMem_Nhom8_KTKN1_N08_V11_Final.docx"
$doc = $word.Documents.Open($docP)
$outPath = Convert-Path "." | Join-Path -ChildPath "v11_word_text.txt"
$doc.Content.Text | Out-File $outPath -Encoding UTF8
$doc.Close([ref]$false)
$word.Quit()
