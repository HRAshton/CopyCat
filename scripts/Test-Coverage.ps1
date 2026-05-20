if (Test-Path "coverage")
{
    Remove-Item "coverage" -Recurse -Force
}

dotnet test --collect:"XPlat Code Coverage" --results-directory "coverage"

dotnet tool run reportgenerator "-reports:coverage\**\coverage.cobertura.xml" "-targetdir:coverage\report" -reporttypes:Html
