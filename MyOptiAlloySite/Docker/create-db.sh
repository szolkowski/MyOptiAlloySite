#!/bin/bash

# CMS and Commerce each own a database. Commerce resolves its own by the
# EcfSqlConnection connection string name, so both must exist before the web
# container starts or the Commerce schema updater fails on a missing catalog.
DATABASES=("${DB_NAME}" "${COMMERCE_DB_NAME}")

query=""
for db in "${DATABASES[@]}"; do
    query+="IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '${db}') CREATE DATABASE [${db}];"
done

echo "Creating databases: ${DATABASES[*]}"

let result=1

for i in {1..100}; do
    /opt/mssql-tools18/bin/sqlcmd -b -S localhost -U sa -P "$SA_PASSWORD" -Q "$query" -C
    let result=$?
    if [ $result -eq 0 ]; then
        echo "Creating databases completed"
        break
    else
        echo "Creating databases. Not ready yet..."
        sleep 1
    fi
done
