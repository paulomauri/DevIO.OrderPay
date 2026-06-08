# ============================================================
# SQL Server 2025 — Developer Edition
# ============================================================
FROM mcr.microsoft.com/mssql/server:2025-latest

USER root

RUN mkdir -p /var/opt/mssql/data \
             /var/opt/mssql/log \
             /var/opt/mssql/backup \
             /usr/src/app/scripts/sql \
    && chown -R mssql:mssql /var/opt/mssql \
    && chmod -R 755 /var/opt/mssql

COPY ./entrypoint.sh /usr/src/app/scripts/entrypoint.sh

RUN chown mssql:mssql /usr/src/app/scripts/entrypoint.sh \
    && chmod +x /usr/src/app/scripts/entrypoint.sh

ENV ACCEPT_EULA=Y \
    MSSQL_PID=Developer \
    MSSQL_SA_PASSWORD=Mauri@22 \
    MSSQL_COLLATION=SQL_Latin1_General_CP1_CI_AS \
    MSSQL_AGENT_ENABLED=true \
    MSSQL_TCP_PORT=1433

EXPOSE 1433

ENTRYPOINT ["/bin/bash", "/usr/src/app/scripts/entrypoint.sh"]