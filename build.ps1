$ErrorActionPreference = "Stop"

dotnet build --warnaserror
dotnet test
dotnet format --verify-no-changes

Push-Location src/CommitAhead.Web
try {
    npm ci
    npm run lint
    npx tsc -b
    npm test
    npm run build
}
finally {
    Pop-Location
}
