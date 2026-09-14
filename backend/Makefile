# SAM custom build for the Lambda function (Metadata.BuildMethod: makefile in
# template.yaml). SAM invokes `make build-<FunctionLogicalId>` with the current
# directory set to CodeUri (backend/) and $(ARTIFACTS_DIR) pointing at the
# folder the published output must land in.
#
# Why this exists: backend/ contains a solution file (SongAppApi.sln) next to
# the test project. SAM's managed dotnet builder would run `dotnet publish` on
# the folder, pick up the solution, and try to publish every project in it,
# including SongAppApi.Tests (which is not publishable and fails the build).
# Targeting SongAppApi.csproj explicitly builds only the API.
#
# The publish flags mirror what SAM's managed dotnet8 builder produces: a
# framework-dependent deployment for the linux-x64 (x86_64) managed runtime.
build-MusicApiFunction:
	dotnet publish SongAppApi.csproj \
		--configuration Release \
		--runtime linux-x64 \
		--self-contained false \
		--output "$(ARTIFACTS_DIR)"
