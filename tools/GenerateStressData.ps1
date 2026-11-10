param(
    [int]$SetCount = 1000,
    [int]$StackOperations = 3000000,
    [int]$QueueOperations = 1500000,
    [int]$ExpressionTokens = 3000000
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

$root = Split-Path -Parent $PSScriptRoot
$utf8 = New-Object System.Text.UTF8Encoding($false)
$utf8Bom = New-Object System.Text.UTF8Encoding($true)

$stackDir = Join-Path $root "StackInput"
$queueDir = Join-Path $root "QueueInput"
$postfixDir = Join-Path $root "PostfixInput"
$infixDir = Join-Path $root "InfixInput"

foreach ($dir in @($stackDir, $queueDir, $postfixDir, $infixDir)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir | Out-Null
    }
}

Get-ChildItem $stackDir, $queueDir, $postfixDir, $infixDir -Filter "*EXTREME*" -File -ErrorAction SilentlyContinue |
    Remove-Item -Force

function New-Lengths([int]$count, [int]$total, [int]$minimum, [int]$maximum, [System.Random]$random) {
    $lengths = New-Object "int[]" $count
    $raw = New-Object "int[]" $count
    $rawSum = 0

    for ($i = 0; $i -lt $count; $i++) {
        $raw[$i] = $random.Next($minimum, $maximum + 1)
        $rawSum += $raw[$i]
    }

    $scaledSum = 0

    for ($i = 0; $i -lt $count; $i++) {
        $value = [int][Math]::Round($raw[$i] * $total / $rawSum)
        if ($value -lt $minimum) { $value = $minimum }
        $lengths[$i] = $value
        $scaledSum += $value
    }

    $difference = $total - $scaledSum
    $index = 0

    while ($difference -ne 0) {
        if ($difference -gt 0) {
            $lengths[$index]++
            $difference--
        }
        elseif ($lengths[$index] -gt $minimum) {
            $lengths[$index]--
            $difference++
        }

        $index = ($index + 1) % $count
    }

    return $lengths
}

$values = @(
    "cat", "alpha", "42", "-3", "0.5", "word", "stack", "beta", "17", "256",
    "omega", "delta", "7", "hello", "-17", "3.14", "node", "data", "test", "x",
    "y", "z", "one", "two", "100", "-42", "0", "9.99", "array", "queue",
    "list", "top", "base", "item", "value", "key", "leaf", "root", "vector", "map"
)

# push, pop, peek, isEmpty, print
$operationModes = @(
    @(0.62, 0.18, 0.10, 0.05, 0.05),
    @(0.40, 0.35, 0.10, 0.08, 0.07),
    @(0.48, 0.25, 0.12, 0.08, 0.07),
    @(0.50, 0.20, 0.18, 0.06, 0.06),
    @(0.55, 0.20, 0.08, 0.10, 0.07)
)

function New-CollectionSet([System.Random]$random, [int]$length, [int]$modeIndex) {
    $builder = New-Object System.Text.StringBuilder
    $size = 0

    # Гарантируем, что в каждой структуре окажется несколько разных значений.
    for ($k = 0; $k -lt 3; $k++) {
        $value = $values[$random.Next($values.Length)]
        [void]$builder.Append("1,$value ")
        $size++
    }

    [void]$builder.Append("5 ")
    $weights = $operationModes[$modeIndex % $operationModes.Length]

    for ($i = 4; $i -lt $length; $i++) {
        $code = 0

        if ($size -eq 0) {
            $code = 1
        }
        elseif ($size -ge 64) {
            $forced = $random.Next(0, 10)
            if ($forced -lt 5) { $code = 2 }
            elseif ($forced -lt 7) { $code = 3 }
            elseif ($forced -lt 9) { $code = 4 }
            else { $code = 5 }
        }
        else {
            $roll = $random.NextDouble()
            $cumulative = 0.0
            $code = 5

            for ($j = 0; $j -lt $weights.Length; $j++) {
                $cumulative += $weights[$j]

                if ($roll -le $cumulative) {
                    $code = $j + 1
                    break
                }
            }
        }

        if ($code -eq 1) {
            $value = $values[$random.Next($values.Length)]
            [void]$builder.Append("1,$value ")
            $size++
        }
        else {
            [void]$builder.Append("$code ")

            if ($code -eq 2 -and $size -gt 0) {
                $size--
            }
        }
    }

    return $builder.ToString().TrimEnd()
}

function New-PostfixExpression([System.Random]$random, [int]$targetTokens) {
    $operands = @(
        [pscustomobject]@{ Text = "1"; Tokens = 1 },
        [pscustomobject]@{ Text = "2"; Tokens = 1 },
        [pscustomobject]@{ Text = "3"; Tokens = 1 },
        [pscustomobject]@{ Text = "4"; Tokens = 1 },
        [pscustomobject]@{ Text = "5"; Tokens = 1 },
        [pscustomobject]@{ Text = "6"; Tokens = 1 },
        [pscustomobject]@{ Text = "7"; Tokens = 1 },
        [pscustomobject]@{ Text = "8"; Tokens = 1 },
        [pscustomobject]@{ Text = "9"; Tokens = 1 },
        [pscustomobject]@{ Text = "4 sqrt"; Tokens = 2 },
        [pscustomobject]@{ Text = "9 sqrt"; Tokens = 2 },
        [pscustomobject]@{ Text = "2 ln"; Tokens = 2 },
        [pscustomobject]@{ Text = "0 sin"; Tokens = 2 },
        [pscustomobject]@{ Text = "0 cos"; Tokens = 2 },
        [pscustomobject]@{ Text = "1 2 /"; Tokens = 3 },
        [pscustomobject]@{ Text = "2 3 ^"; Tokens = 3 }
    )

    $safeOne = @(
        [pscustomobject]@{ Text = "1"; Tokens = 1 },
        [pscustomobject]@{ Text = "0 cos"; Tokens = 2 }
    )

    $safeDivisor = @(
        [pscustomobject]@{ Text = "2"; Tokens = 1 },
        [pscustomobject]@{ Text = "4"; Tokens = 1 },
        [pscustomobject]@{ Text = "4 sqrt"; Tokens = 2 },
        [pscustomobject]@{ Text = "2 3 ^"; Tokens = 3 }
    )

    $operations = @("+", "-", "*", "/", ":", "^")
    $builder = New-Object System.Text.StringBuilder
    [void]$builder.Append("1")
    $count = 1

    while ($count -lt $targetTokens) {
        $operation = $operations[$random.Next($operations.Length)]

        if ($operation -eq "*" -or $operation -eq "^") {
            $operand = $safeOne[$random.Next($safeOne.Length)]
        }
        elseif ($operation -eq "/" -or $operation -eq ":") {
            $operand = $safeDivisor[$random.Next($safeDivisor.Length)]
        }
        else {
            $operand = $operands[$random.Next($operands.Length)]
        }

        [void]$builder.Append(" ")
        [void]$builder.Append($operand.Text)
        [void]$builder.Append(" ")
        [void]$builder.Append($operation)

        $count += $operand.Tokens + 1
    }

    return [pscustomobject]@{
        Text = $builder.ToString()
        Tokens = $count
    }
}

function New-InfixExpression([System.Random]$random, [int]$targetTokens) {
    $segments = @(
        [pscustomobject]@{ Text = "+1"; Tokens = 2 },
        [pscustomobject]@{ Text = "-2"; Tokens = 2 },
        [pscustomobject]@{ Text = "*1"; Tokens = 2 },
        [pscustomobject]@{ Text = "/2"; Tokens = 2 },
        [pscustomobject]@{ Text = ":2"; Tokens = 2 },
        [pscustomobject]@{ Text = "^1"; Tokens = 2 },
        [pscustomobject]@{ Text = "+sqrt(4)"; Tokens = 5 },
        [pscustomobject]@{ Text = "+ln(2)"; Tokens = 5 },
        [pscustomobject]@{ Text = "+sin(0)"; Tokens = 5 },
        [pscustomobject]@{ Text = "+cos(0)"; Tokens = 5 },
        [pscustomobject]@{ Text = "+2^3"; Tokens = 4 }
    )

    $builder = New-Object System.Text.StringBuilder
    [void]$builder.Append("1")
    $count = 1

    while ($count -lt $targetTokens) {
        $segment = $segments[$random.Next($segments.Length)]
        [void]$builder.Append($segment.Text)
        $count += $segment.Tokens
    }

    return [pscustomobject]@{
        Text = $builder.ToString()
        Tokens = $count
    }
}

function Write-OperationFile(
    [string]$path,
    [int[]]$lengths,
    [System.Random]$random,
    [string]$label) {

    $writer = New-Object System.IO.StreamWriter($path, $false, $utf8)
    $total = 0

    try {
        for ($i = 0; $i -lt $lengths.Length; $i++) {
            $line = New-CollectionSet $random $lengths[$i] $i
            $writer.WriteLine($line)
            $total += $lengths[$i]
        }
    }
    finally {
        $writer.Dispose()
    }

    Write-Host "  $label $path"
    Write-Host "    наборов: $($lengths.Length), операций: $total"
}

function Write-ExpressionFile(
    [string]$path,
    [int]$lineCount,
    [int]$minimum,
    [int]$maximum,
    [System.Random]$random,
    [scriptblock]$factory,
    [string]$label) {

    $writer = New-Object System.IO.StreamWriter($path, $false, $utf8)
    $total = 0

    try {
        for ($i = 0; $i -lt $lineCount; $i++) {
            $target = $random.Next($minimum, $maximum + 1)
            $expression = & $factory $random $target
            $writer.WriteLine($expression.Text)
            $total += $expression.Tokens
        }
    }
    finally {
        $writer.Dispose()
    }

    Write-Host "  $label $path"
    Write-Host "    выражений: $lineCount, токенов: $total"
}

$stackRandom = New-Object System.Random(41001)
$queueRandom = New-Object System.Random(41002)
$postfixRandom = New-Object System.Random(41003)
$infixRandom = New-Object System.Random(41004)

$averageStack = [int]($StackOperations / $SetCount)
$stackLengths = New-Lengths $SetCount $StackOperations ([int]($averageStack * 0.5)) ([int]($averageStack * 1.5)) $stackRandom
$stackPath = Join-Path $stackDir "stack_EXTREME_${SetCount}_sets_${StackOperations}_ops.txt"
Write-OperationFile $stackPath $stackLengths $stackRandom "Стек:"

$averageQueue = [int]($QueueOperations / $SetCount)
$queueLengths = New-Lengths $SetCount $QueueOperations ([int]($averageQueue * 0.5)) ([int]($averageQueue * 1.5)) $queueRandom
$queuePath = Join-Path $queueDir "queue_EXTREME_${SetCount}_sets_${QueueOperations}_ops.txt"
Write-OperationFile $queuePath $queueLengths $queueRandom "Очередь:"

$averageExpression = [int]($ExpressionTokens / $SetCount)
$expressionMinimum = [int]($averageExpression * 0.5)
$expressionMaximum = [int]($averageExpression * 1.5)

$postfixPath = Join-Path $postfixDir "postfix_EXTREME_${SetCount}_expressions_3M_tokens.txt"
Write-ExpressionFile $postfixPath $SetCount $expressionMinimum $expressionMaximum $postfixRandom ${function:New-PostfixExpression} "Постфикс:"

$infixPath = Join-Path $infixDir "infix_EXTREME_${SetCount}_expressions_3M_tokens.txt"
Write-ExpressionFile $infixPath $SetCount $expressionMinimum $expressionMaximum $infixRandom ${function:New-InfixExpression} "Инфикс:"

Write-Host ""
Write-Host "Готово. Каждый файл содержит около $SetCount наборов/выражений."
Write-Host "Длины строк различаются, значения push — разные."
Write-Host "Полное время с выводом показывается в конце работы программы."
