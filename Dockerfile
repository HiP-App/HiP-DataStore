FROM microsoft/dotnet:1.1.1-sdk

RUN apt-get update && apt-get install mongodb -y

RUN mkdir -p /data/db

RUN mkdir -p /dotnetapp

COPY . /dotnetapp
WORKDIR /dotnetapp/HiP-DataStore

EXPOSE 58737

RUN dotnet restore --no-cache --configfile NuGet.Config

CMD /dotnetapp/HiP-DataStore/run.sh
