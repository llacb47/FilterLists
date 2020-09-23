# init base for Visual Studio debugging
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine3.12 AS base
WORKDIR /app
EXPOSE 80

# init build
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine3.12 AS build
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

# restore API
WORKDIR /app
COPY SharedKernel/FilterLists.SharedKernel.Apis.Contracts/FilterLists.SharedKernel.Apis.Contracts.csproj SharedKernel/FilterLists.SharedKernel.Apis.Contracts/
COPY SharedKernel/FilterLists.SharedKernel.Apis.Clients/FilterLists.SharedKernel.Apis.Clients.csproj SharedKernel/FilterLists.SharedKernel.Apis.Clients/
COPY SharedKernel/FilterLists.SharedKernel.Logging/FilterLists.SharedKernel.Logging.csproj SharedKernel/FilterLists.SharedKernel.Logging/
COPY SharedKernel/FilterLists.SharedKernel.SeedWork/FilterLists.SharedKernel.SeedWork.csproj SharedKernel/FilterLists.SharedKernel.SeedWork/
COPY Archival/FilterLists.Archival.Infrastructure/FilterLists.Archival.Infrastructure.csproj Archival/FilterLists.Archival.Infrastructure/
COPY Archival/FilterLists.Archival.Application/FilterLists.Archival.Application.csproj Archival/FilterLists.Archival.Application/
WORKDIR /app/Archival/FilterLists.Archival.Api
COPY Archival/FilterLists.Archival.Api/FilterLists.Archival.Api.csproj .
RUN dotnet restore

# build API
WORKDIR /app
COPY /.editorconfig .
COPY SharedKernel/FilterLists.SharedKernel.Apis.Contracts/. SharedKernel/FilterLists.SharedKernel.Apis.Contracts/
COPY SharedKernel/FilterLists.SharedKernel.Apis.Clients/. SharedKernel/FilterLists.SharedKernel.Apis.Clients/
COPY SharedKernel/FilterLists.SharedKernel.Logging/. SharedKernel/FilterLists.SharedKernel.Logging/
COPY SharedKernel/FilterLists.SharedKernel.SeedWork/. SharedKernel/FilterLists.SharedKernel.SeedWork/
COPY Archival/FilterLists.Archival.Infrastructure/. Archival/FilterLists.Archival.Infrastructure/
COPY Archival/FilterLists.Archival.Application/. Archival/FilterLists.Archival.Application/
WORKDIR /app/Archival/FilterLists.Archival.Api
COPY Archival/FilterLists.Archival.Api/. .
RUN dotnet publish --no-restore -o /app/publish -r alpine.3.12-x64

# package final
FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ./FilterLists.Archival.Api