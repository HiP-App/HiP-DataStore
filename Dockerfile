FROM microsoft/dotnet:1.1.1-sdk

RUN apt-get update && apt-get install make gcc -y

RUN wget https://www.openssl.org/source/openssl-1.1.0f.tar.gz && tar xzf openssl-1.1.0f.tar.gz && cd openssl-1.1.0f && ./config && make && make install

RUN wget https://fastdl.mongodb.org/linux/mongodb-linux-x86_64-debian81-3.4.4.tgz && tar xzf mongodb-linux-x86_64-debian81-3.4.4.tgz

RUN mkdir -p /data/db

RUN mkdir -p /dotnetapp

COPY . /dotnetapp
WORKDIR /dotnetapp/HiP-DataStore

EXPOSE 5000

RUN dotnet restore --no-cache --configfile ../NuGet.Config

CMD /dotnetapp/HiP-DataStore/run.sh
