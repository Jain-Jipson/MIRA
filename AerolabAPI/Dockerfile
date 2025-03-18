FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
COPY . .
EXPOSE 5054
CMD ["dotnet", "run", "--urls", "http://0.0.0.0:5054"]
