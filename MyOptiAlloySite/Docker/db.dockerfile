# SQL Server 2025 is published for linux/amd64 only; pin it so the image also
# builds on arm64 hosts (Apple Silicon), where it runs under emulation.
FROM --platform=linux/amd64 mcr.microsoft.com/mssql/server:2025-latest AS base

ENV ACCEPT_EULA=Y

USER root

WORKDIR /src
COPY ./Docker/create-db.sh .
RUN chmod +x /src/create-db.sh

USER mssql

EXPOSE 1433

ENTRYPOINT /src/create-db.sh & /opt/mssql/bin/sqlservr
