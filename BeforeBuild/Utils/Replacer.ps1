param(
    [string]$rootDir,        # La carpeta raíz donde buscar
    [string]$search,         # El texto a buscar
    [string]$replace,        # El texto con el que reemplazar
    [string]$fileExtension   # La extensión de los archivos a procesar (por ejemplo, *.txt)
)
Write-Host "rootDir $rootDir"
Write-Host "search $search"
Write-Host "replace $replace"
Write-Host "fileExtension $fileExtension"
# Verificar si el directorio raíz existe
if (-not (Test-Path $rootDir)) {
    Write-Host "El directorio raíz no existe: $rootDir"
    exit 1
}
# Obtener todos los archivos con la extensión dada de manera recursiva
$files = Get-ChildItem -Path $rootDir -Recurse -Filter $fileExtension
foreach ($file in $files) {
    Write-Host "Procesando archivo: $file"
    # Leer el contenido del archivo
    $content = Get-Content -Path $file.FullName -Raw
    # Reemplazar las referencias
    $content = $content -replace [regex]::Escape($search), $replace
    # Escribir el contenido modificado de nuevo en el archivo
    Set-Content -Path $file.FullName -Value $content
    Write-Host "Reemplazo exitoso en el archivo: $file"
}
Write-Host "Proceso de reemplazo completado."