FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Kakao/Kakao.csproj Kakao/
RUN dotnet restore Kakao/Kakao.csproj
COPY . .
RUN dotnet publish Kakao/Kakao.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kakao.dll"]
