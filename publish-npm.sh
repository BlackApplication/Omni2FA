set -euo pipefail
cd "$(dirname "$0")"

NPM_TOKEN=""

if [ -z "$NPM_TOKEN" ] && [ -f publish.env ]; then
    . ./publish.env
fi
if [ -z "$NPM_TOKEN" ]; then
    echo "NPM_TOKEN is empty — paste it at the top of this script, or copy" >&2
    echo "publish.env.example to publish.env and put it there." >&2
    exit 1
fi

DRY=""
if [ "${1:-}" = "--dry-run" ]; then
    DRY="--dry-run"
fi

# Auth goes into a throwaway npmrc rather than the user's ~/.npmrc, so the token
# lives only for the duration of this run. Windows node needs a native path.
NPMRC="$PWD/.npmrc.publish"
trap 'rm -f "$NPMRC"' EXIT
printf '//registry.npmjs.org/:_authToken=%s\n' "$NPM_TOKEN" > "$NPMRC"
if command -v cygpath > /dev/null 2>&1; then
    NPMRC="$(cygpath -w "$NPMRC")"
fi
export NPM_CONFIG_USERCONFIG="$NPMRC"

echo "==> npm whoami"
npm whoami

echo "==> npm run build"
npm run build

publish() {
    name="$1"
    dir="$2"
    version="$(node -p "require('./$dir/package.json').version")"
    if npm view "$name@$version" version > /dev/null 2>&1; then
        echo "==> $name@$version is already on npm — skipping"
        return
    fi
    echo "==> publishing $name@$version"
    npm publish --workspace "$name" $DRY
}

# core first — react pins an exact @omni2fa/core version.
publish @omni2fa/core Core/js
publish @omni2fa/react React/react

echo
echo "done. @omni2fa/react-mui is private and is not published."
