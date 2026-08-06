#!/bin/bash

set -e

git add .

if git diff --cached --quiet; then
    echo "Nada para commitar. Saindo.."
    exit 0

fi

read -p "Commit: " msg

if [ -z "$msg" ]; then
    echo "Commit vazio nao e commit, cara."
    exit 1

fi

git commit -m "$msg"
git push
