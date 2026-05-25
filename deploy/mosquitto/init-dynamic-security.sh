#!/bin/sh
set -eu

config_file=/mosquitto/data/dynamic-security.json

if [ ! -s "$config_file" ]; then
    umask 077
    mosquitto_ctrl dynsec init \
        "$config_file" \
        "$DYNSEC_ADMIN_USERNAME" \
        "$DYNSEC_ADMIN_PASSWORD"
fi

chown -R 1883:1883 /mosquitto/data
