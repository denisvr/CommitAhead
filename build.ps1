$ErrorActionPreference = "Stop"

Push-Location backend
try {
    dotnet build --warnaserror
    dotnet test
    dotnet format --verify-no-changes
}
finally {
    Pop-Location
}

Push-Location frontend
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
