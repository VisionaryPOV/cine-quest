#!/usr/bin/env bash
# Run Cine Quest pure-logic tests (shipped Core sources).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
if [[ -x "$HOME/.dotnet/dotnet" ]]; then
  export PATH="$HOME/.dotnet:$PATH"
  export DOTNET_ROOT="$HOME/.dotnet"
fi
cd "$ROOT"
dotnet run --project Tools/PureTests -c Release
