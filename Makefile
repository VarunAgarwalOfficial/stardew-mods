.PHONY: check

# The mods need the game's DLLs to build, so check does not build them.
# It checks the manifests, and the C# formatting where dotnet is installed.
check:
	python3 scripts/check.py
	@if command -v dotnet >/dev/null; then \
		for p in */*.csproj; do dotnet format whitespace "$$(dirname $$p)" --folder --verify-no-changes || exit 1; done; \
	else echo "dotnet not installed: skipping the format check"; fi
