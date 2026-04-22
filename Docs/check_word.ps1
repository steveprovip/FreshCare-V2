$word = New-Object -ComObject Word.Application
if ($word) {
    Write-Output 'Word is installed'
    $word.Quit()
} else {
    Write-Output 'Word not found'
}
