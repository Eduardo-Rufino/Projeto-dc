# Driver for running/testing "Projeto DC" (Godot 4.7 C#/.NET 8 game) headlessly
# or with a real window, from an agent session with no interactive display.
#
# Usage:
#   powershell -File driver.ps1 -Action build
#   powershell -File driver.ps1 -Action import  [-QuitAfter 2000]
#   powershell -File driver.ps1 -Action launch  [-Windowed] [-LogPath <path>]
#   powershell -File driver.ps1 -Action screenshot -OutPath <path.png>
#   powershell -File driver.ps1 -Action stop
#   powershell -File driver.ps1 -Action check   -LogPath <path>
#
# All actions are safe to call from the project root or from anywhere -
# paths below are resolved relative to this script's location (../../.. -> project root).

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("build", "import", "launch", "screenshot", "stop", "check")]
    [string]$Action,

    [switch]$Windowed,
    [string]$LogPath = "$env:TEMP\claude\projeto_dc_run.log",
    [string]$OutPath = "$env:TEMP\claude\projeto_dc_screenshot.png",
    [int]$QuitAfter = 2000,
    [int]$LaunchWaitSeconds = 8
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$Csproj = Join-Path $ProjectRoot "Projeto DC.csproj"

# Found by searching Downloads for the extracted mono build - see SKILL.md Gotchas
# if this path doesn't exist on a different machine.
$GodotExe = "C:\Users\barce\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe"

function Ensure-LogDir($path) {
    $dir = Split-Path $path -Parent
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
}

switch ($Action) {
    "build" {
        Push-Location $ProjectRoot
        try {
            dotnet build $Csproj -v quiet
            exit $LASTEXITCODE
        } finally { Pop-Location }
    }

    "import" {
        # New PNG/scene assets are invisible to GD.Load until the editor imports them once -
        # this runs that import pass headlessly (no window) and exits on its own.
        # Watch stdout for "Started (Re)Importando Recursos (N steps)" ... "[ DONE ] reimport" -
        # if -QuitAfter is too low for a big batch of new files, it'll quit mid-import.
        Push-Location $ProjectRoot
        try {
            & $GodotExe --path "." --headless --editor --quit-after $QuitAfter
            exit $LASTEXITCODE
        } finally { Pop-Location }
    }

    "launch" {
        Ensure-LogDir $LogPath
        Push-Location $ProjectRoot
        try {
            $args = @("--path", ".", "--verbose")
            if (-not $Windowed) { } # Godot has no true headless *game* run with rendering;
                                     # omit --headless here so sprites/animations actually
                                     # render (needed for the screenshot action).
            $proc = Start-Process -FilePath $GodotExe -ArgumentList $args `
                -RedirectStandardOutput $LogPath -RedirectStandardError "$LogPath.err" `
                -PassThru -WindowStyle Normal
            $proc.Id | Out-File "$env:TEMP\claude\projeto_dc.pid" -Encoding ascii
            Write-Output "Launched PID $($proc.Id), log at $LogPath"
        } finally { Pop-Location }
    }

    "screenshot" {
        Ensure-LogDir $OutPath
        Add-Type -AssemblyName System.Windows.Forms
        Add-Type -AssemblyName System.Drawing
        Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32 {
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
        # The actual game window is a SEPARATE process from the launched
        # "..._console.exe" (that one just owns the log console) - it's spawned as plain
        # "Godot_v4.7-stable_mono_win64" with title "Projeto DC (DEBUG)" in a debug run.
        # Match by substring, not exact title, and search actual game processes, not the
        # console launcher.
        $target = Get-Process -Name "Godot_v4.7-stable_mono_win64" -ErrorAction SilentlyContinue |
            Where-Object { $_.MainWindowTitle -like "*Projeto DC*" } | Select-Object -First 1

        if (-not $target -or $target.MainWindowHandle -eq [IntPtr]::Zero) {
            Write-Output "Game window not found (title should contain 'Projeto DC') - falling back to full-screen capture."
            $bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
            $bmp = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.CopyFromScreen($bounds.Left, $bounds.Top, 0, 0, $bounds.Size)
            $g.Dispose()
        } else {
            # PrintWindow captures the window's own content directly (composited off-screen
            # buffer), NOT a region of the visible screen - unlike CopyFromScreen this works
            # correctly even when the window is occluded or not focused. This matters because
            # SetForegroundWindow is unreliable when called from a background/unfocused
            # process (a documented Win32 restriction) - an earlier version of this script used
            # SetForegroundWindow+CopyFromScreen and it silently captured a DIFFERENT
            # already-foreground window instead of the game, with no error raised.
            $hwnd = $target.MainWindowHandle
            $rect = New-Object Win32+RECT
            [Win32]::GetClientRect($hwnd, [ref]$rect) | Out-Null
            $w = $rect.Right - $rect.Left
            $h = $rect.Bottom - $rect.Top
            $bmp = New-Object System.Drawing.Bitmap $w, $h
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $hdc = $g.GetHdc()
            [Win32]::PrintWindow($hwnd, $hdc, 2) | Out-Null  # PW_RENDERFULLCONTENT = 2
            $g.ReleaseHdc($hdc)
            $g.Dispose()
        }
        $bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Output "Screenshot saved to $OutPath"
    }

    "stop" {
        Get-Process -Name "Godot_v4.7-stable_mono_win64_console" -ErrorAction SilentlyContinue | Stop-Process -Force
        Get-Process -Name "Godot_v4.7-stable_mono_win64" -ErrorAction SilentlyContinue | Stop-Process -Force
        Write-Output "Stopped."
    }

    "check" {
        if (-not (Test-Path $LogPath)) { Write-Output "No log at $LogPath"; exit 1 }
        $hits = Select-String -Path $LogPath -Pattern "ERROR|Exception|Falha|Frame não encontrado" -ErrorAction SilentlyContinue
        if ($hits) {
            Write-Output "ISSUES FOUND:"
            $hits | Select-Object -First 30 | ForEach-Object { Write-Output $_.Line }
            exit 1
        } else {
            Write-Output "Clean - no ERROR/Exception/Falha/'Frame não encontrado' lines in $LogPath"
            exit 0
        }
    }
}
