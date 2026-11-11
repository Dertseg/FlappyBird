param([string]$Adb = 'adb')
$ErrorActionPreference = 'Stop'
function Read-Ui {
    for ($attempt = 0; $attempt -lt 6; $attempt++) {
        & $Adb shell uiautomator dump /sdcard/flappy-smoke.xml 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            [xml]$document = (& $Adb shell cat /sdcard/flappy-smoke.xml)
            if ($document -and $document.SelectNodes('//node').Count) { return $document }
        }
        Start-Sleep -Seconds 2
    }
    throw 'Android UI did not become available'
}
function Tap-Label([string]$label) {
    $document = Read-Ui
    $element = $document.SelectNodes('//node') | Where-Object { $_.text -eq $label -or $_.'content-desc' -eq $label } | Select-Object -First 1
    if (!$element) { throw "Missing control: $label" }
    $numbers = [regex]::Matches($element.bounds, '\d+') | ForEach-Object { [int]$_.Value }
    & $Adb shell input tap ([int](($numbers[0]+$numbers[2])/2)) ([int](($numbers[1]+$numbers[3])/2))
}
function Assert-Label([string]$label) {
    $document = Read-Ui
    if (!$document.SelectNodes('//node').Count) { throw 'Empty UI' }
    if (!($document.SelectNodes('//node') | Where-Object { $_.text -like "*$label*" })) { throw "Missing text: $label" }
}
& $Adb shell am force-stop com.dertseg.flappybird
& $Adb shell monkey -p com.dertseg.flappybird -c android.intent.category.LAUNCHER 1 | Out-Null
Start-Sleep -Seconds 5
Assert-Label 'Полетели?'
Tap-Label 'Начать полёт  →'
Tap-Label 'Игровое поле. Касание — взмах.'
Start-Sleep -Seconds 3
Assert-Label 'Результат сохранён на устройстве'
Tap-Label 'Посмотреть рекорды'
Assert-Label 'Последний'
& $Adb shell am force-stop com.dertseg.flappybird
& $Adb shell monkey -p com.dertseg.flappybird -c android.intent.category.LAUNCHER 1 | Out-Null
Start-Sleep -Seconds 5
Assert-Label 'Полетели?'
Write-Output 'PASS: Android launch, flight, result save, leaderboard and cold restart'

