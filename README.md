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
- Servidor [PostgreSQL](https://www.postgresql.org/) en ejecución o [Docker](https://www.docker.com/)

### Levantar PostgreSQL con Docker (Opcional y Recomendado)
Si tienes Docker instalado, puedes iniciar PostgreSQL al instante con la configuración requerida ejecutando en la raíz:
```bash
docker compose up -d
```

### Configuración de `appsettings.json`
El archivo `src/Firmeza.Presentation/appsettings.json` viene listo por defecto:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=firmezadb;Username=postgres;Password=12345"
  },
  "JwtSettings": {
    "SecretKey": "FirmezaSuperSecretKeyParaTokensJWTConMasDe32Caracteres!",
    "Issuer": "FirmezaApi",
    "Audience": "FirmezaClients",
    "DurationInMinutes": 60,
    "RefreshTokenDurationInDays": 7
  }
}
```

---

## 🚀 Creación Automática de Base de Datos y Datos de Prueba (Seeding)

**¡No necesitas ejecutar scripts manuales ni migraciones previas en nuevas máquinas!**

Al ejecutar el proyecto por primera vez en cualquier computadora, la aplicación:
1. Crea automáticamente la base de datos `firmezadb` y todas sus tablas si aún no existen (`context.Database.EnsureCreated()`).
2. Ejecuta el sembrador automático (`DatabaseSeeder`), inyectando roles, usuarios de prueba con contraseñas seguras, categorías, productos de catálogo, clientes y ventas iniciales.

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

3. **Iniciar la Web API (Crea y puebla la base de datos automáticamente si está vacía):**
   ```bash
   dotnet run --project src/Firmeza.Presentation
   ```

4. **Abrir Swagger UI en el navegador:**
   - URL: `https://localhost:7203/swagger` (o `http://localhost:5293/swagger`)

---

## 🔐 Cuentas de Prueba Pre-configuradas

Al iniciar la aplicación ya tienes disponibles los siguientes usuarios por defecto para autenticarte directamente en `POST /api/auth/login`:

### 1. Usuario Administrador (Control Total)
- **Email:** `admin@firmeza.com`
- **Contraseña:** `Admin123*`
- **Rol:** `Admin`
- **Uso:** Creación y modificación de categorías, productos, consulta global de ventas, auditoría y administración de usuarios.

```json
{
  "email": "admin@firmeza.com",
  "password": "Admin123*"
}
```

### 2. Usuario Vendedor / Operativo (Gestión Comercial)
- **Email:** `vendedor@firmeza.com`
- **Contraseña:** `Vendedor123*`
- **Rol:** `Vendedor`
- **Uso:** Gestión de clientes, consulta de inventario, emisión y cancelación de ventas.

```json
{
  "email": "vendedor@firmeza.com",
  "password": "Vendedor123*"
}
```

### 3. Usuario Cliente (Compras y Consultas)
- **Email:** `cliente@firmeza.com`
- **Contraseña:** `Cliente123*`
- **Rol:** `Cliente` (Asociado al cliente *Laura Morales Sánchez*)
- **Uso:** Consulta de catálogo público y seguimiento de pedidos o compras personales.

```json
{
  "email": "cliente@firmeza.com",
  "password": "Cliente123*"
}
```

---

## 🧪 Guía Rápida de Pruebas con Swagger y cURL

### Opción A: Probar desde Swagger UI
1. Inicia sesión desde el endpoint `POST /api/auth/login` enviando las credenciales de `admin@firmeza.com` o `vendedor@firmeza.com`.
2. Copia el valor de la propiedad `token` que retorna la respuesta.
3. Haz clic en el botón verde **`Authorize`** (ubicado en la esquina superior derecha de Swagger).
4. En el campo de texto ingresa el token JWT recibido.
5. Haz clic en **Authorize** y luego **Close**.
6. Ahora todos los endpoints protegidos (`/api/categories`, `/api/products`, `/api/customers`, `/api/sales`) funcionarán con tu token.

### Opción B: Probar directamente con cURL / Terminal
```bash
# 1. Obtener Token JWT con el usuario Admin
TOKEN=$(curl -s -X POST http://localhost:5293/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@firmeza.com","password":"Admin123*"}' | jq -r .token)

# 2. Consultar lista de productos
curl -s -H "Authorization: Bearer $TOKEN" http://localhost:5293/api/products | jq .

# 3. Consultar ventas registradas
curl -s -H "Authorization: Bearer $TOKEN" http://localhost:5293/api/sales | jq .

# 4. Descargar Catálogo de Productos en Excel (.xlsx)
curl -s http://localhost:5293/api/products/export/excel -o Catalogo_Productos.xlsx

# 5. Descargar Reporte Consolidado de Ventas en PDF
curl -s -H "Authorization: Bearer $TOKEN" http://localhost:5293/api/sales/export/pdf -o Reporte_Ventas.pdf

# 6. Descargar Factura de Venta Individual en PDF (reemplaza {id} por el GUID de la venta)
curl -s -H "Authorization: Bearer $TOKEN" http://localhost:5293/api/sales/{id}/export/pdf -o Factura_Venta.pdf
```

---

## 📑 Exportación Profesional de Documentos (Excel & PDF)

El backend incorpora un motor de generación de reportes corporativos de alto estándar:

### 1. Catálogo de Productos en Excel (`ClosedXML`)
- **Endpoint:** `GET /api/products/export/excel` (Parámetro opcional: `?categoryId={guid}`)
- **Características:**
  - Encabezado con paleta corporativa Dark Navy (`#1E293B`) y tipografía limpia.
  - Fórmulas de cálculo de inventario dinámicas (`=Precio * Stock`).
  - Formato contable para valores monetarios (`$ #,##0.00 COP`).
  - Filas alternadas (efecto cebra) y chips visuales de estado (ACTIVO / INACTIVO).
  - Fila de totales consolidados con sumatoria automática (`=SUM(...)`).
  - Panel fijo (freeze panes) en encabezados y ajuste automático de columnas.

### 2. Reporte Ejecutivo de Ventas en PDF (`QuestPDF`)
- **Endpoint:** `GET /api/sales/export/pdf` (`[Authorize]`)
- **Características:**
  - Formato A4 apaisado (*Landscape*) para máxima legibilidad.
  - Tarjetas de indicadores clave (KPIs): Total Facturado, Total Ventas, Ventas Completadas y Pendientes.
  - Tabla de transacciones con número de factura, cliente, NIT/CC, estado y total en COP.
  - Paginación dinámica y sellos de documento confidencial.

### 3. Factura / Comprobante de Venta Oficial en PDF (`QuestPDF`)
- **Endpoint:** `GET /api/sales/{id}/export/pdf` (`[Authorize]`)
- **Características:**
  - Formato A4 vertical con membrete formal de *Firmeza S.A.S.* (NIT, dirección, contacto).
  - Bloque oficial de factura con numeración `FZ-XXXXXXXX`, fecha y estado.
  - Ficha completa del cliente (Razón social, NIT/C.C., dirección de entrega, teléfono y email).
  - Tabla detallada de materiales: descripción, cantidad, precio unitario y subtotal.
  - Cuadro financiero consolidado con discriminación de subtotal e importe total destacado.
  - Notas comerciales, términos legales (Art. 774 C.Co), cuenta bancaria y casillas de firma de entrega y recibo a conformidad.
