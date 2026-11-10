$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

$root = Split-Path -Parent $PSScriptRoot
$utf8 = New-Object System.Text.UTF8Encoding($false)
$random = New-Object System.Random(20261110)

$stackDir = Join-Path $root "StackInput"
$queueDir = Join-Path $root "QueueInput"
$postfixDir = Join-Path $root "PostfixInput"
$infixDir = Join-Path $root "InfixInput"

foreach ($dir in @($stackDir, $queueDir, $postfixDir, $infixDir)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir | Out-Null
    }
}

function Write-Utf8Lines([string]$path, [string[]]$lines) {
    [System.IO.File]::WriteAllLines($path, $lines, $utf8)
}

function Get-RandomValue {
    $values = @("56", "7", "cat", "alpha", "42", "word", "15", "beta", "-3", "0.5", "stack", "42")
    return $values[$random.Next($values.Length)]
}

function New-StackSet([int]$length) {
    $tokens = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $length; $i++) {
        if ($i -eq 0 -or $random.NextDouble() -lt 0.48) {
            $tokens.Add("1,$(Get-RandomValue)")
        }
        else {
            $tokens.Add([string]$random.Next(2, 6))
        }
    }

    return ($tokens -join " ")
}

function New-StackFile([string]$name, [int]$setCount, [int]$minLength, [int]$maxLength) {
    $lines = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $setCount; $i++) {
        $lines.Add((New-StackSet ($random.Next($minLength, $maxLength + 1))))
    }

    Write-Utf8Lines (Join-Path $stackDir $name) $lines.ToArray()
}

function New-QueueSet([int]$count, [string]$mode) {
    $tokens = New-Object System.Collections.Generic.List[string]

    if ($mode -eq "push") {
        for ($i = 0; $i -lt $count; $i++) {
            $tokens.Add("1,item$i")
        }
    }
    elseif ($mode -eq "read") {
        $tokens.Add("1,seed")
        for ($i = 1; $i -lt $count; $i++) {
            if ($i % 2 -eq 0) { $tokens.Add("3") } else { $tokens.Add("4") }
        }
    }
    elseif ($mode -eq "mixed") {
        $pattern = @("1,a", "1,b", "3", "4", "5", "2")
        for ($i = 0; $i -lt $count; $i++) {
            $tokens.Add($pattern[$i % $pattern.Length])
        }
    }
    else {
        for ($i = 0; $i -lt $count; $i++) {
            if ($i -eq 0 -or $random.NextDouble() -lt 0.52) {
                $tokens.Add("1,$(Get-RandomValue)")
            }
            else {
                $tokens.Add([string]$random.Next(2, 6))
            }
        }
    }

    return ($tokens -join " ")
}

function New-QueueFile([string]$name, [int]$setCount, [int]$operationsPerSet, [string]$mode) {
    $lines = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $setCount; $i++) {
        $lines.Add((New-QueueSet $operationsPerSet $mode))
    }

    Write-Utf8Lines (Join-Path $queueDir $name) $lines.ToArray()
}

function New-PositivePostfix([int]$depth) {
    if ($depth -le 0 -or $random.NextDouble() -lt 0.36) {
        return [string]$random.Next(1, 7)
    }

    $left = New-PositivePostfix ($depth - 1)
    $right = New-PositivePostfix ($depth - 1)
    $operations = @("+", "*", "+", "*", "/")
    $operation = $operations[$random.Next($operations.Length)]

    return "$left $right $operation"
}

function New-PostfixExpression {
    $base = New-PositivePostfix 3

    switch ($random.Next(0, 10)) {
        0 { return "$base sqrt" }
        1 { return "$base ln" }
        2 { return "$base sin" }
        3 { return "$base cos" }
        4 { return "$base 0.5 ^" }
        5 { return "$base 2 ^" }
        6 { return "$base 3 -" }
        7 { return "$base 2 + 3 *" }
        8 { return "$base 2 :" }
        default { return $base }
    }
}

function New-PostfixFile([string]$name, [int]$count) {
    $lines = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $count; $i++) {
        $lines.Add((New-PostfixExpression))
    }

    Write-Utf8Lines (Join-Path $postfixDir $name) $lines.ToArray()
}

function New-PositiveInfix([int]$depth) {
    if ($depth -le 0 -or $random.NextDouble() -lt 0.33) {
        return [string]$random.Next(1, 10)
    }

    $left = New-PositiveInfix ($depth - 1)
    $right = New-PositiveInfix ($depth - 1)

    switch ($random.Next(0, 5)) {
        0 { return "($left+$right)" }
        1 { return "($left*$right)" }
        2 { return "($left/$right)" }
        3 { return "($left^2)" }
        default { return "($left+$right)" }
    }
}

function New-InfixExpression {
    $base = New-PositiveInfix 3

    switch ($random.Next(0, 8)) {
        0 { return "sqrt($base)" }
        1 { return "ln($base)" }
        2 { return "sin($base)" }
        3 { return "cos($base)" }
        4 { return "-($base)" }
        5 { return "($base-2)" }
        default { return $base }
    }
}

function New-InfixFile([string]$name, [int]$count) {
    $lines = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $count; $i++) {
        $lines.Add((New-InfixExpression))
    }

    Write-Utf8Lines (Join-Path $infixDir $name) $lines.ToArray()
}

# --- Примеры и файлы стека -------------------------------------------------
Write-Utf8Lines (Join-Path $stackDir "stack_3_sets.txt") @(
    "3 4 1,56 1,7 1,cat 2 5 4",
    "1,alpha 5 3 2 4",
    "1,10 1,20 1,30 2 3 5"
)

New-StackFile "stack_10_sets.txt" 10 5 10
New-StackFile "stack_100_sets.txt" 100 6 12
New-StackFile "stack_1000_sets.txt" 1000 6 12

Write-Utf8Lines (Join-Path $stackDir "stack_invalid_extra.txt") @(
    "1,5 2 9 5",
    "1 5 4",
    "1,ok 2 5"
)

# --- Очередь ---------------------------------------------------------------
Write-Utf8Lines (Join-Path $queueDir "input.txt") @(
    "3 4 1,56 1,7 1,cat 2 5 4"
)

New-QueueFile "queue_5_sets_10_ops.txt" 5 10 "random"
New-QueueFile "queue_5_sets_100_ops.txt" 5 100 "random"
New-QueueFile "queue_5_sets_1000_ops.txt" 5 1000 "random"
New-QueueFile "queue_100_sets_10_ops.txt" 100 10 "random"
New-QueueFile "queue_1000_sets_10_ops.txt" 1000 10 "random"
New-QueueFile "queue_10_sets_1000_ops_push.txt" 10 1000 "push"
New-QueueFile "queue_10_sets_1000_ops_read.txt" 10 1000 "read"
New-QueueFile "queue_10_sets_1000_ops_mixed.txt" 10 1000 "mixed"

Write-Utf8Lines (Join-Path $queueDir "queue_invalid_extra.txt") @(
    "1,a 2 7 4",
    "8 1,b 5"
)

# --- Постфиксные выражения -------------------------------------------------
Write-Utf8Lines (Join-Path $postfixDir "postfix_10_expressions.txt") @(
    "4 2 + 3 *",
    "2 3 ^ 1 -",
    "9 sqrt 2 +",
    "0.5 sin 2 *",
    "2 3 + 4 5 + *",
    "10 2 : 3 +",
    "2 ln",
    "5 2 - 3 ^",
    "100 sqrt 3 /",
    "1 2 3 4 5 + + + +"
)

New-PostfixFile "postfix_100_expressions.txt" 100
New-PostfixFile "postfix_1000_expressions.txt" 1000

# --- Инфиксные выражения ---------------------------------------------------
Write-Utf8Lines (Join-Path $infixDir "infix_10_expressions.txt") @(
    "2+2",
    "(2+3)*4",
    "sin(0.5)+cos(0.5)",
    "sqrt(9)+2^3",
    "10/2+3",
    "2^3^2",
    "ln(2)+1",
    "-(2+3)*4",
    "2*(3+4)/5",
    "sqrt(2^2+3^2)"
)

New-InfixFile "infix_100_expressions.txt" 100
New-InfixFile "infix_1000_expressions.txt" 1000

Write-Host "Тестовые данные созданы."
Write-Host "Стек:    $stackDir"
Write-Host "Очередь: $queueDir"
Write-Host "Постфикс: $postfixDir"
Write-Host "Инфикс:  $infixDir"
