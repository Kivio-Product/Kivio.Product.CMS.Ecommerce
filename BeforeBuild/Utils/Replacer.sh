#!/bin/bash

# Parámetros
ROOT_DIR=$1          # Carpeta raíz
SEARCH=$2            # Texto a buscar
REPLACE=$3           # Texto de reemplazo
FILE_EXTENSION=$4    # Extensión de archivos

echo "Directorio: $ROOT_DIR"
echo "Buscando: $SEARCH"
echo "Reemplazando con: $REPLACE"
echo "Extensión de archivos: $FILE_EXTENSION"

# Verificar si el directorio raíz existe
if [ ! -d "$ROOT_DIR" ]; then
  echo "Error: El directorio raíz no existe: $ROOT_DIR"
  exit 1
fi

# Buscar y procesar archivos
find "$ROOT_DIR" -type f -name "$FILE_EXTENSION" | while read -r FILE; do
  echo "Procesando archivo: $FILE"
  
  # Leer el contenido del archivo
  CONTENT=$(cat "$FILE")
  
  # Verificar si el texto de reemplazo ya existe
  if echo "$CONTENT" | grep -q "$REPLACE"; then
    echo "La cadena '$REPLACE' ya existe en el archivo $FILE. Omitiendo..."
    continue
  fi
  
  # Verificar si la cadena de búsqueda existe
  if echo "$CONTENT" | grep -q "$SEARCH"; then
    # Reemplazar y escribir de nuevo
    echo "$CONTENT" | sed "s|$SEARCH|$REPLACE|g" > "$FILE"
    echo "Reemplazo exitoso en el archivo: $FILE"
  else
    echo "La cadena '$SEARCH' no se encontró en el archivo $FILE. No se realiza reemplazo."
  fi
done

echo "Proceso de reemplazo completado."
