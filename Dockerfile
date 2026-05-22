# Stage 1: Build ứng dụng bằng SDK 8.0
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ mã nguồn vào container
COPY . .

# Đi vào thư mục con chứa file .csproj để restore và publish
WORKDIR "/src/HQ.Backend/HQ.Backend"
RUN dotnet restore "HQ.Backend.csproj"
RUN dotnet publish "HQ.Backend.csproj" -c Release -o /app/publish

# Stage 2: Chạy ứng dụng bằng Runtime 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cấu hình cổng lắng nghe theo biến môi trường PORT của Railway
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Mẹo nhỏ: Dùng lệnh shell để tự động tìm file .dll chính trong thư mục và chạy, tránh lỗi viết Hoa/thường
ENTRYPOINT ["sh", "-c", "dotnet $(ls *.runtimeconfig.json | cut -d'.' -f1-2).dll"]
