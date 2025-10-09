# Script to run tests with code coverage and generate HTML report

Write-Host "Running tests with code coverage..." -ForegroundColor Cyan

# Run tests with coverage
dotnet test RecipeManager.UnitTests --collect:"XPlat Code Coverage" --logger "console;verbosity=minimal"

# Generate HTML report
Write-Host "`nGenerating coverage report..." -ForegroundColor Cyan
reportgenerator `
    "-reports:RecipeManager.UnitTests/TestResults/*/coverage.cobertura.xml" `
    "-targetdir:RecipeManager.UnitTests/TestResults/CoverageReport" `
    "-reporttypes:Html;TextSummary"

# Display summary
Write-Host "`n=== COVERAGE SUMMARY ===" -ForegroundColor Green
Get-Content "RecipeManager.UnitTests/TestResults/CoverageReport/Summary.txt"

# Open HTML report in browser
Write-Host "`nOpening coverage report in browser..." -ForegroundColor Cyan
Start-Process "RecipeManager.UnitTests/TestResults/CoverageReport/index.html"

Write-Host "`nDone! Coverage report opened in your default browser." -ForegroundColor Green
