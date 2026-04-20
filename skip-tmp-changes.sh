#!/usr/bin/env bash
# Tell git to ignore local changes to the TMP font assets that Unity
# re-serializes on open. Pass --undo to re-track them.

set -euo pipefail

FILES=(
  "Assets/TextMesh Pro/Resources/Fonts & Materials/LCD_2_SDF.asset"
  "Assets/TextMesh Pro/Resources/Fonts & Materials/LCD_Mono_SDF.asset"
)

MODE="skip"
if [[ "${1:-}" == "--undo" ]]; then
  MODE="no-skip"
fi

cd "$(dirname "$0")"

for f in "${FILES[@]}"; do
  if [[ ! -f "$f" ]]; then
    echo "warning: not found, skipping: $f" >&2
    continue
  fi
  git update-index "--$MODE-worktree" -- "$f"
  echo "$MODE-worktree: $f"
done
