# Stage 1: Build dự án
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ file trong repository vào Docker
COPY . .

# Sử dụng đường dẫn trực tiếp từ thư mục gốc để restore và publish 
# (Hệ thống sẽ tự quét chuẩn xác theo đúng cấu trúc hai tầng thư mục của bạn)
RUN dotnet restore "HQ.Backend/HQ.Backend/HQ.Backend.csproj"
RUN dotnet publish "HQ.Backend/HQ.Backend/HQ.Backend.csproj" -c Release -o /app/publish

# Stage 2: Chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cấu hình Port chạy cho Railway
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Tự động quét file dll chính để chạy
ENTRYPOINT ["sh", "-c", "dotnet $(ls *.runtimeconfig.json | cut -d'.' -f1-2).dll"]
