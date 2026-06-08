#!/bin/bash
set -e

MSSQL_DIR="/var/opt/mssql"
MSSQL_USER="mssql"

# ---- Step 1: Fix ownership of mounted volumes ----
echo "🔧  Fixing volume ownership for '${MSSQL_USER}'..."
chown -R ${MSSQL_USER}:${MSSQL_USER} "${MSSQL_DIR}"
chmod -R 755 "${MSSQL_DIR}"
echo "✅  Ownership fixed: ${MSSQL_DIR} → ${MSSQL_USER}"

# ---- Step 2: Run SQL init scripts AFTER SQL Server is ready ----
init_scripts() {
    local SCRIPT_DIR="/usr/src/app/scripts/sql"
    local MAX_RETRIES=40
    local RETRY=0

    echo "⏳  Waiting for SQL Server to be ready..."
    until /opt/mssql-tools18/bin/sqlcmd \
            -S localhost -U SA -P "${MSSQL_SA_PASSWORD}" \
            -No -Q "SELECT 1" > /dev/null 2>&1; do
        RETRY=$((RETRY + 1))
        if [ "${RETRY}" -ge "${MAX_RETRIES}" ]; then
            echo "❌  SQL Server did not become ready after ${MAX_RETRIES} attempts."
            exit 1
        fi
        echo "   attempt ${RETRY}/${MAX_RETRIES} — retrying in 2s..."
        sleep 2
    done

    echo "✅  SQL Server is ready."

    shopt -s nullglob
    scripts=("${SCRIPT_DIR}"/*.sql)
    if [ ${#scripts[@]} -eq 0 ]; then
        echo "ℹ️   No SQL init scripts found in ${SCRIPT_DIR} — skipping."
    else
        for script in "${scripts[@]}"; do
            echo "▶  Running: $(basename "${script}")"
            /opt/mssql-tools18/bin/sqlcmd \
                -S localhost -U SA -P "${MSSQL_SA_PASSWORD}" \
                -No -i "${script}"
            echo "✅  Done: $(basename "${script}")"
        done
    fi

    echo "🚀  Initialisation complete. SQL Server is running."
}

# Run init scripts in background — doesn't block sqlservr
init_scripts &

# ---- Step 3: Drop to mssql user and start SQL Server as PID 1 ----
echo "▶  Starting SQL Server 2025 Developer Edition as '${MSSQL_USER}'..."
exec su -s /bin/bash ${MSSQL_USER} -c "exec /opt/mssql/bin/sqlservr"