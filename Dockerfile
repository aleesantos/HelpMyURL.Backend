# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restaura dependências primeiro (otimização de cache)
COPY *.csproj .
RUN dotnet restore

# Copia o resto e publica
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Configurações para o Railway
ENV ASPNETCORE_URLS=http://*:$PORT
EXPOSE $PORT

# Copia apenas os arquivos publicados
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HelpMyURL.Backend.dll"]