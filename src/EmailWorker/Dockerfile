FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./MessageQueueProject.sln ./
COPY ./src/Contracts/ ./src/Contracts/
COPY ./src/EmailWorker/ ./src/EmailWorker/
RUN dotnet restore ./src/EmailWorker/EmailWorker.csproj
RUN dotnet publish ./src/EmailWorker/EmailWorker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EmailWorker.dll"]

