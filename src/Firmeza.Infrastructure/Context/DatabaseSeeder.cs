using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Firmeza.Domain.ValueObjects;
using Firmeza.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Firmeza.Infrastructure.Context;

/// <summary>
/// Sembrador automático de datos de prueba para entornos de desarrollo y testing.
/// 
/// Cuentas de usuario predeterminadas disponibles al iniciar:
/// 1. Administrador:
///    - Email: admin@firmeza.com
///    - Password: Admin123*
///    - Rol: Admin (Acceso y gestión total del sistema)
/// 
/// 2. Vendedor / Comercial:
///    - Email: vendedor@firmeza.com
///    - Password: Vendedor123*
///    - Rol: Vendedor (Gestión de ventas, cotizaciones y catálogo)
/// 
/// 3. Cliente:
///    - Email: cliente@firmeza.com
///    - Password: Cliente123*
///    - Rol: Cliente (Cliente registrado para pedidos)
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(DatabaseSeeder));

        try
        {
            // 1. Roles del Sistema
            await SeedRolesAsync(roleManager, logger);

            // 2. Usuarios de Prueba
            await SeedUsersAsync(userManager, logger);

            // 3. Clientes (Customers)
            var seededCustomers = await SeedCustomersAsync(context, logger);

            // 4. Categorías y Productos de Materiales de Construcción
            var seededProducts = await SeedCatalogAsync(context, logger);

            // 5. Ventas de Prueba con Detalles
            await SeedSalesAsync(context, seededCustomers, seededProducts, logger);

            logger?.LogInformation("Sembrado de datos para pruebas completado exitosamente.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ocurrió un error durante el sembrado de datos de prueba.");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger? logger)
    {
        var roles = new[]
        {
            ("Admin", "Administrador con control total del sistema"),
            ("Vendedor", "Gestión de catálogo, clientes y registro de ventas"),
            ("Cliente", "Cliente comprador registrado en la plataforma")
        };

        foreach (var (roleName, description) in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole(roleName, description);
                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    logger?.LogInformation("Rol creado: {RoleName}", roleName);
                }
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, ILogger? logger)
    {
        var usersToSeed = new[]
        {
            new
            {
                Email = "admin@firmeza.com",
                FirstName = "Administrador",
                LastName = "Principal",
                PhoneNumber = "3001234567",
                Password = "Admin123*",
                Role = "Admin"
            },
            new
            {
                Email = "vendedor@firmeza.com",
                FirstName = "Carlos",
                LastName = "Mendoza",
                PhoneNumber = "3109876543",
                Password = "Vendedor123*",
                Role = "Vendedor"
            },
            new
            {
                Email = "cliente@firmeza.com",
                FirstName = "Laura",
                LastName = "Morales",
                PhoneNumber = "3154567890",
                Password = "Cliente123*",
                Role = "Cliente"
            }
        };

        foreach (var item in usersToSeed)
        {
            var existingUser = await userManager.FindByEmailAsync(item.Email);
            if (existingUser is null)
            {
                var user = new ApplicationUser
                {
                    UserName = item.Email,
                    Email = item.Email,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    PhoneNumber = item.PhoneNumber,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(user, item.Password);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, item.Role);
                    logger?.LogInformation("Usuario de prueba creado: {Email} [{Role}]", item.Email, item.Role);
                }
                else
                {
                    logger?.LogWarning("No se pudo crear el usuario {Email}: {Errors}",
                        item.Email,
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private static async Task<Dictionary<string, Customer>> SeedCustomersAsync(ApplicationDbContext context, ILogger? logger)
    {
        var customersDict = new Dictionary<string, Customer>();

        var existingCustomers = await context.Customers.ToListAsync();
        foreach (var c in existingCustomers)
        {
            customersDict[c.Email.Value] = c;
        }

        if (existingCustomers.Count > 0)
        {
            return customersDict;
        }

        var customer1 = new Customer(
            firstName: "Laura",
            lastName: "Morales Sánchez",
            identification: new IdentificationDocument(IdentificationType.NationalIdentity, "1020304050"),
            email: new Email("cliente@firmeza.com"),
            phoneNumber: new PhoneNumber("+57", "3154567890"),
            address: new Address("Carrera 15 # 85-32 Apto 402", "Bogotá", "Cundinamarca", "110221", "Colombia"));

        var customer2 = new Customer(
            firstName: "Constructora El Roble",
            lastName: "S.A.S.",
            identification: new IdentificationDocument(IdentificationType.TaxId, "900765432-1"),
            email: new Email("compras@constructoraelroble.com"),
            phoneNumber: new PhoneNumber("+57", "3201112233"),
            address: new Address("Avenida El Dorado # 68C-61", "Bogotá", "Cundinamarca", "111071", "Colombia"));

        var customer3 = new Customer(
            firstName: "Juan Carlos",
            lastName: "Pérez Gómez",
            identification: new IdentificationDocument(IdentificationType.NationalIdentity, "79845123"),
            email: new Email("juan.perez@constructorescol.com"),
            phoneNumber: new PhoneNumber("+57", "3118889900"),
            address: new Address("Calle 10 # 43E-20", "Medellín", "Antioquia", "050021", "Colombia"));

        var customer4 = new Customer(
            firstName: "Obras y Diseños",
            lastName: "del Valle",
            identification: new IdentificationDocument(IdentificationType.TaxId, "805123987-4"),
            email: new Email("proyectos@obrasdelvalle.com"),
            phoneNumber: new PhoneNumber("+57", "3187776655"),
            address: new Address("Avenida 6N # 28N-45", "Cali", "Valle del Cauca", "760001", "Colombia"));

        var newCustomers = new[] { customer1, customer2, customer3, customer4 };
        await context.Customers.AddRangeAsync(newCustomers);
        await context.SaveChangesAsync();

        foreach (var c in newCustomers)
        {
            customersDict[c.Email.Value] = c;
            logger?.LogInformation("Cliente registrado: {Name} ({Email})", $"{c.FirstName} {c.LastName}", c.Email.Value);
        }

        return customersDict;
    }

    private static async Task<Dictionary<string, Product>> SeedCatalogAsync(ApplicationDbContext context, ILogger? logger)
    {
        var productsDict = new Dictionary<string, Product>();

        if (await context.Categories.AnyAsync())
        {
            var existingProds = await context.Products.ToListAsync();
            foreach (var p in existingProds)
            {
                productsDict[p.Name] = p;
            }
            return productsDict;
        }

        // Definición de Categorías
        var catMamposteria = new Category
        {
            Name = "Cemento y Mampostería",
            Description = "Cementos hidráulicos, arenas lavadas, gravas, cal y bloques de concreto."
        };

        var catAcero = new Category
        {
            Name = "Acero y Estructuras",
            Description = "Varillas corrugadas sismorresistentes, perfiles estructurales y mallas electrosoldadas."
        };

        var catHerramientas = new Category
        {
            Name = "Herramientas y Maquinaria",
            Description = "Maquinaria de obra, mezcladoras, vibradores, taladros y herramientas de mano."
        };

        var catPinturas = new Category
        {
            Name = "Acabados y Pinturas",
            Description = "Pinturas arquitectónicas lavables, estucos plásticos, selladores e impermeabilizantes."
        };

        var catTuberia = new Category
        {
            Name = "Tuberías y Fontanería",
            Description = "Tuberías sanitarias, de ventilación y presión PVC con accesorios para redes hidrosanitarias."
        };

        var categories = new[] { catMamposteria, catAcero, catHerramientas, catPinturas, catTuberia };
        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        // Definición de Productos
        var products = new List<Product>
        {
            // Cemento y Mampostería
            new(
                name: "Cemento Gris Uso General Argos 50 Kg",
                price: new Money(34500m, "COP"),
                stock: 250,
                categoryId: catMamposteria.Id,
                description: "Cemento Portland de alta resistencia y fraguado uniforme para fundiciones y mampostería."
            ),
            new(
                name: "Cemento Blanco Estructural 40 Kg",
                price: new Money(48000m, "COP"),
                stock: 80,
                categoryId: catMamposteria.Id,
                description: "Cemento blanco de alta pureza ideal para concretos arquitectónicos y prefabricados."
            ),
            new(
                name: "Arena de Peña Lavada (Bulto 40 Kg)",
                price: new Money(8500m, "COP"),
                stock: 300,
                categoryId: catMamposteria.Id,
                description: "Arena seleccionada y lavada libre de impurezas para mezclas finas de mortero y concreto."
            ),
            new(
                name: "Bloque de Cemento Estructural 15x20x40 cm",
                price: new Money(3200m, "COP"),
                stock: 600,
                categoryId: catMamposteria.Id,
                description: "Bloque de concreto de 3 cavidades de alta resistencia para muros de carga y cerramientos."
            ),

            // Acero y Estructuras
            new(
                name: "Varilla Corrugada 1/2 pulgada x 6 m (W60)",
                price: new Money(38900m, "COP"),
                stock: 180,
                categoryId: catAcero.Id,
                description: "Acero de refuerzo sismorresistente grado 60 conforme a la norma técnica colombiana NSR-10."
            ),
            new(
                name: "Varilla Corrugada 3/8 pulgada x 6 m (W60)",
                price: new Money(24500m, "COP"),
                stock: 220,
                categoryId: catAcero.Id,
                description: "Acero corrugado para estribos y armaduras secundarias en columnas y vigas de confinamiento."
            ),
            new(
                name: "Malla Electrosoldada M-188 (2.35 x 6 m)",
                price: new Money(115000m, "COP"),
                stock: 75,
                categoryId: catAcero.Id,
                description: "Malla de alambre trefilado corrugado de alta resistencia para losas de contrapiso y entrepisos."
            ),
            new(
                name: "Alambre Negro Recocido Calibre 18 (Rollo 50 Kg)",
                price: new Money(195000m, "COP"),
                stock: 40,
                categoryId: catAcero.Id,
                description: "Alambre dúctil y maleable para amarre de acero de refuerzo en obra."
            ),

            // Herramientas y Maquinaria
            new(
                name: "Mezcladora de Concreto 1.5 Bultos Motor Eléctrico 3HP",
                price: new Money(2850000m, "COP"),
                stock: 8,
                categoryId: catHerramientas.Id,
                description: "Trompo mezclador con tambor de lámina de alto calibre y transmisión por corona dentada reforzada."
            ),
            new(
                name: "Vibrador de Concreto Gasolina 5.5 HP con Guaya 4 m",
                price: new Money(1420000m, "COP"),
                stock: 12,
                categoryId: catHerramientas.Id,
                description: "Equipo compacto para compactación de mezclas de concreto y eliminación de aire atrapado."
            ),
            new(
                name: "Taladro Percutor DeWalt 1/2 pulgada 800W DWD024",
                price: new Money(430000m, "COP"),
                stock: 25,
                categoryId: catHerramientas.Id,
                description: "Taladro percutor de velocidad variable y reversa con mandril industrial para concreto y metal."
            ),
            new(
                name: "Carretilla Buggy Tolva Metálica 6 Pies Cúbicos",
                price: new Money(210000m, "COP"),
                stock: 35,
                categoryId: catHerramientas.Id,
                description: "Carretilla de alta resistencia para transporte de agregados y concreto con llanta antipinchazo."
            ),

            // Acabados y Pinturas
            new(
                name: "Pintura Viniltex Tipo 1 Blanco Balde 5 Galones",
                price: new Money(168000m, "COP"),
                stock: 45,
                categoryId: catPinturas.Id,
                description: "Pintura base agua de alta lavabilidad y excelente poder cubriente para interiores y exteriores cubiertos."
            ),
            new(
                name: "Estuco Plástico Listo Corona Balde 25 Kg",
                price: new Money(52000m, "COP"),
                stock: 90,
                categoryId: catPinturas.Id,
                description: "Masilla acrílica de excelente blancura y acabado liso de secado rápido, lista para aplicar."
            ),
            new(
                name: "Impermeabilizante Sika 1 Galón 4 Kg",
                price: new Money(42500m, "COP"),
                stock: 60,
                categoryId: catPinturas.Id,
                description: "Aditivo líquido hidrófugo para morteros y pañetes impermeables en muros y fachadas."
            ),

            // Tuberías y Fontanería
            new(
                name: "Tubo PVC Sanitario 4 pulgadas x 6 m Pavco Wavin",
                price: new Money(74000m, "COP"),
                stock: 70,
                categoryId: catTuberia.Id,
                description: "Tubería sanitaria para aguas residuales y ventilación con unión mecánica o soldada."
            ),
            new(
                name: "Tubo PVC Presión RDE 21 1/2 pulgada x 6 m",
                price: new Money(18500m, "COP"),
                stock: 110,
                categoryId: catTuberia.Id,
                description: "Tubo de PVC para distribución y transporte de agua potable a presión."
            )
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        foreach (var p in products)
        {
            productsDict[p.Name] = p;
            logger?.LogInformation("Producto creado: {Name} [{Price}]", p.Name, p.Price);
        }

        return productsDict;
    }

    private static async Task SeedSalesAsync(
        ApplicationDbContext context,
        Dictionary<string, Customer> customers,
        Dictionary<string, Product> products,
        ILogger? logger)
    {
        if (await context.Sales.AnyAsync())
        {
            return;
        }

        if (!customers.TryGetValue("compras@constructoraelroble.com", out var customerRoble) ||
            !customers.TryGetValue("juan.perez@constructorescol.com", out var customerPerez) ||
            !customers.TryGetValue("cliente@firmeza.com", out var customerLaura))
        {
            logger?.LogWarning("No se encontraron clientes requeridos para generar ventas de prueba.");
            return;
        }

        // Venta 1: Completada (Constructora El Roble) - Gran compra de materiales
        if (products.TryGetValue("Cemento Gris Uso General Argos 50 Kg", out var cemento) &&
            products.TryGetValue("Varilla Corrugada 1/2 pulgada x 6 m (W60)", out var varilla) &&
            products.TryGetValue("Malla Electrosoldada M-188 (2.35 x 6 m)", out var malla))
        {
            var sale1 = new Sale(customerRoble.Id, "COP")
            {
                SaleDate = DateTime.UtcNow.AddDays(-3)
            };

            sale1.AddDetail(cemento, 50);
            sale1.AddDetail(varilla, 30);
            sale1.AddDetail(malla, 10);
            sale1.Complete();

            context.Sales.Add(sale1);
            logger?.LogInformation("Venta de prueba #1 (Completada) generada para {Customer} por {Total}", customerRoble.FirstName, sale1.TotalAmount);
        }

        // Venta 2: Completada (Juan Carlos Pérez) - Compra de maquinaria y herramientas
        if (products.TryGetValue("Mezcladora de Concreto 1.5 Bultos Motor Eléctrico 3HP", out var mezcladora) &&
            products.TryGetValue("Carretilla Buggy Tolva Metálica 6 Pies Cúbicos", out var carretilla) &&
            products.TryGetValue("Taladro Percutor DeWalt 1/2 pulgada 800W DWD024", out var taladro))
        {
            var sale2 = new Sale(customerPerez.Id, "COP")
            {
                SaleDate = DateTime.UtcNow.AddDays(-1)
            };

            sale2.AddDetail(mezcladora, 1);
            sale2.AddDetail(carretilla, 2);
            sale2.AddDetail(taladro, 1);
            sale2.Complete();

            context.Sales.Add(sale2);
            logger?.LogInformation("Venta de prueba #2 (Completada) generada para {Customer} por {Total}", customerPerez.FirstName, sale2.TotalAmount);
        }

        // Venta 3: Pendiente (Laura Morales) - Compra de acabados lista para completar o cancelar en pruebas
        if (products.TryGetValue("Pintura Viniltex Tipo 1 Blanco Balde 5 Galones", out var pintura) &&
            products.TryGetValue("Estuco Plástico Listo Corona Balde 25 Kg", out var estuco) &&
            products.TryGetValue("Impermeabilizante Sika 1 Galón 4 Kg", out var impermeabilizante))
        {
            var sale3 = new Sale(customerLaura.Id, "COP")
            {
                SaleDate = DateTime.UtcNow
            };

            sale3.AddDetail(pintura, 2);
            sale3.AddDetail(estuco, 3);
            sale3.AddDetail(impermeabilizante, 2);
            // Queda en estado Pending

            context.Sales.Add(sale3);
            logger?.LogInformation("Venta de prueba #3 (Pendiente) generada para {Customer} por {Total}", customerLaura.FirstName, sale3.TotalAmount);
        }

        await context.SaveChangesAsync();
    }
}
