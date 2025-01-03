# Obtener el directorio raíz del proyecto
$SolutionDir = (Get-Location).Path

# Mostrar información para diagnóstico
Write-Host "Directorio raíz detectado: $SolutionDir"

# Verificar que el directorio raíz existe
if (-Not (Test-Path $SolutionDir)) {
    Write-Error "El directorio raíz no existe: $SolutionDir"
    exit 1
}

# Definir rutas de scripts
$ReplacerScript = Join-Path $SolutionDir "BeforeBuild\Utils\Replacer.ps1"

# Script 1: Reemplazar rutas de archivos en archivos .csproj
$TargetDir = Join-Path $SolutionDir "NopCommerce\src"
$SearchPattern = "SolutionDir)\"
$ReplacePattern = "SolutionDir)NopCommerce\src\"
$FileExtension = "*.csproj"

# Ejecutar el script Replacer
if (Test-Path $ReplacerScript) {
    Write-Host "Ejecutando Replacer.ps1 --> Reemplazar rutas de archivos en archivos .csproj..."
    powershell -ExecutionPolicy Bypass -File $ReplacerScript $TargetDir $SearchPattern $ReplacePattern $FileExtension
} else {
    Write-Warning "No se encontró el script: $ReplacerScript"
}

# Copiar la carpeta Libraries
$LibrariesSource = Join-Path $SolutionDir "CustomEcommerce\NopCommerce\Libraries\*"
$LibrariesTarget = Join-Path $TargetDir "Libraries"
if (Test-Path $LibrariesSource) {
    Write-Host "Copiando la carpeta Libraries..."
    xcopy /Y /E $LibrariesSource $LibrariesTarget
} else {
    Write-Warning "No se encontró la carpeta Libraries en: $LibrariesSource"
}

# Copiar la carpeta Presentation
$PresentationSource = Join-Path $SolutionDir "CustomEcommerce\NopCommerce\Presentation\*"
$PresentationTarget = Join-Path $TargetDir "Presentation"
if (Test-Path $PresentationSource) {
    Write-Host "Copiando la carpeta Presentation..."
    xcopy /Y /E $PresentationSource $PresentationTarget
} else {
    Write-Warning "No se encontró la carpeta Presentation en: $PresentationSource"
}

# Agrega más scripts si es necesario
# Ejemplo: Ejecutar un script de configuración de dependencias
# $ConfigScript = Join-Path $SolutionDir "Setup\ConfigureDependencies.ps1"
# if (Test-Path $ConfigScript) {
#     Write-Host "Ejecutando ConfigureDependencies.ps1..."
#     powershell -ExecutionPolicy Bypass -File $ConfigScript
# } else {
#     Write-Warning "No se encontró el script: $ConfigScript"
# }

Write-Host "Inicialización completada con éxito."