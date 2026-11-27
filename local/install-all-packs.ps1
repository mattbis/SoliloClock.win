<#
.SYNOPSIS
Installs Windows speech voices for SoliloClock's languages and makes them
visible to SAPI 5, so every shipped language can actually be heard.

.DESCRIPTION
Windows keeps two speech stacks. Settings and most modern apps use the OneCore
voices; System.Speech (which SoliloClock uses) only lists SAPI 5 voices. Many
languages only exist as OneCore voices, so they never appear in
Options -> Voice even though Windows itself can speak them.

This helper closes that gap in two steps:

  1. -Install adds the Windows "text-to-speech" language feature
     (Language.TextToSpeech~~~<tag>) for the requested languages.
  2. -Bridge copies those OneCore voice tokens into the SAPI 5 token tree
     (HKLM\SOFTWARE\Microsoft\Speech\Voices\Tokens). That is exactly what
     Microsoft itself does for Hazel/Susan/George, whose SAPI 5 tokens point at
     C:\Windows\Speech_OneCore\Engines\TTS.

Voices are a machine-level install: nothing is bundled with SoliloClock and
nothing here is committed. Every token this script creates is recorded under
HKLM\SOFTWARE\SoliloClock\SapiBridge, so -Remove deletes only those and leaves
voices you installed yourself untouched.

Welsh (cy-GB) has no Windows voice at all; install Techiaith TTS with
-VoicePack instead.

Requires an elevated 64-bit PowerShell, plus internet access for Windows Update
when installing language features. Restart SoliloClock afterwards: the voice
list is read once at startup.

.PARAMETER Install
Add the Windows text-to-speech feature for each tag in -Tag, then bridge.

.PARAMETER Tag
Language tags to install. Defaults to the shipped European packs.
Only tags Windows advertises as Language.TextToSpeech are installable.

.PARAMETER Bridge
Copy already-installed OneCore voices into the SAPI 5 tree without downloading
anything. Use this after adding a language through Windows Settings.

.PARAMETER Remove
Delete the tokens this script created and forget them. Safe to run twice.

.PARAMETER VoicePack
Paths to vendor voice installers (for example Techiaith TTS for Welsh). .msi
files are installed silently; other files are launched so you can click through.

.EXAMPLE
pwsh local/install-all-packs.ps1
Report only: which voice speaks each shipped language and what is missing.

.EXAMPLE
pwsh local/install-all-packs.ps1 -Install
Install and expose the Western, Central/Eastern European, Arabic and East Asian
packs.

.EXAMPLE
pwsh local/install-all-packs.ps1 -Install -Tag de-DE,pl-PL,ru-RU
Install an arbitrary set of languages, including ones SoliloClock has not
shipped yet.

.EXAMPLE
pwsh local/install-all-packs.ps1 -Bridge
Expose voices that are already installed on this machine.

.EXAMPLE
pwsh local/install-all-packs.ps1 -VoicePack C:\Downloads\Techiaith-TTS.msi
Install a third-party SAPI 5 voice such as Welsh.

.EXAMPLE
pwsh local/install-all-packs.ps1 -Remove
Undo the bridging. Leaves Windows language features installed.
#>
param(
    [switch]$Install,
    [switch]$Bridge,
    [switch]$Remove,
    [string[]]$Tag = @('fr-FR', 'de-DE', 'es-ES', 'it-IT', 'nl-NL', 'pt-PT', 'pl-PL', 'ru-RU', 'el-GR', 'ar-SA', 'hi-IN', 'zh-CN', 'zh-TW', 'ja-JP', 'ko-KR', 'da-DK', 'sv-SE', 'nb-NO', 'cs-CZ',
    'tr-TR', 'hu-HU', 'ro-RO', 'hr-HR', 'bg-BG', 'sk-SK', 'fi-FI'),
    [string[]]$VoicePack = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sapiTokens = 'HKLM:\SOFTWARE\Microsoft\Speech\Voices\Tokens'
$oneCoreTokens = 'HKLM:\SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens'
$ledgerPath = 'HKLM:\SOFTWARE\SoliloClock\SapiBridge'

$sapiTokensNative = 'SOFTWARE\Microsoft\Speech\Voices\Tokens'
$oneCoreTokensNative = 'SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens'
$ledgerNative = 'SOFTWARE\SoliloClock\SapiBridge'

# What SoliloClock ships today (see LocaleRegistry.cs). Welsh is vendor-only.
$shipped = [ordered]@{
    'en-GB' = 'English'
    'fr-FR' = 'French'
    'de-DE' = 'German'
    'es-ES' = 'Spanish'
    'it-IT' = 'Italian'
    'nl-NL' = 'Dutch'
    'pt-PT' = 'Portuguese'
    'pl-PL' = 'Polish'
    'ru-RU' = 'Russian'
    'el-GR' = 'Greek'
    'da-DK' = 'Danish'
    'sv-SE' = 'Swedish'
    'nb-NO' = 'Norwegian'
    'cs-CZ' = 'Czech'
    'tr-TR' = 'Turkish'
    'hu-HU' = 'Hungarian'
    'ro-RO' = 'Romanian'
    'hr-HR' = 'Croatian'
    'bg-BG' = 'Bulgarian'
    'sk-SK' = 'Slovak'
    'fi-FI' = 'Finnish'
    'ar' = 'Arabic'
    'zh-Hans' = 'Chinese (Simplified)'
    'zh-Hant' = 'Chinese (Traditional)'
    'ja-JP' = 'Japanese'
    'hi' = 'Hindi'
    'ko-KR' = 'Korean'
    'cy-GB' = 'Welsh'
}

# Registry tags that are neutral or script-based (ar, hi, zh-Hans) need a Windows
# region tag before Windows will offer a text-to-speech capability for them.
$windowsTags = @{
    'ar' = 'ar-SA'
    'hi' = 'hi-IN'
    'zh-Hans' = 'zh-CN'
    'zh-Hant' = 'zh-TW'
}

function Test-IsElevated {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal $identity
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Voice tokens store their language as a hexadecimal LCID string ("809" = en-GB).
function ConvertTo-CultureName {
    param($Language)

    if ($null -eq $Language) { return '' }

    try {
        if ($Language -is [int] -or $Language -is [long]) {
            $lcid = [int]$Language
        }
        else {
            $text = ([string]$Language).Trim()
            if ($text.StartsWith('0x', [StringComparison]::OrdinalIgnoreCase)) { $text = $text.Substring(2) }
            if ($text -notmatch '^[0-9A-Fa-f]+$') { return '' }
            $lcid = [Convert]::ToInt32($text, 16)
        }

        return [System.Globalization.CultureInfo]::GetCultureInfo($lcid).Name
    }
    catch {
        return ''
    }
}

function Get-VoiceTokens {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { return @() }

    $voices = @()
    foreach ($key in Get-ChildItem -LiteralPath $Path) {
        $name = $key.PSChildName
        $culture = ''

        $attributesPath = Join-Path $key.PSPath 'Attributes'
        if (Test-Path -LiteralPath $attributesPath) {
            $attributes = Get-ItemProperty -LiteralPath $attributesPath
            $properties = @($attributes.PSObject.Properties.Name)
            if ($properties -contains 'Name') { $name = [string]$attributes.Name }
            if ($properties -contains 'Language') { $culture = ConvertTo-CultureName $attributes.Language }
        }

        $voices += [pscustomobject]@{
            Key     = $key.PSChildName
            Name    = $name
            Culture = $culture
        }
    }

    return $voices
}

function Copy-RegistryTree {
    param(
        [Microsoft.Win32.RegistryKey]$Source,
        [Microsoft.Win32.RegistryKey]$Destination
    )

    foreach ($valueName in $Source.GetValueNames()) {
        $Destination.SetValue($valueName, $Source.GetValue($valueName), $Source.GetValueKind($valueName))
    }

    foreach ($childName in $Source.GetSubKeyNames()) {
        $from = $Source.OpenSubKey($childName)
        if ($null -eq $from) { continue }

        $to = $Destination.CreateSubKey($childName, $true)
        try { Copy-RegistryTree -Source $from -Destination $to }
        finally { $from.Dispose(); $to.Dispose() }
    }
}

function Get-BridgedTokens {
    $key = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($ledgerNative)
    if ($null -eq $key) { return @() }

    try {
        $value = $key.GetValue('Tokens')
        if ($null -eq $value) { return @() }
        return @($value)
    }
    finally { $key.Dispose() }
}

function Add-BridgedTokens {
    param([string[]]$Names)

    if ($Names.Count -eq 0) { return }

    $merged = @(@(Get-BridgedTokens) + $Names | Select-Object -Unique)
    $key = [Microsoft.Win32.Registry]::LocalMachine.CreateSubKey($ledgerNative)
    try {
        $key.SetValue('Tokens', [string[]]$merged, [Microsoft.Win32.RegistryValueKind]::MultiString)
    }
    finally { $key.Dispose() }
}

function Invoke-Bridge {
    $oneCore = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($oneCoreTokensNative)
    if ($null -eq $oneCore) {
        Write-Host 'This Windows build has no OneCore voice tokens; nothing to bridge.'
        return @()
    }

    $added = @()
    try {
        $sapi = [Microsoft.Win32.Registry]::LocalMachine.CreateSubKey($sapiTokensNative)
        try {
            $existing = @($sapi.GetSubKeyNames())
            foreach ($name in $oneCore.GetSubKeyNames()) {
                if ($existing -contains $name) { continue }

                $from = $oneCore.OpenSubKey($name)
                if ($null -eq $from) { continue }

                $to = $sapi.CreateSubKey($name, $true)
                try { Copy-RegistryTree -Source $from -Destination $to }
                finally { $from.Dispose(); $to.Dispose() }

                $added += $name
            }
        }
        finally { $sapi.Dispose() }
    }
    finally { $oneCore.Dispose() }

    Add-BridgedTokens -Names $added
    return $added
}

function Invoke-Remove {
    $names = @(Get-BridgedTokens)
    if ($names.Count -eq 0) {
        Write-Host 'Nothing to remove: this script has not bridged any voices.'
        return
    }

    $sapi = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($sapiTokensNative, $true)
    if ($null -eq $sapi) {
        Write-Warning 'The SAPI 5 voice token tree is missing; nothing to remove.'
        return
    }

    $removed = 0
    try {
        $existing = @($sapi.GetSubKeyNames())
        foreach ($name in $names) {
            if ($existing -notcontains $name) { continue }
            try {
                $sapi.DeleteSubKeyTree($name)
                $removed++
            }
            catch {
                Write-Warning "Could not remove '$name': $($_.Exception.Message)"
            }
        }
    }
    finally { $sapi.Dispose() }

    [Microsoft.Win32.Registry]::LocalMachine.DeleteSubKeyTree($ledgerNative, $false)

    # Leave no empty SoliloClock key behind.
    try {
        $parent = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey('SOFTWARE\SoliloClock')
        if ($null -ne $parent) {
            $empty = $parent.SubKeyCount -eq 0 -and $parent.ValueCount -eq 0
            $parent.Dispose()
            if ($empty) { [Microsoft.Win32.Registry]::LocalMachine.DeleteSubKey('SOFTWARE\SoliloClock') }
        }
    }
    catch {
        # A leftover empty key is harmless.
    }

    Write-Host "Removed $removed bridged voice token(s). Windows language features are untouched."
}

function Get-CapabilityStates {
    $states = @{}
    try {
        foreach ($capability in Get-WindowsCapability -Online -ErrorAction Stop) {
            if ($capability.Name -match '^Language\.TextToSpeech~~~([A-Za-z]{2,3}-[A-Za-z]{2,4})~') {
                $states[$Matches[1]] = $capability.State
            }
        }
    }
    catch {
        Write-Verbose "Could not query Windows capabilities: $($_.Exception.Message)"
    }

    return $states
}

function Invoke-CapabilityInstall {
    param([string[]]$Tags)

    $failed = @()
    foreach ($tag in $Tags) {
        $name = "Language.TextToSpeech~~~$tag~0.0.1.0"
        $capability = Get-WindowsCapability -Online -Name $name -ErrorAction SilentlyContinue

        if ($null -eq $capability) {
            Write-Host ("  {0,-6} Windows offers no text-to-speech feature for this tag" -f $tag)
            $failed += $tag
            continue
        }

        if ($capability.State -eq 'Installed') {
            Write-Host ("  {0,-6} already installed" -f $tag)
            continue
        }

        try {
            $result = Add-WindowsCapability -Online -Name $name -ErrorAction Stop
            $restart = if ($result.RestartNeeded) { ' (restart needed)' } else { '' }
            Write-Host ("  {0,-6} installed{1}" -f $tag, $restart)
        }
        catch {
            Write-Host ("  {0,-6} failed: {1}" -f $tag, $_.Exception.Message)
            $failed += $tag
        }
    }

    return $failed
}

function Invoke-VoicePackInstall {
    param([string[]]$Paths)

    foreach ($path in $Paths) {
        $resolved = Resolve-Path -LiteralPath $path -ErrorAction SilentlyContinue
        if ($null -eq $resolved) {
            Write-Warning "Voice pack not found: $path"
            continue
        }

        $installer = $resolved.Path
        if ($installer.EndsWith('.msi', [StringComparison]::OrdinalIgnoreCase)) {
            Write-Host "Installing $installer silently ..."
            $process = Start-Process -FilePath 'msiexec.exe' -ArgumentList @('/i', ('"{0}"' -f $installer), '/qn', '/norestart') -Wait -PassThru
            # 3010 means success but a restart is pending.
            if ($process.ExitCode -eq 0 -or $process.ExitCode -eq 3010) {
                Write-Host '  installed'
            }
            else {
                Write-Warning "  msiexec returned exit code $($process.ExitCode)"
            }
        }
        else {
            Write-Host "Launching $installer; follow its own prompts ..."
            Start-Process -FilePath $installer -Wait | Out-Null
        }
    }
}

function Write-Report {
    param([hashtable]$Capabilities)

    $sapiVoices = @(Get-VoiceTokens -Path $sapiTokens | Sort-Object Culture, Name)
    $oneCoreVoices = @(Get-VoiceTokens -Path $oneCoreTokens)

    Write-Host ''
    Write-Host 'Voices SAPI 5 can use (this is what SoliloClock lists):'
    if ($sapiVoices.Count -eq 0) {
        Write-Host '  none'
    }
    else {
        foreach ($voice in $sapiVoices) {
            $culture = if ($voice.Culture) { $voice.Culture } else { 'unknown' }
            Write-Host ("  {0,-8} {1}" -f $culture, $voice.Name)
        }
    }

    $unbridged = @($oneCoreVoices | Where-Object { $sapiVoices.Key -notcontains $_.Key })
    if ($unbridged.Count -gt 0) {
        Write-Host ''
        Write-Host 'Bonus: Windows voices that exist but are not bridged yet (-Bridge exposes them):'
        foreach ($voice in $unbridged | Sort-Object Culture, Name) {
            $culture = if ($voice.Culture) { $voice.Culture } else { 'unknown' }
            Write-Host ("  {0,-8} {1}" -f $culture, $voice.Name)
        }
    }

    Write-Host ''
    Write-Host 'SoliloClock shipped languages:'
    foreach ($culture in $shipped.Keys) {
        $language = $shipped[$culture]
        $windowsTag = if ($windowsTags.ContainsKey($culture)) { $windowsTags[$culture] } else { $culture }

        $exact = @($sapiVoices | Where-Object {
            $_.Culture -ieq $culture -or $_.Culture -ieq $windowsTag
        })

        # Chinese scripts share one language code, so match the region rather than
        # claiming a Traditional voice can speak a Simplified catalog.
        $related = @($sapiVoices | Where-Object {
            if (-not $_.Culture) { $false }
            elseif ($culture -eq 'zh-Hans') { $_.Culture -in @('zh-CN', 'zh-SG', 'zh-Hans') }
            elseif ($culture -eq 'zh-Hant') { $_.Culture -in @('zh-TW', 'zh-HK', 'zh-MO', 'zh-Hant') }
            else { $_.Culture.Split('-')[0] -eq $culture.Split('-')[0] }
        })

        if ($exact.Count -gt 0) {
            Write-Host ("  {0,-6} {1,-11} ok: {2}" -f $culture, $language, ($exact.Name -join ', '))
            continue
        }

        if ($related.Count -gt 0) {
            Write-Host ("  {0,-6} {1,-11} no exact voice; falls back to {2}" -f $culture, $language, ($related.Name -join ', '))
            continue
        }

        $state = if ($Capabilities.ContainsKey($windowsTag)) { $Capabilities[$windowsTag] } else { $null }
        $hint = if ($culture -eq 'cy-GB') {
            'install Techiaith TTS with -VoicePack <installer>'
        }
        elseif ($state -eq 'Installed') {
            'installed in Windows but not bridged: run -Bridge'
        }
        elseif ($null -ne $state) {
            "install with -Install -Tag $windowsTag"
        }
        else {
            'no Windows voice is offered for this language'
        }

        Write-Host ("  {0,-6} {1,-11} missing: {2}" -f $culture, $language, $hint)
    }

    Write-Host ''
    Write-Host 'Restart SoliloClock after changing voices; the list is read at startup.'
}

$needsAdmin = $Install -or $Bridge -or $Remove -or ($VoicePack.Count -gt 0)
$exitCode = 0

if (-not [Environment]::Is64BitProcess) {
    Write-Host 'This script must run in 64-bit PowerShell: SoliloClock is x64 and reads the 64-bit SAPI 5 token tree.'
    exit 1
}

if ($needsAdmin -and -not (Test-IsElevated)) {
    Write-Host 'This action needs administrator rights (machine-wide voice tokens and Windows features).'
    Write-Host 'Open PowerShell with "Run as administrator" and run the same command again.'
    exit 1
}

if ($Remove) {
    Invoke-Remove
}

if ($VoicePack.Count -gt 0) {
    Invoke-VoicePackInstall -Paths $VoicePack
}

if ($Install) {
    Write-Host 'Installing Windows text-to-speech language features ...'
    $failed = @(Invoke-CapabilityInstall -Tags $Tag)
    if ($failed.Count -gt 0) {
        Write-Warning "Could not install: $($failed -join ', '). Installs need Windows Update access; if it failed with 0x800f0954, check the Windows Update policy."
        $exitCode = 1
    }
}

if ($Install -or $Bridge) {
    $added = @(Invoke-Bridge)
    if ($added.Count -eq 0) {
        Write-Host 'Nothing new to bridge: every installed Windows voice is already visible to SAPI 5.'
    }
    else {
        Write-Host "Bridged $($added.Count) Windows voice(s) into SAPI 5: $($added -join ', ')"
    }
}

Write-Report -Capabilities (Get-CapabilityStates)

exit $exitCode
