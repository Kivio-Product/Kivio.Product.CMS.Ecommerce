# Proyecto Kivio Product CMS Ecommerce

---

## Requisitos Previos

### **General**
- **.NET SDK**: Asegúrate de tener el SDK de .NET instalado y configurado.
- **Git**: Para la gestión de versiones y sincronización de código.

### **Windows**
- **PowerShell**: Necesario para ejecutar el archivo `init.ps1`.
- **Permisos de ejecución**: Si encuentras problemas, habilita la ejecución de scripts con:

  ```powershell
  Set-ExecutionPolicy Bypass -Scope Process
  ```

### **Linux/MacOS**
- **Bash**: Necesario para ejecutar `init.sh`.
- **Permisos de ejecución**: Asegúrate de que los scripts tengan permisos de ejecución:

  ```bash
  chmod +x init.sh
  chmod +x BeforeBuild/Utils/replacer.sh
  ```

---

## Ejecución del Script de Inicialización

Cada vez que inicies el proyecto, **debes ejecutar el script `init` correspondiente** para preparar correctamente el entorno. Esto incluye tareas como restaurar paquetes, ejecutar configuraciones iniciales y preparar dependencias.

---

### **Pasos para Ejecutar en Windows**
1. **Navega al directorio del proyecto**:

   ```powershell
   cd "~\Kivio.Product.CMS.Ecommerce"
   ```

2. **Ejecuta el script de inicialización**:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\init.ps1
   ```

---

### **Pasos para Ejecutar en Linux/MacOS**
1. **Navega al directorio del proyecto**:

   ```bash
   cd ~/Kivio.Product.CMS.Ecommerce
   ```

2. **Ejecuta el script de inicialización**:

   ```bash
   ./init.sh
   ```

---

## Qué Hacen los Scripts

### **Windows (`init.ps1`)**
- **Restaura dependencias**: Ejecuta `dotnet restore` para restaurar paquetes NuGet.
- **Reemplazo de texto**: Actualiza archivos de configuración específicos usando `Replacer.ps1`.
- **Configuraciones adicionales**: Ejecuta cualquier configuración adicional definida.

### **Linux/MacOS (`init.sh`)**
- **Restaura dependencias**: Ejecuta `dotnet restore` para restaurar paquetes NuGet.
- **Reemplazo de texto**: Actualiza archivos de configuración usando `replacer.sh`.
- **Configuraciones adicionales**: Ejecuta cualquier otro script definido en `init.sh`.

---

## Solución de Problemas

Si encuentras errores al ejecutar los scripts:

### General
- **Permisos**: Asegúrate de tener permisos para ejecutar scripts.
- **Rutas incorrectas**: Verifica que te encuentres en la raíz del proyecto.
- **Dependencias faltantes**: Si `dotnet` no está disponible, asegúrate de que esté instalado y configurado en tu PATH.

### Windows
- **Error de ejecución en PowerShell**: Verifica que la política de ejecución esté configurada correctamente.

  ```powershell
  Set-ExecutionPolicy Bypass -Scope Process
  ```

### Linux/MacOS
- **Error de permisos**: Asegúrate de haber otorgado permisos de ejecución a los scripts con `chmod +x`.
- **Error de bash no encontrado**: Asegúrate de que Bash esté instalado y configurado correctamente.

