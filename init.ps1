# Obtener el directorio raíz del proyecto
$SolutionDir = (Get-Location).Path

# Mostrar información para diagnóstico
Write-Host "Directorio raíz detectado: $SolutionDir"

# Definir rutas de scripts
$ReplacerScript = "$SolutionDir\BeforeBuild\Utils\Replacer.ps1"

# Script 1: Reemplazar rutas de archivos en archivos .csproj
$TargetDir = "$SolutionDir\NopCommerce\src"
$SearchPattern = "SolutionDir)\"
$ReplacePattern = "SolutionDir)NopCommerce\src\"
$FileExtension = "*.csproj"

# Ejecutar el script Replacer
Write-Host "Ejecutando Replacer.ps1 --> Reemplazar rutas de archivos en archivos .csproj..."
powershell -ExecutionPolicy Bypass -File $ReplacerScript $TargetDir $SearchPattern $ReplacePattern $FileExtension

# Agrega más scripts si es necesario
# Ejemplo: Ejecutar un script de configuración de dependencias
# $ConfigScript = "$SolutionDir\Setup\ConfigureDependencies.ps1"
# Write-Host "Ejecutando ConfigureDependencies.ps1..."
# powershell -ExecutionPolicy Bypass -File $ConfigScript

Write-Host "Inicialización completada con éxito."