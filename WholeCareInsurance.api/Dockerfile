FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY WholeCareInsurance.api.csproj .
RUN dotnet restore WholeCareInsurance.api.csproj

COPY . .
RUN dotnet publish WholeCareInsurance.api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WholeCareInsurance.api.dll"]
