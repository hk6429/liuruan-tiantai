#!/bin/sh
set -eu
pwd
scripts_dir=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: 此 runner 需要 .NET SDK；本次交件使用本機 dotnet，未宣稱支援其他編譯器。" >&2
    exit 1
fi
export LIURUAN_PROJECT_ROOT=$(CDPATH= cd -- "$scripts_dir/../.." && pwd)
# R2 的結局測試直接讀取 C 線 JSON／原文，避免複製過期 fixture。
work_dir=$(mktemp -d "${TMPDIR:-/tmp}/liuruan-core.XXXXXX")
trap '/bin/rm -r "$work_dir"' EXIT HUP INT TERM
export DOTNET_CLI_HOME="$work_dir/cli"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1
sdk_version=$(dotnet --version)
sdk_major=${sdk_version%%.*}
case "$sdk_major" in
    ''|*[!0-9]*) echo "ERROR: 無法辨識 SDK: $sdk_version" >&2; exit 1 ;;
esac
if [ "$sdk_major" -lt 8 ]; then
    echo "ERROR: 需要 .NET SDK 8 以上（C# 限定為 9）。" >&2
    exit 1
fi
cp "$scripts_dir"/Runtime/*Core.cs "$scripts_dir"/Tests/EditMode/*Tests.cs \
    "$scripts_dir/Tests/Console/Runner.cs" "$work_dir/"
cat > "$work_dir/NuGet.Config" <<'XML'
<configuration><packageSources><clear /></packageSources></configuration>
XML
cat > "$work_dir/CoreTests.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net$sdk_major.0</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
</Project>
XML
printf 'Compiler: .NET SDK %s; C# 9.0; no NuGet packages; shared EditMode test methods\n' "$sdk_version"
dotnet run --project "$work_dir/CoreTests.csproj" --configuration Release --no-launch-profile --disable-build-servers
