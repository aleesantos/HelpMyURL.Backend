# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia o arquivo de projeto e restaura dependências
COPY *.csproj .
RUN dotnet restore

# Copia o resto do código e publica
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Configurações para o Railway
ENV ASPNETCORE_URLS=http://*:$PORT
EXPOSE $PORT

ENTRYPOINT ["dotnet", "HelpMyURL.Backend.dll"]