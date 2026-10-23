FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY LabReservations.sln ./
COPY src/LabReservations.Cli/*.csproj src/LabReservations.Cli/
COPY tests/LabReservations.Tests/*.csproj tests/LabReservations.Tests/
RUN dotnet restore

COPY . ./
RUN dotnet build -c Release --no-restore

ENTRYPOINT ["dotnet", "run", "--project", "src/LabReservations.Cli", "--no-build", "--"]