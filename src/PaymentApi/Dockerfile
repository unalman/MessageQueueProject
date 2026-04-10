FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./MessageQueueProject.sln ./
COPY ./src/Contracts/ ./src/Contracts/
COPY ./src/PaymentApi/ ./src/PaymentApi/
RUN dotnet restore ./src/PaymentApi/PaymentApi.csproj
RUN dotnet publish ./src/PaymentApi/PaymentApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaymentApi.dll"]

