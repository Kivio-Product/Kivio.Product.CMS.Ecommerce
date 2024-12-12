#!/bin/bash

# Obtener el directorio raíz (directorio del script actual)
SOLUTION_DIR=$(dirname "$(realpath "$0")")

# Mostrar información para diagnóstico
echo "Directorio raíz detectado: $SOLUTION_DIR"

# Definir la ruta del script de reemplazo
REPLACER_SCRIPT="$SOLUTION_DIR/BeforeBuild/Utils/replacer.sh"

# Configurar parámetros (usa comillas simples para evitar escapes)
TARGET_DIR="$SOLUTION_DIR/NopCommerce/src"
SEARCH_PATTERN='SolutionDir)\\'
REPLACE_PATTERN='SolutionDir)NopCommerce\\src'
FILE_EXTENSION="*.csproj"

# Ejecutar el script Replacer
echo "Ejecutando replacer.sh..."
bash "$REPLACER_SCRIPT" "$TARGET_DIR" "$SEARCH_PATTERN" "$REPLACE_PATTERN" "$FILE_EXTENSION"

echo "Inicialización completada con éxito."
