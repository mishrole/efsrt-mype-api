# efsrt-mype-api

## Crear una migración manualmente

1. Ingresa al directorio Mype:

```bash
  cd Mype
```

2. Ejecutar el script indicando el nombre de la migración:

```powershell
  powershell -ExecutionPolicy Bypass -File .\add-migration.ps1 MIGRATION_NAME
```

## Regenerar la última migración

Elimina la última migración y vuelve a generarla con el modelo actual.

> Sólo aplica si la última migración aún no ha sido aplicada en la BD.

```powershell
  powershell -ExecutionPolicy Bypass -File .\regenerate-migration.ps1 MIGRATION_NAME
```
