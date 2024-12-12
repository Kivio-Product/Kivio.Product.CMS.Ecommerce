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
    
    # Verifica si el texto que se va a insertar ya existe en el archivo
    if ($content -match [regex]::Escape($replace)) {
        Write-Host "La cadena '$replace' ya existe en el archivo $file, omitiendo reemplazo..."
        continue
    }

    # Verifica si la cadena de búsqueda existe en el archivo
    if ($content -match [regex]::Escape($search)) {
        # Realizar el reemplazo
        $newContent = $content -replace [regex]::Escape($search), $replace
        Set-Content -Path $file.FullName -Value $newContent
        Write-Host "Reemplazo exitoso en el archivo: $file"
    } else {
        Write-Host "La cadena '$search' no se encontró en el archivo $file, no se realiza reemplazo."
    }
}

Write-Host "Proceso de reemplazo completado."
