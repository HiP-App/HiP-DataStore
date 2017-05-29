FROM microsoft/dotnet:1.1.1-sdk

RUN apt-get update && apt-get install mongodb -y

RUN mkdir -p /data/db

#RUN wget http://download.geteventstore.com/binaries/EventStore-OSS-Ubuntu-14.04-v4.0.0.tar.gz && tar xzf EventStore-OSS-Ubuntu-14.04-v4.0.0.tar.gz

RUN mkdir -p /dotnetapp

COPY . /dotnetapp
WORKDIR /dotnetapp/HiP-DataStore

EXPOSE 5000

RUN dotnet restore --no-cache --configfile NuGet.Config

CMD /dotnetapp/HiP-DataStore/run.sh
