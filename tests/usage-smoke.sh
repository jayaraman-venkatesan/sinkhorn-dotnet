#!/usr/bin/env bash

set -euo pipefail

dotnet_command="${DOTNET_COMMAND:-dotnet}"
output_file="$(mktemp)"
trap 'rm -f "$output_file"' EXIT

if ! "$dotnet_command" run --project examples/Usage -c Release >"$output_file"; then
  cat "$output_file"
  echo "Usage example exited unsuccessfully." >&2
  exit 1
fi

cat "$output_file"

for expected in \
  "Basic: usable" \
  "LogDomain: usable" \
  "Basic zero support: NumericalBreakdown"
do
  if ! grep -Fqx "$expected" "$output_file"; then
    echo "Missing expected output: $expected" >&2
    exit 1
  fi
done
