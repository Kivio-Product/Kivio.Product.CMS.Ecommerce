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
$TargetDir = Join-Path $SolutionDir "NopCommerce\src"

# Validar que Replacer.ps1 existe
if (-Not (Test-Path $ReplacerScript)) {
    Write-Error "El script Replacer.ps1 no existe en: $ReplacerScript"
    exit 1
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

# Copiar la carpeta Plugins
$PluginsSource = Join-Path $SolutionDir "CustomEcommerce\NopCommerce\Plugins\*"
$PluginsTarget = Join-Path $TargetDir "Plugins"
if (Test-Path $LibrariesSource) {
    Write-Host "Copiando la carpeta Plugins..."
    xcopy /Y /E $PluginsSource $PluginsTarget
} else {
    Write-Warning "No se encontró la carpeta Plugins en: $PluginsSource"
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

# Copiar la carpeta Tests
$TestsSource = Join-Path $SolutionDir "CustomEcommerce\NopCommerce\Tests\*"
$TestsTarget = Join-Path $TargetDir "Tests"
if (Test-Path $TestsSource) {
    Write-Host "Copiando la carpeta Tests..."
    xcopy /Y /E $TestsSource $TestsTarget
} else {
    Write-Warning "No se encontró la carpeta Tests en: $TestsSource"
}

# Script 1: Reemplazar rutas de archivos en archivos .csproj
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