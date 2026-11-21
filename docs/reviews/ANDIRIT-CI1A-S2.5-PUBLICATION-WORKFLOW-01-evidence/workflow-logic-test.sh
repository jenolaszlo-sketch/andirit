#!/usr/bin/env bash
# PUBLICATION-WORKFLOW-01: exercise the corrected publish.yml logic locally.
set -u
cd /d/m54b-release/workflow-check || exit 99

tag_validation() {
  VERSION="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' Directory.Build.props)"
  if [ -z "$VERSION" ]; then
    echo "RESULT: error-no-version"; return 1
  fi
  if [ "$GITHUB_REF_TYPE" = "tag" ]; then
    if [ "$GITHUB_REF_NAME" != "v$VERSION" ]; then
      echo "RESULT: error-tag-mismatch ($GITHUB_REF_NAME vs v$VERSION)"; return 1
    fi
    echo "RESULT: ok-tag-matches ($GITHUB_REF_NAME = v$VERSION)"
  else
    echo "RESULT: ok-non-tag ($GITHUB_REF_NAME, repo version $VERSION)"
  fi
}

inventory_gate() {
  local nupkg snupkg missing
  nupkg=$(ls -1 ./artifacts/*.nupkg 2>/dev/null | wc -l)
  snupkg=$(ls -1 ./artifacts/*.snupkg 2>/dev/null | wc -l)
  local expected_ids="Andirit Andirit.Abstractions Andirit.CSharp Andirit.LatticeDb Andirit.Testing Andirit.TreeSitter"
  missing=""
  for id in $expected_ids; do
    ls ./artifacts/"$id".*.nupkg >/dev/null 2>&1 || missing="$missing $id"
  done
  if [ "$nupkg" -ne 6 ] || [ "$snupkg" -ne 6 ] || [ -n "$missing" ]; then
    echo "RESULT: error-incomplete ($nupkg nupkg, $snupkg snupkg; missing:$missing)"; return 1
  fi
  echo "RESULT: ok-inventory ($nupkg nupkg, $snupkg snupkg)"
}

echo "== case 1: tag v0.2.0-preview.7 (the intended tag) =="
GITHUB_REF_TYPE=tag GITHUB_REF_NAME=v0.2.0-preview.7 tag_validation

echo "== case 2: tag v0.2.0-preview.6 (stale version tag) =="
GITHUB_REF_TYPE=tag GITHUB_REF_NAME=v0.2.0-preview.6 tag_validation

echo "== case 3: tag v0.2.0-preview.8 (future version tag) =="
GITHUB_REF_TYPE=tag GITHUB_REF_NAME=v0.2.0-preview.8 tag_validation

echo "== case 4: workflow_dispatch on main (non-tag) =="
GITHUB_REF_TYPE=branch GITHUB_REF_NAME=main tag_validation

echo "== case 5: inventory gate against the reproduced artifacts =="
inventory_gate