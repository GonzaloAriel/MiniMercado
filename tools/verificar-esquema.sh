#!/usr/bin/env bash
#
# verificar-esquema.sh - la barrera que detecta deriva entre el modelo de EF y
# las migraciones. Ver P-025.
#
# Son tres chequeos, y cada uno atrapa algo que los otros no:
#
#   A) Deriva modelo/migraciones. Compara una base creada solo con migraciones
#      contra una creada desde el modelo. Es lo que encuentra P-002 (columnas que
#      ninguna migracion crea), P-023 (indice y FK que faltaban) y P-029
#      (EgresosCaja.Anulado inexistente -> el modulo daba 500 en todos los
#      entornos). Lo hace tools/SchemaCheck.
#
#   B) El modelo esta adelantado al snapshot. Detecta "cambie el modelo y no
#      genere la migracion", que es el error humano. Se comprueba generando una
#      migracion de prueba y mirando si tiene algo dentro: si esta vacia, el
#      modelo ya estaba enteramente en el snapshot (ver P-031: en ese caso la
#      migracion vacia no arregla nada).
#
#   C) Hay migraciones sin commitear. Cierra el otro agujero: "genere la
#      migracion y me olvide de git add", o "la migrate todavia no la comitee".
#
# Por que NO alcanza con un paso solo:
#   - A no ve el error humano (si el snapshot ya tiene el cambio, A da verde y
#     el problema sigue).
#   - B no ve la deriva real: si el snapshot va adelantado, B da verde y A
#     sigue sin encontrar la deriva.
#   - C no ve nada del modelo; solo del working tree.
#
# Uso:
#   ConnectionStrings__MiniMercadoDB='Server=...;...' tools/verificar-esquema.sh
#
# Es agnóstico de plataforma: corre en una máquina y también dentro de cualquier
# CI (GitHub Actions, GitLab, Jenkins) sin cambios. Solo necesita el SDK de .NET
# 6 con runtime (para ejecutar el binario net6.0) y dotnet-ef 6.
#
# Códigos de salida:
#   0  todo bien
#   1  deriva entre el modelo y las migraciones (chequeo A)
#   2  falta configuración (ConnectionStrings__MiniMercadoDB)
#   3  el modelo está adelantado al snapshot (chequeo B)
#   4  hay migraciones sin commitear (chequeo C)
#   5  error interno al correr los chequeos

set -uo pipefail

# Las rutas se resuelven desde donde está este script, no desde el cwd: el
# script se puede llamar desde cualquier lado.
RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROYECTO="$RAIZ/MiniMercado"
MIGRACIONES="$PROYECTO/Migrations"
SCHEMACHECK="$RAIZ/tools/SchemaCheck"

# El binario es net6.0 y el SDK del sistema puede ser otro (P-013). Se permite
# forzar el muxer con DOTNET_MUXER.
seleccionar_dotnet() {
  if [ -n "${DOTNET_MUXER:-}" ]; then
    echo "$DOTNET_MUXER"
    return
  fi
  # ~/.dotnet es donde se instaló el runtime 6 sin sudo.
  if [ -x "$HOME/.dotnet/dotnet" ]; then
    echo "$HOME/.dotnet/dotnet"
    return
  fi
  command -v dotnet
}

DOTNET="$(seleccionar_dotnet)"

if [ -z "$DOTNET" ]; then
  echo "No se encontro dotnet. Instalalo o apuntá DOTNET_MUXER al muxer correcto." >&2
  exit 5
fi

if [ -z "${ConnectionStrings__MiniMercadoDB:-}" ]; then
  cat >&2 <<'MSG'
Falta la variable de entorno ConnectionStrings__MiniMercadoDB.

El connection string llega por variable de entorno, nunca por appsettings.json
(ver P-010). Ejemplo:

  ConnectionStrings__MiniMercadoDB='Server=127.0.0.1,1433;Database=MiniMercadoDB;User Id=sa;Password=...;TrustServerCertificate=True;' \
    tools/verificar-esquema.sh
MSG
  exit 2
fi

# La migración de prueba que genera el chequeo B. Nombre sin timestamp para que
# sea obvio que es temporal si alguien la ve.
NOMBRE_TEMPORAL="VerificacionTemporalDeEsquema"

# Si el script muere entre "add" y "remove", queda una migración basura y el
# snapshot modificado. Este trap lo revierte siempre.
limpiar_migracion_temporal() {
  local archivos
  archivos="$(ls "$MIGRACIONES"/*_"$NOMBRE_TEMPORAL".cs 2>/dev/null | grep -v Designer || true)"
  if [ -n "$archivos" ]; then
    echo "--- limpieza: revirtiendo la migración temporal ---"
    (cd "$PROYECTO" && "$DOTNET" ef migrations remove --force >/dev/null 2>&1) || \
      echo "AVISO: no se pudo revertir la migración temporal. Revisá $MIGRACIONES a mano." >&2
  fi
}
trap limpiar_migracion_temporal EXIT

echo "================================================================"
echo " Verificación de esquema (P-025)"
echo " dotnet: $DOTNET"
echo " proyecto: $PROYECTO"
echo "================================================================"

# ---------------------------------------------------------------------------
# A) Deriva entre el modelo y las migraciones
# ---------------------------------------------------------------------------
echo
echo "[A] Deriva modelo/migraciones (SchemaCheck)..."

# SchemaCheck tiene su propia copia de la app en su carpeta de salida: hay que
# rebuildarlo siempre, o corre contra un binario viejo que puede no tener la
# migración más nueva (ver 00_MEMORIA.md §5).
(cd "$RAIZ" && "$DOTNET" build "$SCHEMACHECK/SchemaCheck.csproj" >/tmp/verificar-esquema-build.log 2>&1)
if [ $? -ne 0 ]; then
  echo "     no se pudo compilar SchemaCheck. Log en /tmp/verificar-esquema-build.log" >&2
  exit 5
fi

(cd "$RAIZ" && "$DOTNET" run --project "$SCHEMACHECK" --no-build 2>&1 | sed 's/^/     /')
estado_a=${PIPESTATUS[0]}

case $estado_a in
  0) echo "     OK: sin deriva." ;;
  2) echo "     FALLA: falta configuracion de la connection string." >&2; exit 2 ;;
  *) echo "     FALLA: hay deriva entre el modelo y las migraciones (P-025)." >&2; exit 1 ;;
esac

# ---------------------------------------------------------------------------
# B) El modelo esta adelantado al snapshot
# ---------------------------------------------------------------------------
echo
echo "[B] El modelo está adelantado al snapshot..."

(cd "$PROYECTO" && "$DOTNET" ef migrations add "$NOMBRE_TEMPORAL" --output-dir Migrations >/tmp/verificar-esquema-add.log 2>&1)
if [ $? -ne 0 ]; then
  echo "     no se pudo correr 'migrations add'. Log en /tmp/verificar-esquema-add.log" >&2
  exit 5
fi

archivo_temporal="$(ls "$MIGRACIONES"/*_"$NOMBRE_TEMPORAL".cs 2>/dev/null | grep -v Designer | head -1)"
if [ -z "$archivo_temporal" ]; then
  echo "     error: 'migrations add' no genero archivo." >&2
  exit 5
fi

# Un Up() vacío no arregla nada: significa que el snapshot ya tenía todo el
# modelo, o sea que la deriva es de otro tipo (P-031).
operaciones="$(grep -c 'migrationBuilder\.' "$archivo_temporal" || true)"

# Se revierte antes de decidir, para no dejar el working tree sucio.
limpiar_migracion_temporal
trap - EXIT

if [ "$operaciones" -gt 0 ]; then
  echo "     FALLA: el modelo tiene cambios que no estan en ninguna migracion ($operaciones operacion(es))." >&2
  echo "     Corré 'dotnet ef migrations add <Nombre>' y commiteá el modelo y la migracion juntos." >&2
  exit 3
fi

echo "     OK: el modelo esta enteramente en el snapshot."

# ---------------------------------------------------------------------------
# C) Migraciones sin commitear
# ---------------------------------------------------------------------------
echo
echo "[C] Migraciones sin commitear..."

sin_commitear="$(cd "$RAIZ" && git status --porcelain -- "$MIGRACIONES" 2>/dev/null | grep -E '^\?\?|^.[MD]' || true)"

if [ -n "$sin_commitear" ]; then
  echo "     FALLA: hay cambios en Migrations/ sin commitear:" >&2
  echo "$sin_commitear" | sed 's/^/       /' >&2
  echo "     Un cambio de esquema nunca se pushea sin su migracion en el mismo commit." >&2
  exit 4
fi

echo "     OK: Migrations/ limpio."

# ---------------------------------------------------------------------------
echo
echo "================================================================"
echo " OK: modelo y migraciones consistentes."
echo "================================================================"
exit 0