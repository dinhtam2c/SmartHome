#!/bin/bash

SKIP_ADD=false
MIGRATION_NAME=""

for arg in "$@"; do
	if [ "$arg" = "-u" ]; then
		SKIP_ADD=true
	else
		MIGRATION_NAME="$arg"
	fi
done

if [ "$SKIP_ADD" = false ] && [ -z "$MIGRATION_NAME" ]; then
	echo "Usage: $0 <migration-name> [-u]"
	exit 1
fi

if [ "$SKIP_ADD" = false ]; then
	dotnet-ef migrations add "$MIGRATION_NAME" -p Infrastructure -s WebAPI || exit 1
fi

dotnet-ef database update -p Infrastructure -s WebAPI
