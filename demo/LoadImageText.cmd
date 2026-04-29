:: Load image with "text typography"
:: Image see in ../LocalArtifacts/Backgrounds
cd ..
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query "text typography" --count 5000 --max-pages 100 --sorting relevance --output "LocalArtifacts/TextImage"
