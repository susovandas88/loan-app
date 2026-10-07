FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/LoanApp.Application/LoanApp.Application.csproj LoanApp.Application/
COPY src/LoanApp.Infrastructure/LoanApp.Infrastructure.csproj LoanApp.Infrastructure/
COPY src/LoanApp.Api/LoanApp.Api.csproj LoanApp.Api/
RUN dotnet restore LoanApp.Api/LoanApp.Api.csproj
COPY src/LoanApp.Application/ LoanApp.Application/
COPY src/LoanApp.Infrastructure/ LoanApp.Infrastructure/
COPY src/LoanApp.Api/ LoanApp.Api/
RUN dotnet publish LoanApp.Api/LoanApp.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/data /app/blobs
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LoanApp.Api.dll"]
