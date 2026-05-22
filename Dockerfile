# Stage 1: Build dự án bằng SDK 8.0
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ mã nguồn vào trong container
COPY . .

# MẸO QUAN TRỌNG: Di chuyển vào thư mục chứa file .csproj một cách tự động
# Lệnh này sẽ tìm file .csproj bất kể nó nằm sâu ở tầng nào, rồi nhảy vào thư mục đó.
RUN cd $(dirname $(find . -name "*.csproj" -print -quit)) && \
    dotnet restore && \
    dotnet publish -c Release -o /app/publish

# Stage 2: Chạy ứng dụng bằng Runtime 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cấu hình Port chạy cho Railway
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Chạy file .dll chính (Tên file .dll sinh ra sẽ là HQ.Backend.dll)
ENTRYPOINT ["dotnet", "HQ.Backend.dll"]
