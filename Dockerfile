# Stage 1: Build dự án
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ file vào Docker
COPY . .

# Thực hiện restore và publish từ thư mục gốc
RUN dotnet restore "HQ.Backend/HQ.Backend/HQ.Backend.csproj"
RUN dotnet publish "HQ.Backend/HQ.Backend/HQ.Backend.csproj" -c Release -o /app/publish

# Stage 2: Chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cấu hình Port chạy cho Railway
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Chỉ định đích danh file dll chạy trực tiếp (Không dùng lệnh shell ls/cut nữa)
ENTRYPOINT ["dotnet", "HQ.Backend.dll"]
