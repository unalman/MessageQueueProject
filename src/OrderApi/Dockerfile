FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./MessageQueueProject.sln ./
COPY ./src/Contracts/ ./src/Contracts/
COPY ./src/OrderApi/ ./src/OrderApi/
RUN dotnet restore ./src/OrderApi/OrderApi.csproj
RUN dotnet publish ./src/OrderApi/OrderApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderApi.dll"]

