# Build stage (usando .NET 8.0 LTS para estabilidade)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas o .csproj e restaura dependências
COPY *.csproj .
RUN dotnet restore

# Copia o resto e publica
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copia apenas os arquivos publicados
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://*:$PORT

EXPOSE $PORT
ENTRYPOINT ["dotnet", "HelpMyURL.Backend.dll"]