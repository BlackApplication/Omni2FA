set -euo pipefail
cd "$(dirname "$0")"

NUGET_API_KEY=""

SOURCE="https://api.nuget.org/v3/index.json"

DRY=""
if [ "${1:-}" = "--dry-run" ]; then
    DRY="1"
fi

if [ -z "$NUGET_API_KEY" ] && [ -f publish.env ]; then
    . ./publish.env
fi
if [ -z "$NUGET_API_KEY" ] && [ -z "$DRY" ]; then
    echo "NUGET_API_KEY is empty — paste it at the top of this script, or copy" >&2
    echo "publish.env.example to publish.env and put it there." >&2
    exit 1
fi

echo "==> clearing artifacts/"
rm -rf artifacts

echo "==> dotnet pack"
dotnet pack .Net/Omni2FA.slnx -c Release -o artifacts

if [ -n "$DRY" ]; then
    echo
    echo "dry run — packed but not pushed:"
    ls -1 artifacts
    exit 0
fi

for package in artifacts/*.nupkg; do
    echo "==> pushing $(basename "$package")"
    dotnet nuget push "$package" --api-key "$NUGET_API_KEY" --source "$SOURCE" --skip-duplicate
done

echo
echo "done. nuget.org validates and indexes for a few minutes before the packages"
echo "are installable — the 'not indexed yet' banner is expected."
