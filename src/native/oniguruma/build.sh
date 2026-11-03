#!/usr/bin/env bash
# Builds the native Oniguruma library for Linux (libonig.so) or macOS (universal libonig.dylib).
#
#   1. Downloads the pinned Oniguruma release and checks its SHA-256 (same as build.ps1).
#   2. Builds it with CMake as a shared library, -fPIC, -O2, no -march (portable).
#   3. Copies the library to artifacts/native/<rid>/.
#
# Requirements: CMake and a C compiler (Xcode command line tools on macOS), curl, tar, sha256sum or shasum.
#
# Usage: ./src/native/oniguruma/build.sh --rid linux-x64|linux-arm64|osx
#   linux-*  builds for the machine it runs on (use an x64 or an arm64 runner); the RID only names the output folder.
#   osx      builds a universal binary (x86_64 + arm64), minimum macOS 12.0.
set -euo pipefail

# --- Pinned version. Keep in sync with build.ps1 (see docs/adr/0001-baseline.md). ---
VERSION='6.9.10'
URL="https://github.com/kkos/oniguruma/releases/download/v${VERSION}/onig-${VERSION}.tar.gz"
SHA256='2a5cfc5ae259e4e97f86b68dfffc152cdaffe94e2060b770cb827238d769fc05'

RID=''
while [ $# -gt 0 ]; do
  case "$1" in
    --rid) RID="${2:-}"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done
case "$RID" in
  linux-x64|linux-arm64|osx) ;;
  *) echo "Usage: $0 --rid linux-x64|linux-arm64|osx" >&2; exit 2 ;;
esac

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$here/../../.." && pwd)"
work="$here/.build"
archive="$work/onig-${VERSION}.tar.gz"
sources="$work/onig-${VERSION}"
build_dir="$work/build-$RID"
output="$repo_root/artifacts/native/$RID"
mkdir -p "$work" "$output"

if [ ! -f "$archive" ]; then
  echo "Downloading $URL"
  curl -fsSL "$URL" -o "$archive"
fi

if command -v sha256sum >/dev/null 2>&1; then
  actual="$(sha256sum "$archive" | cut -d' ' -f1)"
else
  actual="$(shasum -a 256 "$archive" | cut -d' ' -f1)"
fi
if [ "$actual" != "$SHA256" ]; then
  rm -f "$archive"
  echo "SHA-256 mismatch for $archive. Expected $SHA256, got $actual." >&2
  exit 1
fi

[ -d "$sources" ] || tar -xzf "$archive" -C "$work"

args=(-DBUILD_SHARED_LIBS=ON -DENABLE_POSIX_API=OFF -DENABLE_BINARY_COMPATIBLE_POSIX_API=OFF
      -DCMAKE_BUILD_TYPE=Release -DCMAKE_POSITION_INDEPENDENT_CODE=ON -DCMAKE_C_FLAGS=-O2)
if [ "$RID" = osx ]; then
  args+=("-DCMAKE_OSX_ARCHITECTURES=x86_64;arm64" -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0)
fi

cmake -S "$sources" -B "$build_dir" "${args[@]}"
cmake --build "$build_dir" --config Release

# CMake names the library libonig.so.<major> / libonig.<major>.dylib plus symlinks; ship the real file under the plain name.
if [ "$RID" = osx ]; then lib=libonig.dylib; else lib=libonig.so; fi
built="$(find "$build_dir" -maxdepth 1 -name "$lib" | head -n 1)"
[ -n "$built" ] || { echo "$lib was not produced in $build_dir." >&2; exit 1; }
cp -L "$built" "$output/$lib"
cp "$sources/COPYING" "$output/oniguruma-COPYING.txt"
echo "$lib ($RID) -> $output"
