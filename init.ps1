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

# Validar que Replacer.ps1 existe
if (-Not (Test-Path $ReplacerScript)) {
    Write-Error "El script Replacer.ps1 no existe en: $ReplacerScript"
    exit 1
}

# Script 1: Reemplazar rutas de archivos en archivos .csproj
$TargetDir = Join-Path $SolutionDir "NopCommerce\src"
$SearchPattern = "SolutionDir)\"  # Texto a buscar
$ReplacePattern = "SolutionDir)NopCommerce\src\"  # Texto con el que se reemplaza
$FileExtension = "*.csproj"  # Extensión de los archivos a procesar

# Ejecutar el script Replacer
Write-Host "Ejecutando Replacer.ps1 para archivos .csproj..."
powershell -ExecutionPolicy Bypass -File $ReplacerScript `
    -rootDir $TargetDir `
    -search $SearchPattern `
    -replace $ReplacePattern `
    -fileExtension $FileExtension

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