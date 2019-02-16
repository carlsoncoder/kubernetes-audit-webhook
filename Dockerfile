FROM microsoft/dotnet:2.2-sdk-nanoserver-1803 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-nanoserver-1803
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "kubernetes-audit-webhook.dll"]