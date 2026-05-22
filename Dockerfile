# Stage 1: Build dự án bằng SDK 9.0
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy toàn bộ mã nguồn vào trong container
COPY . .

# Di chuyển tự động vào thư mục chứa file .csproj và build
RUN cd $(dirname $(find . -name "*.csproj" -print -quit)) && \
    dotnet restore && \
    dotnet publish -c Release -o /app/publish

# Stage 2: Chạy ứng dụng bằng Runtime 9.0 (Sử dụng bản Alpine để đảm bảo đầy đủ lệnh hệ thống shell)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cấu hình Port chạy cho Railway
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Chạy trực tiếp file dll chính
ENTRYPOINT ["dotnet", "HQ.Backend.dll"]
