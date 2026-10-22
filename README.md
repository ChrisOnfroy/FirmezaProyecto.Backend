# 🏛️ Firmeza - Backend API

Backend robusto desarrollado en **.NET 10** bajo los principios de **Clean Architecture** y **Domain-Driven Design (DDD)** básico, con persistencia en **PostgreSQL**, autenticación y autorización mediante **ASP.NET Core Identity + JWT**, y documentación interactiva con **Swagger UI**.

---

## 📐 Arquitectura del Proyecto

El sistema está dividido en cuatro capas desacopladas que respetan el principio de inversión de dependencias:

```text
Firmeza.Backend/
├── Firmeza.Backend.slnx
├── README.md
├── src/
│   ├── Firmeza.Domain/              # Núcleo del Negocio (Entidades, Value Objects, Enums, Excepciones)
│   ├── Firmeza.Application/         # Casos de Uso, DTOs e Interfaces de Repositorios/Servicios
│   ├── Firmeza.Infrastructure/      # EF Core, PostgreSQL, ASP.NET Core Identity, JWT, Repositorios y SMTP
│   └── Firmeza.Presentation/        # ASP.NET Core Web API, Controladores REST y Swagger UI
└── tests/
    └── Firmeza.UnitTests/           # Suite de pruebas unitarias xUnit, Moq y FluentAssertions
```

### 1. `Firmeza.Domain` (Capa de Dominio)
No tiene dependencias de frameworks ni librerías externas.
- **Entidades**: `Customer`, `Product`, `Category`, `Sale`, `SaleDetail` (heredando de `BaseEntity` con `Guid` y auditoría de fechas).
- **Value Objects**: `Money` (cálculos monetarios y monedas), `Email` (validación de formato y normalización), `Address` (direcciones completas), `IdentificationDocument` (documento con tipo y número), `PhoneNumber` (teléfono internacional).
- **Enums**: `IdentificationType`, `SaleStatus`.
- **Excepciones de Negocio**: `DomainException`, `InsufficientStockException`.

### 2. `Firmeza.Application` (Capa de Aplicación)
Orquesta la lógica de los casos de uso sin depender de tecnologías de persistencia ni web.
- **Contratos de Persistencia**: `IBaseRepository<T>`, `ICustomerRepository`, `IProductRepository`, `ICategoryRepository`, `ISaleRepository`, `IUnitOfWork`.
- **Contratos de Seguridad**: `IAuthService`, `ICurrentUserService`.
- **DTOs**: Modelos planos de entrada y salida para no exponer las entidades de dominio.
- **Servicios**: `CategoryService`, `ProductService`, `CustomerService`, `SaleService`.

### 3. `Firmeza.Infrastructure` (Capa de Infraestructura)
Implementa la tecnología y el acceso a datos.
- **Base de Datos**: `ApplicationDbContext` configurado con `Npgsql.EntityFrameworkCore.PostgreSQL`.
- **Mapeos Fluent API**: Mapeo transparente de Value Objects a columnas de PostgreSQL.
- **Seguridad**:
  - ASP.NET Core Identity (`ApplicationUser`, `ApplicationRole`).
  - Generación criptográfica de tokens JWT con claims de usuario y roles (`AuthService`).
- **Implementación de Repositorios**: Consultas optimizadas con EF Core y `UnitOfWork`.

### 4. `Firmeza.Presentation` (Capa de Presentación)
Punto de entrada de la aplicación mediante API RESTful.
- **Controladores**:
  - `AuthController`: Endpoints públicos de login y registro.
  - `CategoriesController`: CRUD de categorías (`[Authorize]`).
  - `ProductsController`: Catálogo y stock (`[Authorize]`).
  - `CustomersController`: Gestión de clientes (`[Authorize]`).
  - `SalesController`: Registro y cancelación de ventas con actualización de stock (`[Authorize]`).
- **Swagger UI**: Interfaz visual interactiva en `/swagger` con soporte integrado para autorización con Bearer JWT.

---

## 🛠️ Tecnologías Utilizadas

- **.NET 10** (`net10.0`)
- **Entity Framework Core 10**
- **Npgsql PostgreSQL Provider**
- **ASP.NET Core Identity**
- **JWT (JSON Web Tokens)** con `System.IdentityModel.Tokens.Jwt`
- **Swashbuckle Swagger UI**

---

## ⚙️ Configuración y Requisitos

### Prerrequisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Servidor [PostgreSQL](https://www.postgresql.org/) en ejecución

### Configuración de `appsettings.json`
Edita el archivo `src/Firmeza.Presentation/appsettings.json` con tus credenciales:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=firmezadb;Username=postgres;Password=12345"
  },
  "JwtSettings": {
    "SecretKey": "TuClaveSecretaSuperSeguraDeMasDe32Caracteres!",
    "Issuer": "FirmezaApi",
    "Audience": "FirmezaClients",
    "DurationInMinutes": 60,
    "RefreshTokenDurationInDays": 7
  }
}
```

---

## 🚀 Migraciones y Creación de Base de Datos

Si tienes instalado `dotnet-ef`, ejecuta estos comandos en la raíz del proyecto para generar y aplicar la base de datos en PostgreSQL:

```bash
# 1. Crear la migración inicial
dotnet ef migrations add InitialCreate --project src/Firmeza.Infrastructure --startup-project src/Firmeza.Presentation

# 2. Aplicar la migración a la base de datos
dotnet ef database update --project src/Firmeza.Infrastructure --startup-project src/Firmeza.Presentation
```

---

## ▶️ Ejecución del Proyecto y Pruebas Unitarias

1. **Compilar la solución:**
   ```bash
   dotnet build Firmeza.Backend.slnx
   ```

2. **Ejecutar la suite de pruebas unitarias (xUnit):**
   ```bash
   dotnet test Firmeza.Backend.slnx
   ```
   *Ejecuta 31 pruebas unitarias sobre entidades de dominio, value objects (Money, Email), stock de productos y el servicio transaccional de ventas con despacho de comprobantes SMTP.*

3. **Iniciar la Web API:**
   ```bash
   dotnet run --project src/Firmeza.Presentation
   ```

4. **Abrir Swagger UI en el navegador:**
   - URL: `https://localhost:7203/swagger` (o `http://localhost:5293/swagger`)

---

## 🔐 Guía de Uso con Swagger y JWT

1. **Registrar un usuario:**
   - Dirígete al endpoint `POST /api/auth/register`.
   - Envía los datos requeridos (email, password, nombre, apellido, rol).
   - Recibirás una respuesta con el token JWT.

2. **Iniciar sesión:**
   - Puedes usar `POST /api/auth/login` con tus credenciales para obtener un nuevo token.

3. **Autenticar Swagger:**
   - Haz clic en el botón verde **`Authorize`** (ubicado en la esquina superior derecha de Swagger).
   - En el campo de texto ingresa el token JWT recibido.
   - Haz clic en **Authorize** y luego **Close**.

4. **Probar los Endpoints:**
   - Ahora puedes crear categorías, productos, clientes y registrar ventas protegidas por el token JWT.
