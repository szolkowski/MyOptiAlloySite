FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

#Restore NuGet packages so they are cached when we start the container
COPY ./MyOptiAlloySite.csproj .
COPY ./Directory.Build.props .
COPY ./nuget.config .

RUN dotnet restore

EXPOSE 80
EXPOSE 443
EXPOSE 5100
EXPOSE 5101

ENTRYPOINT dotnet run --no-launch-profile
