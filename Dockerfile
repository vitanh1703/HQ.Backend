# Stage 1: Build dự án
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file solution và các file project vào để restore dependencies trước
COPY ["HQ.Backend.sln", "./"]
COPY ["HQ.Backend/HQ.Backend.csproj", "HQ.Backend/"]
RUN dotnet restore

# Copy toàn bộ mã nguồn còn lại vào và build
COPY . .
WORKDIR "/src/HQ.Backend"
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Railway tự động cấp port qua biến môi trường PORT, cấu hình .NET lắng nghe port đó
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HQ.Backend.dll"]
