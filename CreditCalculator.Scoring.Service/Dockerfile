FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CreditCalculator.Scoring.Service/CreditCalculator.Scoring.Service.csproj CreditCalculator.Scoring.Service/
COPY CreditCalculator.Scoring.Messages/CreditCalculator.Scoring.Messages.csproj CreditCalculator.Scoring.Messages/
COPY CreditCalculator.Calculations/CreditCalculator.Calculations.csproj CreditCalculator.Calculations/
RUN dotnet restore CreditCalculator.Scoring.Service/CreditCalculator.Scoring.Service.csproj
COPY . .
WORKDIR /src/CreditCalculator.Scoring.Service
RUN dotnet publish CreditCalculator.Scoring.Service.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CreditCalculator.Scoring.Service.dll"]