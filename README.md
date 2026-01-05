# .NET 8 Microservices with Ocelot API Gateway

A complete microservices solution built with .NET 8, featuring an Ocelot API Gateway, multiple microservices using Minimal APIs, Docker containerization, and Docker Compose orchestration.

## 🏗️ Solution Architecture

```
┌─────────────────┐
│  API Gateway    │  (Ocelot - Port 5000)
│   (Gateway)     │
└────────┬────────┘
         │
    ┌────┴────┬──────────┬──────────┐
    │         │          │          │
┌───▼───┐ ┌──▼───┐  ┌───▼───┐  ┌───▼────┐
│ User  │ │Order │  │Product│  │ ...    │
│Service│ │Service│  │Service│  │        │
│ :5001 │ │ :5002│  │ :5003│  │        │
└───────┘ └──────┘  └───────┘  └────────┘
```

## 📁 Project Structure

```
ocelot-multi-route-gateway/
├── OcelotGateway.sln          # Solution file
├── docker-compose.yml         # Docker Compose configuration
├── .dockerignore              # Docker ignore file
├── README.md                  # This file
└── src/
    ├── Gateway/               # Ocelot API Gateway
    │   ├── Gateway.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   ├── ocelot.json        # Ocelot routing configuration
    │   └── Dockerfile
    ├── UserService/           # User Management Microservice
    │   ├── UserService.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   └── Dockerfile
    ├── OrderService/          # Order Management Microservice
    │   ├── OrderService.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   └── Dockerfile
    └── ProductService/        # Product Management Microservice
        ├── ProductService.csproj
        ├── Program.cs
        ├── appsettings.json
        └── Dockerfile
```

## 🚀 Features

### Gateway (Ocelot)
- ✅ Centralized routing to microservices
- ✅ JWT Authentication
- ✅ Request/Response logging with Serilog
- ✅ Circuit breaker and retry policies
- ✅ Health check aggregation
- ✅ Swagger UI for API documentation

### Microservices
- ✅ Minimal APIs (.NET 8)
- ✅ CRUD operations for each service
- ✅ Health check endpoints
- ✅ Swagger/OpenAPI documentation
- ✅ In-memory data stores (ready for database integration)

### Infrastructure
- ✅ Docker containerization for all services
- ✅ Docker Compose for orchestration
- ✅ Service-to-service communication via Docker network
- ✅ Environment-based configuration

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for Windows/Mac) or Docker Engine (for Linux)
- [Docker Compose](https://docs.docker.com/compose/install/) (usually included with Docker Desktop)

## 🛠️ Building the Solution

### Option 1: Using Docker Compose (Recommended)

1. **Clone or navigate to the project directory:**
   ```bash
   cd ocelot-multi-route-gateway
   ```

2. **Build and start all services:**
   ```bash
   docker-compose up --build
   ```

   This will:
   - Build Docker images for all services
   - Start the gateway and all microservices
   - Create a Docker network for service communication

3. **Run in detached mode (background):**
   ```bash
   docker-compose up -d --build
   ```

4. **View logs:**
   ```bash
   docker-compose logs -f
   ```

5. **Stop all services:**
   ```bash
   docker-compose down
   ```

### Option 2: Running Locally (Without Docker)

1. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

2. **Build the solution:**
   ```bash
   dotnet build
   ```

3. **Run services individually:**

   **Terminal 1 - UserService:**
   ```bash
   cd src/UserService
   dotnet run --urls "http://localhost:5001"
   ```

   **Terminal 2 - OrderService:**
   ```bash
   cd src/OrderService
   dotnet run --urls "http://localhost:5002"
   ```

   **Terminal 3 - ProductService:**
   ```bash
   cd src/ProductService
   dotnet run --urls "http://localhost:5003"
   ```

   **Terminal 4 - Gateway:**
   ```bash
   cd src/Gateway
   dotnet run --urls "http://localhost:5000"
   ```

   **Note:** When running locally, update `ocelot.json` to use `localhost` instead of service names:
   ```json
   "DownstreamHostAndPorts": [
     {
       "Host": "localhost",
       "Port": 5001
     }
   ]
   ```

## 🧪 Testing the Solution

### 1. Health Checks

Check if services are running:

```bash
# Gateway health
curl http://localhost:5000/health

# UserService health (through gateway)
curl http://localhost:5000/health/users

# OrderService health (through gateway)
curl http://localhost:5000/health/orders

# ProductService health (through gateway)
curl http://localhost:5000/health/products
```

### 2. Swagger Documentation

Access Swagger UI for each service:

- **Gateway:** http://localhost:5000/swagger
- **UserService:** http://localhost:5001/swagger
- **OrderService:** http://localhost:5002/swagger
- **ProductService:** http://localhost:5003/swagger

### 3. API Testing via Gateway

All requests should go through the gateway at `http://localhost:5000`.

#### User Service Endpoints

**Get all users:**
```bash
curl http://localhost:5000/api/users
```

**Get user by ID:**
```bash
curl http://localhost:5000/api/users/1
```

**Create user:**
```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d "{\"name\":\"Alice Johnson\",\"email\":\"alice@example.com\"}"
```

**Update user:**
```bash
curl -X PUT http://localhost:5000/api/users/1 \
  -H "Content-Type: application/json" \
  -d "{\"id\":1,\"name\":\"John Updated\",\"email\":\"john.updated@example.com\"}"
```

**Delete user:**
```bash
curl -X DELETE http://localhost:5000/api/users/1
```

#### Order Service Endpoints

**Get all orders:**
```bash
curl http://localhost:5000/api/orders
```

**Get order by ID:**
```bash
curl http://localhost:5000/api/orders/1
```

**Get orders by user ID:**
```bash
curl http://localhost:5000/api/orders/user/1
```

**Create order:**
```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d "{\"userId\":1,\"productId\":1,\"quantity\":2,\"totalAmount\":199.98,\"status\":\"Pending\"}"
```

**Update order:**
```bash
curl -X PUT http://localhost:5000/api/orders/1 \
  -H "Content-Type: application/json" \
  -d "{\"id\":1,\"userId\":1,\"productId\":1,\"quantity\":3,\"totalAmount\":299.97,\"status\":\"Completed\"}"
```

**Delete order:**
```bash
curl -X DELETE http://localhost:5000/api/orders/1
```

#### Product Service Endpoints

**Get all products:**
```bash
curl http://localhost:5000/api/products
```

**Get product by ID:**
```bash
curl http://localhost:5000/api/products/1
```

**Create product:**
```bash
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d "{\"name\":\"Wireless Mouse\",\"description\":\"Ergonomic wireless mouse\",\"price\":29.99,\"stock\":150}"
```

**Update product:**
```bash
curl -X PUT http://localhost:5000/api/products/1 \
  -H "Content-Type: application/json" \
  -d "{\"id\":1,\"name\":\"Laptop Pro\",\"description\":\"Updated description\",\"price\":1099.99,\"stock\":30}"
```

**Delete product:**
```bash
curl -X DELETE http://localhost:5000/api/products/1
```

### 4. Using PowerShell (Windows)

**Get all users:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/users" -Method Get
```

**Create user:**
```powershell
$body = @{
    name = "Bob Smith"
    email = "bob@example.com"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/users" -Method Post -Body $body -ContentType "application/json"
```

## 🔐 JWT Authentication

The gateway is configured with JWT authentication. To use authenticated endpoints:

1. **Configure JWT settings** in `src/Gateway/appsettings.json`:
   ```json
   "JwtSettings": {
     "SecretKey": "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!",
     "Issuer": "Gateway",
     "Audience": "Microservices",
     "ExpirationMinutes": 60
   }
   ```

2. **Generate a JWT token** (you'll need to implement a token generation endpoint or use an identity service)

3. **Include the token in requests:**
   ```bash
   curl -H "Authorization: Bearer YOUR_JWT_TOKEN" http://localhost:5000/api/users
   ```

**Note:** Currently, JWT authentication is configured but not enforced on all routes. You can modify `ocelot.json` to require authentication on specific routes.

## ⚙️ Configuration

### Environment Variables

You can override configuration using environment variables in `docker-compose.yml`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_URLS=http://0.0.0.0:5000
```

### Ocelot Configuration

The routing configuration is in `src/Gateway/ocelot.json`. Key features:

- **Routes:** Define upstream and downstream paths
- **QoSOptions:** Circuit breaker and timeout settings
  - `ExceptionsAllowedBeforeBreaking`: Number of exceptions before opening circuit
  - `DurationOfBreak`: How long the circuit stays open (ms)
  - `TimeoutValue`: Request timeout (ms)
- **AuthenticationOptions:** JWT authentication settings

### Service Ports

| Service | Port | Description |
|---------|------|-------------|
| Gateway | 5000 | API Gateway entry point |
| UserService | 5001 | User management service |
| OrderService | 5002 | Order management service |
| ProductService | 5003 | Product management service |

## 📊 Logging

The gateway uses **Serilog** for structured logging:

- **Console output:** Real-time logs in console
- **File logging:** Logs saved to `logs/gateway-YYYYMMDD.txt`
- **Request logging:** HTTP request/response logging enabled

Log levels can be configured in `appsettings.json`:
```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

## 🔄 Circuit Breaker & Retry Policies

Ocelot includes circuit breaker and retry policies configured in `ocelot.json`:

```json
"QoSOptions": {
  "ExceptionsAllowedBeforeBreaking": 3,
  "DurationOfBreak": 1000,
  "TimeoutValue": 5000
}
```

This means:
- After 3 exceptions, the circuit opens
- Circuit stays open for 1000ms
- Requests timeout after 5000ms

## 🐳 Docker Commands

### Useful Docker Compose Commands

```bash
# Start services
docker-compose up

# Start in background
docker-compose up -d

# Rebuild and start
docker-compose up --build

# Stop services
docker-compose down

# Stop and remove volumes
docker-compose down -v

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f gateway

# Restart a specific service
docker-compose restart userservice

# Scale a service (if needed)
docker-compose up --scale userservice=2
```

### Individual Docker Commands

```bash
# Build gateway image
docker build -f src/Gateway/Dockerfile -t gateway:latest .

# Run gateway container
docker run -p 5000:5000 gateway:latest

# List running containers
docker ps

# View container logs
docker logs <container-id>

# Execute command in container
docker exec -it <container-id> /bin/bash
```

## 🧩 Adding a New Microservice

To add a new microservice:

1. **Create the service project:**
   ```bash
   dotnet new web -n NewService -o src/NewService
   ```

2. **Add Minimal API endpoints** in `Program.cs`

3. **Create Dockerfile** similar to existing services

4. **Add to docker-compose.yml:**
   ```yaml
   newservice:
     build:
       context: .
       dockerfile: src/NewService/Dockerfile
     container_name: new-service
     ports:
       - "5004:5004"
     networks:
       - microservices-network
   ```

5. **Add route to `ocelot.json`**

6. **Add project to solution:**
   ```bash
   dotnet sln add src/NewService/NewService.csproj
   ```

## 🐛 Troubleshooting

### Services can't communicate

- Ensure all services are on the same Docker network (`microservices-network`)
- Check service names in `ocelot.json` match container names
- Verify ports are correctly mapped

### Gateway returns 404

- Check `ocelot.json` routing configuration
- Verify downstream services are running
- Check service URLs match the configuration

### Port already in use

- Change ports in `docker-compose.yml` and `ocelot.json`
- Or stop the service using the port:
  ```bash
  # Windows
  netstat -ano | findstr :5000
  taskkill /PID <PID> /F
  
  # Linux/Mac
  lsof -ti:5000 | xargs kill
  ```

### Docker build fails

- Ensure Docker Desktop is running
- Check Dockerfile paths are correct
- Verify .NET SDK is available in Docker image

## 📚 Next Steps

- [ ] Replace in-memory stores with actual databases (SQL Server, PostgreSQL, MongoDB)
- [ ] Implement JWT token generation endpoint
- [ ] Add API versioning
- [ ] Implement service discovery (Consul, Eureka)
- [ ] Add distributed tracing (OpenTelemetry, Application Insights)
- [ ] Implement rate limiting
- [ ] Add request/response transformation
- [ ] Set up CI/CD pipeline
- [ ] Add unit and integration tests
- [ ] Implement caching (Redis)

## 📖 Resources

- [Ocelot Documentation](https://ocelot.readthedocs.io/)
- [.NET 8 Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Serilog Documentation](https://serilog.net/)

## 📝 License

This project is provided as-is for educational and development purposes.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

**Happy Coding! 🚀**
