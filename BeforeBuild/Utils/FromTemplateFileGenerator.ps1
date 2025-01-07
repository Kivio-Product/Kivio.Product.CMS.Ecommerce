Param(
    [Parameter(Mandatory = $true)]
    [string]
    $EnvironmentName,

    [Parameter(Mandatory = $true)]
    [string]
    $SourceFile,

    [Parameter(Mandatory = $true)]
    [string]
    $DestinationFile,

    [Parameter(Mandatory = $true)]
    [string]
    $Pattern
)

# Convertir el nombre del entorno a mayúsculas
$EnvironmentName = $EnvironmentName.ToUpper()

Write-Host "Reemplazando '$Pattern' con '$EnvironmentName' en el archivo fuente: $SourceFile" -ForegroundColor Cyan
Write-Host "El archivo modificado será generado en: $DestinationFile" -ForegroundColor Cyan

# Validar si el archivo fuente existe
if (Test-Path $SourceFile) {
    Write-Host "Procesando archivo fuente: $SourceFile" -ForegroundColor Green

    # Leer el contenido del archivo fuente
    $content = Get-Content -Path $SourceFile -Raw

    # Reemplazar el patrón con el nombre del entorno en mayúsculas
    $escapedPattern = [regex]::Escape($Pattern) # Escapar el patrón para usarlo en la expresión regular
    $newContent = $content -replace $escapedPattern, $EnvironmentName

    # Crear o sobrescribir el archivo destino con el contenido modificado
    Set-Content -Path $DestinationFile -Value $newContent

    Write-Host "Archivo generado exitosamente en: $DestinationFile" -ForegroundColor Green
} else {
    Write-Host "Error: No se encontró el archivo fuente: $SourceFile" -ForegroundColor Red
    exit 1
}

Write-Host "Reemplazo finalizado." -ForegroundColor Cyan
