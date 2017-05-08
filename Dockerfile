FROM microsoft/dotnet:1.1.1-sdk

RUN mkdir -p /dotnetapp

COPY . /dotnetapp
WORKDIR /dotnetapp

EXPOSE 5001

WORKDIR /dotnetapp
RUN dotnet restore --no-cache --configfile HiP-DataStore/NuGet.Config

ENTRYPOINT ["dotnet", "run"]
