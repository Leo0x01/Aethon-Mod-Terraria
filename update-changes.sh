#!/bin/bash
# Script para actualizar CHANGES.md automáticamente con cada commit
# Se ejecuta manualmente o se puede integrar en un hook de git

REPO_DIR="/home/z/my-project/AethonMod"
CHANGES_FILE="$REPO_DIR/CHANGES.md"

# Obtener el último commit
LATEST_HASH=$(git -C "$REPO_DIR" rev-parse --short HEAD 2>/dev/null)
LATEST_MSG=$(git -C "$REPO_DIR" log -1 --pretty=format:"%s" 2>/dev/null)

if [ -z "$LATEST_HASH" ]; then
    echo "No se pudo obtener el commit"
    exit 1
fi

# Verificar si el commit ya está en CHANGES.md
if grep -q "## Commit $LATEST_HASH" "$CHANGES_FILE" 2>/dev/null; then
    echo "El commit $LATEST_HASH ya está documentado"
    exit 0
fi

# Insertar el nuevo commit al principio (después del título)
TEMP_FILE=$(mktemp)
echo "# AethonMod — Historial de Cambios" > "$TEMP_FILE"
echo "" >> "$TEMP_FILE"
echo "## Commit $LATEST_HASH — $LATEST_MSG" >> "$TEMP_FILE"
echo "- Commit: $LATEST_HASH" >> "$TEMP_FILE"
echo "- Fecha: $(date '+%Y-%m-%d %H:%M:%S')" >> "$TEMP_FILE"
echo "- Mensaje: $LATEST_MSG" >> "$TEMP_FILE"
echo "- Archivos modificados:" >> "$TEMP_FILE"
git -C "$REPO_DIR" diff-tree --no-commit-id --name-only -r HEAD 2>/dev/null | while read f; do
    echo "  - $f" >> "$TEMP_FILE"
done
echo "" >> "$TEMP_FILE"

# Preservar el resto del archivo
if [ -f "$CHANGES_FILE" ]; then
    tail -n +3 "$CHANGES_FILE" >> "$TEMP_FILE"
fi

mv "$TEMP_FILE" "$CHANGES_FILE"
echo "✅ CHANGES.md actualizado con commit $LATEST_HASH"
