[CmdletBinding()]
param(
	[switch] $KeepPublishDirectories
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step
{
	param(
		[Parameter(Mandatory)]
		[string] $Message
	)

	Write-Host ""
	Write-Host "==> $Message"
}

function Assert-PowerShellVersion
{
	if ($PSVersionTable.PSVersion.Major -lt 7)
	{
		throw "Для публикации требуется PowerShell 7 или новее."
	}
}

function Assert-DotNetSdk
{
	$dotnetCommand = Get-Command "dotnet" -ErrorAction SilentlyContinue

	if ($null -eq $dotnetCommand)
	{
		throw "Команда dotnet не найдена. Установите .NET SDK 10."
	}

	$sdkVersion = (& dotnet --version).Trim()

	if ($LASTEXITCODE -ne 0)
	{
		throw "Не удалось определить версию .NET SDK."
	}

	$parsedVersion = $null

	if (![Version]::TryParse($sdkVersion, [ref] $parsedVersion))
	{
		throw "Не удалось распознать версию .NET SDK: $sdkVersion."
	}

	if ($parsedVersion.Major -ne 10)
	{
		throw "Требуется .NET SDK 10. Обнаружена версия: $sdkVersion."
	}

	Write-Host "PowerShell: $($PSVersionTable.PSVersion)"
	Write-Host ".NET SDK:   $sdkVersion"
}

function Invoke-DotNetPublish
{
	param(
		[Parameter(Mandatory)]
		[string] $ProjectPath,

		[Parameter(Mandatory)]
		[string] $ProfileName
	)

	$arguments = @(
		"publish",
		$ProjectPath,
		"--configuration",
		"Release",
		"--property:PublishProfile=$ProfileName"
	)

	& dotnet @arguments

	if ($LASTEXITCODE -ne 0)
	{
		throw "Публикация профиля '$ProfileName' завершилась с ошибкой."
	}
}

function New-ReleaseArchive
{
	param(
		[Parameter(Mandatory)]
		[string] $SourceDirectory,

		[Parameter(Mandatory)]
		[string] $ArchivePath
	)

	if (!(Test-Path -LiteralPath $SourceDirectory -PathType Container))
	{
		throw "Каталог публикации не найден: $SourceDirectory"
	}

	$publishedFiles = Get-ChildItem -LiteralPath $SourceDirectory -Force

	if ($publishedFiles.Count -eq 0)
	{
		throw "Каталог публикации пуст: $SourceDirectory"
	}

	if (Test-Path -LiteralPath $ArchivePath)
	{
		Remove-Item -LiteralPath $ArchivePath -Force
	}

	Add-Type -AssemblyName System.IO.Compression.FileSystem

	[System.IO.Compression.ZipFile]::CreateFromDirectory(
		$SourceDirectory,
		$ArchivePath,
		[System.IO.Compression.CompressionLevel]::Optimal,
		$false)
}

Assert-PowerShellVersion
Assert-DotNetSdk

$scriptDirectory = $PSScriptRoot
$solutionDirectory = Split-Path -Parent $scriptDirectory
$projectDirectory = Join-Path $solutionDirectory "YandexTimesheetApp"
$projectPath = Join-Path $projectDirectory "YandexTimesheetApp.csproj"
$publishProfilesDirectory = Join-Path $projectDirectory "Properties\PublishProfiles"

$artifactsDirectory = Join-Path $solutionDirectory "artifacts"
$publishRootDirectory = Join-Path $artifactsDirectory "publish"
$packagesDirectory = Join-Path $artifactsDirectory "packages"

$projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)

$targets = @(
	[PSCustomObject]@{
		ProfileName = "win-x64"
		RuntimeIdentifier = "win-x64"
		ExecutableName = "$projectName.exe"
	},
	[PSCustomObject]@{
		ProfileName = "osx-arm64"
		RuntimeIdentifier = "osx-arm64"
		ExecutableName = $projectName
	}
)

if (!(Test-Path -LiteralPath $projectPath -PathType Leaf))
{
	throw "Файл проекта не найден: $projectPath"
}

foreach ($target in $targets)
{
	$profilePath = Join-Path $publishProfilesDirectory "$($target.ProfileName).pubxml"

	if (!(Test-Path -LiteralPath $profilePath -PathType Leaf))
	{
		throw "Профиль публикации не найден: $profilePath"
	}
}

Write-Step "Подготовка каталога артефактов"

if (Test-Path -LiteralPath $artifactsDirectory)
{
	Remove-Item -LiteralPath $artifactsDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $publishRootDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $packagesDirectory -Force | Out-Null

foreach ($target in $targets)
{
	$profileName = $target.ProfileName
	$runtimeIdentifier = $target.RuntimeIdentifier
	$publishDirectory = Join-Path $publishRootDirectory $runtimeIdentifier
	$archiveName = "$projectName.$runtimeIdentifier.zip"
	$archivePath = Join-Path $packagesDirectory $archiveName

	Write-Step "Публикация профиля $profileName"

	Invoke-DotNetPublish `
		-ProjectPath $projectPath `
		-ProfileName $profileName

	$executablePath = Join-Path $publishDirectory $target.ExecutableName

	if (!(Test-Path -LiteralPath $executablePath -PathType Leaf))
	{
		throw "Исполняемый файл публикации не найден: $executablePath"
	}

	Write-Step "Создание архива $archiveName"

	New-ReleaseArchive `
		-SourceDirectory $publishDirectory `
		-ArchivePath $archivePath

	$archiveInfo = Get-Item -LiteralPath $archivePath

	Write-Host "Создан: $($archiveInfo.FullName)"
	Write-Host "Размер: $([Math]::Round($archiveInfo.Length / 1MB, 2)) МБ"
}

if (!$KeepPublishDirectories)
{
	Write-Step "Удаление промежуточных каталогов публикации"

	Remove-Item -LiteralPath $publishRootDirectory -Recurse -Force
}

Write-Step "Публикация завершена"

Get-ChildItem -LiteralPath $packagesDirectory -File |
	Sort-Object Name |
	Select-Object Name, Length, LastWriteTime |
	Format-Table -AutoSize