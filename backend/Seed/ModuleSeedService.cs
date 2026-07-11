using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BusinessOps.Backend.Auth;
using BusinessOps.Backend.Data;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Seed;

public static class ModuleSeedService
{
    private static readonly string[] SystemRoleNames =
    [
        "Owner",
        "Admin",
        "Project Manager",
        "Developer",
        "Designer",
        "Sales",
        "Finance",
        "Viewer"
    ];

    private static readonly HashSet<string> DefaultBusinessModuleNames =
    [
        "Tasks",
        "Team Members",
        "Projects",
        "Sales",
        "Expenses",
        "Inventory",
        "Calendar",
        "Clients"
    ];

    private static readonly HashSet<string> LegacyArchivedModuleNames =
    [
        "Journal",
        "Mood",
        "Mood Tracker",
        "Habit Tracker",
        "Cigarette Counter",
        "Meds Tracker",
        "Medication Tracker"
    ];

    private static readonly string[] AllDemoRoleNames =
    [
        "Owner",
        "Admin",
        "Project Manager",
        "Developer",
        "Designer",
        "Sales",
        "Finance",
        "Viewer"
    ];

    private static readonly DateTime DemoTimelineSourceStart = new(2026, 5, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DemoTimelineSourceEnd = new(2026, 7, 12, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DemoTimelineTargetStart = new(2025, 7, 11, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DemoTimelineTargetEnd = new(2026, 7, 25, 18, 0, 0, DateTimeKind.Utc);

    private sealed record SeedField(string Name, string Type, string TimestampDisplayMode = "auto");
    private sealed record SeedModule(string Name, IReadOnlyList<SeedField> Fields);
    private sealed record DemoUserSeed(string FullName, string Email, string RoleName);

    public static async Task EnsureDefaultModulesAsync(AppDbContext dbContext)
    {
        var defaultModules = new[]
        {
            new SeedModule("Tasks", new[]
            {
                new SeedField("title", "text"),
                new SeedField("status", "select"),
                new SeedField("priority", "select"),
                new SeedField("assignedTo", "text"),
                new SeedField("project", "text"),
                new SeedField("dueTimestamp", "timestamp", "dateTime"),
                new SeedField("estimatedHours", "number"),
                new SeedField("actualHours", "number"),
                new SeedField("notes", "text")
            }),
            new SeedModule("Projects", new[]
            {
                new SeedField("name", "text"),
                new SeedField("client", "text"),
                new SeedField("status", "select"),
                new SeedField("projectManager", "text"),
                new SeedField("startTimestamp", "timestamp", "dateTime"),
                new SeedField("deadlineTimestamp", "timestamp", "dateTime"),
                new SeedField("budget", "number"),
                new SeedField("billedAmount", "number"),
                new SeedField("notes", "text")
            }),
            new SeedModule("Clients", new[]
            {
                new SeedField("name", "text"),
                new SeedField("contactPerson", "text"),
                new SeedField("email", "text"),
                new SeedField("phone", "text"),
                new SeedField("status", "select"),
                new SeedField("industry", "text"),
                new SeedField("notes", "text")
            }),
            new SeedModule("Sales", new[]
            {
                new SeedField("client", "text"),
                new SeedField("project", "text"),
                new SeedField("amount", "number"),
                new SeedField("status", "select"),
                new SeedField("saleTimestamp", "timestamp", "dateTime"),
                new SeedField("owner", "text"),
                new SeedField("notes", "text")
            }),
            new SeedModule("Expenses", new[]
            {
                new SeedField("category", "select"),
                new SeedField("amount", "number"),
                new SeedField("vendor", "text"),
                new SeedField("expenseTimestamp", "timestamp", "dateTime"),
                new SeedField("project", "text"),
                new SeedField("notes", "text")
            }),
            new SeedModule("Inventory", new[]
            {
                new SeedField("itemName", "text"),
                new SeedField("category", "select"),
                new SeedField("sku", "text"),
                new SeedField("quantity", "number"),
                new SeedField("unitCost", "number"),
                new SeedField("reorderLevel", "number"),
                new SeedField("assignedTo", "text"),
                new SeedField("supplier", "text"),
                new SeedField("notes", "text")
            }),
            new SeedModule("Calendar", new[]
            {
                new SeedField("title", "text"),
                new SeedField("startTimestamp", "timestamp", "dateTime"),
                new SeedField("endTimestamp", "timestamp", "dateTime"),
                new SeedField("assignedTo", "text"),
                new SeedField("project", "text"),
                new SeedField("notes", "text")
            }),
            new SeedModule("Team Members", new[]
            {
                new SeedField("name", "text"),
                new SeedField("role", "text"),
                new SeedField("email", "text"),
                new SeedField("phone", "text"),
                new SeedField("status", "select"),
                new SeedField("department", "select"),
                new SeedField("hourlyRate", "number"),
                new SeedField("notes", "text")
            }),
        };

        foreach (var definition in defaultModules)
        {
            var existing = await dbContext.Modules
                .FirstOrDefaultAsync(x => x.Name.ToLower() == definition.Name.ToLower());

            if (existing is null)
            {
                var module = new Module
                {
                    Id = Guid.NewGuid(),
                    Name = definition.Name,
                    Fields = JsonSerializer.SerializeToElement(definition.Fields.Select(ToFieldObject)),
                    CreatedAt = DateTime.UtcNow
                };

                await dbContext.Modules.AddAsync(module);
                continue;
            }

            var mergedFields = MergeMissingFields(existing.Fields, definition.Fields, out var changed);
            if (changed)
            {
                existing.Fields = JsonSerializer.SerializeToElement(mergedFields);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public static async Task EnsureSecurityDefaultsAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        PasswordHasherService passwordHasherService)
    {
        await EnsureRolesAsync(dbContext);
        await EnsureModulePermissionsAsync(dbContext);
        await EnsureFieldPermissionsAsync(dbContext);
        await EnsureDemoUsersAsync(dbContext, configuration, passwordHasherService);
        await EnsureDemoCompanyDataAsync(dbContext);
        await dbContext.SaveChangesAsync();
    }

    public static async Task EnsurePermissionsForModuleAsync(AppDbContext dbContext, Module module)
    {
        await EnsureRolesAsync(dbContext);

        var roles = await dbContext.Roles.AsNoTracking().ToListAsync();
        var existingModulePermissions = await dbContext.ModulePermissions
            .Where(x => x.ModuleId == module.Id)
            .ToListAsync();

        foreach (var role in roles)
        {
            var defaults = GetDefaultModulePermission(role.Name, module.Name);
            var current = existingModulePermissions.FirstOrDefault(x => x.RoleId == role.Id);
            if (current is null)
            {
                current = new ModulePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    ModuleId = module.Id
                };
                await dbContext.ModulePermissions.AddAsync(current);
            }

            current.CanView = defaults.CanView;
            current.CanCreate = defaults.CanCreate;
            current.CanEdit = defaults.CanEdit;
            current.CanDelete = defaults.CanDelete;
            current.CanManagePermissions = defaults.CanManagePermissions;
        }

        await dbContext.SaveChangesAsync();
        await EnsureFieldPermissionsForModuleAsync(dbContext, module);
    }

    public static bool IsSystemRoleName(string roleName) =>
        SystemRoleNames.Contains(roleName, StringComparer.OrdinalIgnoreCase);

    public static bool IsDefaultBusinessModule(string moduleName) =>
        DefaultBusinessModuleNames.Contains(moduleName, StringComparer.OrdinalIgnoreCase);

    public static bool IsArchivedLegacyModule(string moduleName) =>
        LegacyArchivedModuleNames.Contains(moduleName, StringComparer.OrdinalIgnoreCase);

    public static string GetModuleCategory(string moduleName)
    {
        if (IsArchivedLegacyModule(moduleName))
        {
            return "Archived";
        }

        if (IsDefaultBusinessModule(moduleName))
        {
            return "Business";
        }

        return "Custom";
    }

    private static async Task EnsureRolesAsync(AppDbContext dbContext)
    {
        var existingNames = await dbContext.Roles
            .Select(x => x.Name)
            .ToListAsync();

        foreach (var roleName in SystemRoleNames)
        {
            if (existingNames.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            await dbContext.Roles.AddAsync(new Role
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                CreatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureModulePermissionsAsync(AppDbContext dbContext)
    {
        var roles = await dbContext.Roles.AsNoTracking().ToListAsync();
        var modules = await dbContext.Modules.AsNoTracking().ToListAsync();
        var existing = await dbContext.ModulePermissions.ToListAsync();

        foreach (var role in roles)
        {
            foreach (var module in modules)
            {
                var defaults = GetDefaultModulePermission(role.Name, module.Name);
                var current = existing.FirstOrDefault(x => x.RoleId == role.Id && x.ModuleId == module.Id);
                if (current is null)
                {
                    current = new ModulePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = role.Id,
                        ModuleId = module.Id
                    };
                    existing.Add(current);
                    await dbContext.ModulePermissions.AddAsync(current);
                }

                current.CanView = defaults.CanView;
                current.CanCreate = defaults.CanCreate;
                current.CanEdit = defaults.CanEdit;
                current.CanDelete = defaults.CanDelete;
                current.CanManagePermissions = defaults.CanManagePermissions;
            }
        }
    }

    private static async Task EnsureFieldPermissionsAsync(AppDbContext dbContext)
    {
        var roles = await dbContext.Roles.AsNoTracking().ToListAsync();
        var modules = await dbContext.Modules.AsNoTracking().ToListAsync();

        foreach (var module in modules)
        {
            foreach (var role in roles)
            {
                await EnsureExplicitFieldPermissionsForRoleAsync(dbContext, role, module);
            }
        }
    }

    private static async Task EnsureFieldPermissionsForModuleAsync(AppDbContext dbContext, Module module)
    {
        var roles = await dbContext.Roles.AsNoTracking().ToListAsync();
        foreach (var role in roles)
        {
            await EnsureExplicitFieldPermissionsForRoleAsync(dbContext, role, module);
        }
    }

    private static async Task EnsureExplicitFieldPermissionsForRoleAsync(AppDbContext dbContext, Role role, Module module)
    {
        var fieldNames = ExtractFieldNames(module.Fields);
        if (fieldNames.Count == 0)
        {
            return;
        }

        var existing = await dbContext.FieldPermissions
            .Where(x => x.RoleId == role.Id && x.ModuleId == module.Id)
            .ToListAsync();

        foreach (var fieldName in fieldNames)
        {
            var defaults = GetDefaultFieldPermission(role.Name, module.Name, fieldName);
            var current = existing.FirstOrDefault(x =>
                x.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            if (defaults is null)
            {
                if (current is not null && IsSystemRoleName(role.Name))
                {
                    dbContext.FieldPermissions.Remove(current);
                }
                continue;
            }

            if (current is null)
            {
                current = new FieldPermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    ModuleId = module.Id,
                    FieldName = fieldName
                };
                await dbContext.FieldPermissions.AddAsync(current);
            }

            current.CanView = defaults.Value.CanView;
            current.CanEdit = defaults.Value.CanEdit;
        }
    }

    private static IReadOnlyList<object> MergeMissingFields(
        JsonElement existingFields,
        IReadOnlyList<SeedField> requiredFields,
        out bool changed)
    {
        var normalizedExisting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<object>();

        if (existingFields.ValueKind == JsonValueKind.Array)
        {
            foreach (var field in existingFields.EnumerateArray())
            {
                if (field.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var fieldName = field.TryGetProperty("name", out var nameEl)
                    ? (nameEl.GetString() ?? string.Empty).Trim()
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(fieldName))
                {
                    continue;
                }

                var fieldType = field.TryGetProperty("type", out var typeEl)
                    ? (typeEl.GetString() ?? "text").Trim()
                    : "text";
                var displayMode = field.TryGetProperty("timestampDisplayMode", out var displayEl)
                    ? (displayEl.GetString() ?? "auto").Trim()
                    : "auto";

                merged.Add(new
                {
                    name = fieldName,
                    type = FieldTypeConstants.NormalizeFieldType(fieldType),
                    timestampDisplayMode = string.IsNullOrWhiteSpace(displayMode) ? "auto" : displayMode
                });

                normalizedExisting.Add(Normalize(fieldName));
            }
        }

        changed = false;
        foreach (var required in requiredFields)
        {
            if (normalizedExisting.Contains(Normalize(required.Name)))
            {
                continue;
            }

            merged.Add(ToFieldObject(required));
            changed = true;
        }

        return merged;
    }

    private static object ToFieldObject(SeedField field)
    {
        return new
        {
            name = field.Name,
            type = FieldTypeConstants.NormalizeFieldType(field.Type),
            timestampDisplayMode = field.TimestampDisplayMode
        };
    }

    private static string Normalize(string value)
    {
        return new string(value
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static List<string> ExtractFieldNames(JsonElement fields)
    {
        if (fields.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return fields.EnumerateArray()
            .Select(x => x.TryGetProperty("name", out var nameProperty) ? nameProperty.GetString() : null)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();
    }

    private static (bool CanView, bool CanCreate, bool CanEdit, bool CanDelete, bool CanManagePermissions)
        GetDefaultModulePermission(string roleName, string moduleName)
    {
        var role = roleName.Trim().ToLowerInvariant();
        var module = Normalize(moduleName);

        return role switch
        {
            "owner" => (true, true, true, true, true),
            "admin" => (true, true, true, true, true),
            "project manager" => module switch
            {
                "projects" or "tasks" or "calendar" or "clients" => (true, true, true, true, false),
                "teammembers" => (true, false, false, false, false),
                "sales" => (true, false, false, false, false),
                _ => (false, false, false, false, false)
            },
            "developer" => module switch
            {
                "tasks" => (true, false, true, false, false),
                "projects" => (true, false, false, false, false),
                "calendar" => (true, true, true, false, false),
                "clients" => (true, false, false, false, false),
                _ => (false, false, false, false, false)
            },
            "designer" => module switch
            {
                "tasks" => (true, false, true, false, false),
                "projects" => (true, false, false, false, false),
                "calendar" => (true, true, true, false, false),
                "clients" => (true, false, false, false, false),
                _ => (false, false, false, false, false)
            },
            "sales" => module switch
            {
                "clients" or "sales" or "calendar" => (true, true, true, false, false),
                "projects" => (true, false, false, false, false),
                _ => (false, false, false, false, false)
            },
            "finance" => module switch
            {
                "sales" or "expenses" => (true, true, true, true, false),
                "projects" or "clients" => (true, false, false, false, false),
                _ => (false, false, false, false, false)
            },
            "viewer" => module switch
            {
                "projects" or "clients" => (true, false, false, false, false),
                _ => (false, false, false, false, false)
            },
            _ => (false, false, false, false, false)
        };
    }

    private static (bool CanView, bool CanEdit)? GetDefaultFieldPermission(
        string roleName,
        string moduleName,
        string fieldName)
    {
        var role = roleName.Trim().ToLowerInvariant();
        var module = Normalize(moduleName);
        var field = Normalize(fieldName);

        if (role is "owner" or "admin")
        {
            return null;
        }

        if (module == "teammembers" && field == "hourlyrate")
        {
            return role switch
            {
                "finance" or "project manager" => (true, false),
                _ => (false, false)
            };
        }

        if (module == "projects" && (field == "budget" || field == "billedamount"))
        {
            return role switch
            {
                "project manager" or "finance" => (true, false),
                "developer" or "designer" or "sales" or "viewer" => (false, false),
                _ => null
            };
        }

        if (module == "sales" && field == "amount")
        {
            return role switch
            {
                "sales" or "finance" => null,
                _ => (false, false)
            };
        }

        if (module == "expenses" && field == "amount")
        {
            return role switch
            {
                "finance" => null,
                _ => (false, false)
            };
        }

        if (module == "inventory" && field == "unitcost")
        {
            return role switch
            {
                "finance" => null,
                _ => (false, false)
            };
        }

        if (module == "clients" && role is "developer" or "designer" or "viewer")
        {
            return field switch
            {
                "phone" or "notes" => (false, false),
                _ => null
            };
        }

        if (module == "expenses" && role == "project manager" && field == "notes")
        {
            return (true, false);
        }

        return null;
    }

    private static async Task EnsureDemoUsersAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        PasswordHasherService passwordHasherService)
    {
        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var roles = await dbContext.Roles
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase);

        var ownerEmail = configuration["ADMIN_EMAIL"] ??
            configuration["BootstrapAdmin:Email"] ??
            "alex@bytebridge.local";
        var ownerPassword = configuration["ADMIN_PASSWORD"] ??
            configuration["BootstrapAdmin:Password"] ??
            "Password123!";
        var ownerFullName = configuration["ADMIN_FULL_NAME"] ??
            configuration["BootstrapAdmin:FullName"] ??
            "Alex Ionescu";

        var users = new[]
        {
            new DemoUserSeed(ownerFullName, ownerEmail, "Owner"),
            new DemoUserSeed("Maria Popescu", "maria@bytebridge.local", "Admin"),
            new DemoUserSeed("Vlad Georgescu", "vlad@bytebridge.local", "Project Manager"),
            new DemoUserSeed("Andrei Stan", "andrei@bytebridge.local", "Developer"),
            new DemoUserSeed("Ioana Radu", "ioana@bytebridge.local", "Developer"),
            new DemoUserSeed("Elena Marin", "elena@bytebridge.local", "Designer"),
            new DemoUserSeed("Radu Matei", "radu@bytebridge.local", "Sales"),
            new DemoUserSeed("Cristina Pavel", "cristina@bytebridge.local", "Finance"),
            new DemoUserSeed("Demo Viewer", "demo.viewer@bytebridge.local", "Viewer")
        };

        foreach (var user in users)
        {
            if (!roles.TryGetValue(user.RoleName, out var role))
            {
                continue;
            }

            var password = user.RoleName.Equals("Owner", StringComparison.OrdinalIgnoreCase)
                ? ownerPassword.Trim()
                : "Password123!";

            await dbContext.Users.AddAsync(new User
            {
                Id = Guid.NewGuid(),
                Email = user.Email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHasherService.HashPassword(password),
                FullName = user.FullName.Trim(),
                RoleId = role.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureDemoCompanyDataAsync(AppDbContext dbContext)
    {
        var modules = await dbContext.Modules
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var users = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .ToDictionaryAsync(x => x.Email, StringComparer.OrdinalIgnoreCase);
        var roles = await dbContext.Roles
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase);

        await EnsureTeamMembersDataAsync(dbContext, modules, users, roles);
        await EnsureClientDataAsync(dbContext, modules, users, roles);
        await EnsureProjectDataAsync(dbContext, modules, users, roles);
        await EnsureTaskDataAsync(dbContext, modules, users, roles);
        await EnsureSalesDataAsync(dbContext, modules, users, roles);
        await EnsureExpenseDataAsync(dbContext, modules, users, roles);
        await EnsureInventoryDataAsync(dbContext, modules, users, roles);
        await EnsureCalendarDataAsync(dbContext, modules, users, roles);
        await EnsureDashboardDemoAsync(dbContext, modules);
    }

    private static async Task EnsureTeamMembersDataAsync(
        AppDbContext dbContext,
        IReadOnlyDictionary<string, Module> modules,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Role> roles)
    {
        if (!modules.TryGetValue("Team Members", out var module) ||
            await dbContext.Entries.AnyAsync(x => x.ModuleId == module.Id))
        {
            return;
        }

        var admin = users["maria@bytebridge.local"];
        var records = new[]
        {
            new Dictionary<string, object?>
            {
                ["name"] = "Alex Ionescu",
                ["role"] = "Owner",
                ["email"] = "alex@bytebridge.local",
                ["phone"] = "+40 721 100 001",
                ["status"] = "Active",
                ["department"] = "Management",
                ["hourlyRate"] = 95,
                ["notes"] = "Founder and delivery sponsor for strategic accounts."
            },
            new Dictionary<string, object?>
            {
                ["name"] = "Maria Popescu",
                ["role"] = "Admin",
                ["email"] = "maria@bytebridge.local",
                ["phone"] = "+40 721 100 002",
                ["status"] = "Active",
                ["department"] = "Admin",
                ["hourlyRate"] = 55,
                ["notes"] = "Runs operations, internal tooling, and user management."
            },
            new Dictionary<string, object?>
            {
                ["name"] = "Vlad Georgescu",
                ["role"] = "Project Manager",
                ["email"] = "vlad@bytebridge.local",
                ["phone"] = "+40 721 100 003",
                ["status"] = "Active",
                ["department"] = "Management",
                ["hourlyRate"] = 70,
                ["notes"] = "Owns project delivery cadence and client planning."
            },
            new Dictionary<string, object?>
            {
                ["name"] = "Andrei Stan",
                ["role"] = "Developer",
                ["email"] = "andrei@bytebridge.local",
                ["phone"] = "+40 721 100 004",
                ["status"] = "Active",
                ["department"] = "Development",
                ["hourlyRate"] = 60,
                ["notes"] = "Backend and auth implementation lead."
            },
            new Dictionary<string, object?>
            {
                ["name"] = "Ioana Radu",
                ["role"] = "Developer",
                ["email"] = "ioana@bytebridge.local",
                ["phone"] = "+40 721 100 005",
                ["status"] = "Active",
                ["department"] = "Development",
                ["hourlyRate"] = 58,
                ["notes"] = "Frontend delivery and QA automation."
            },
            new Dictionary<string, object?>
            {
                ["name"] = "Elena Marin",
                ["role"] = "Designer",
                ["email"] = "elena@bytebridge.local",
                ["phone"] = "+40 721 100 006",
                ["status"] = "Active",
                ["department"] = "Design",
                ["hourlyRate"] = 52,
                ["notes"] = "Owns wireframes, UI systems, and client demos."
            },
            new Dictionary<string, object?>
            {
                ["name"] = "Radu Matei",
                ["role"] = "Sales",
                ["email"] = "radu@bytebridge.local",
                ["phone"] = "+40 721 100 007",
                ["status"] = "Active",
                ["department"] = "Sales",
                ["hourlyRate"] = 48,
                ["notes"] = "Runs pipeline and account expansion."
            },
            new Dictionary<string, object?>
            {
                ["name"] = "Cristina Pavel",
                ["role"] = "Finance",
                ["email"] = "cristina@bytebridge.local",
                ["phone"] = "+40 721 100 008",
                ["status"] = "Active",
                ["department"] = "Finance",
                ["hourlyRate"] = 50,
                ["notes"] = "Tracks invoicing, expenses, and margin reporting."
            },
            new Dictionary<string, object?>
            {
                ["name"] = "Sonia Enache",
                ["role"] = "Developer",
                ["email"] = "sonia@bytebridge.local",
                ["phone"] = "+40 721 100 009",
                ["status"] = "Active",
                ["department"] = "Development",
                ["hourlyRate"] = 57,
                ["notes"] = "Owns integration testing and release support."
            },
            new Dictionary<string, object?>
            {
                ["name"] = "Paul Dumitrescu",
                ["role"] = "Support",
                ["email"] = "paul@bytebridge.local",
                ["phone"] = "+40 721 100 010",
                ["status"] = "Active",
                ["department"] = "Operations",
                ["hourlyRate"] = 44,
                ["notes"] = "Handles support handoff and customer follow-ups."
            }
        };

        var baseDate = new DateTime(2026, 6, 2, 8, 30, 0, DateTimeKind.Utc);
        for (var i = 0; i < records.Length; i++)
        {
            await AddEntryWithVisibilityAsync(
                dbContext,
                module,
                admin,
                roles,
                ScaleDemoDate(baseDate.AddDays(i)),
                records[i],
                ["Owner", "Admin", "Project Manager", "Finance"],
                []);
        }
    }

    private static async Task EnsureClientDataAsync(
        AppDbContext dbContext,
        IReadOnlyDictionary<string, Module> modules,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Role> roles)
    {
        if (!modules.TryGetValue("Clients", out var module) ||
            await dbContext.Entries.AnyAsync(x => x.ModuleId == module.Id))
        {
            return;
        }

        var sales = users["radu@bytebridge.local"];
        var admin = users["maria@bytebridge.local"];
        var clients = new[]
        {
            new { Name = "Nova Retail SRL", Contact = "Oana Dumitru", Email = "oana@novaretail.ro", Phone = "+40 731 300 101", Status = "Active", Industry = "Retail", Notes = "Needs executive dashboard and store KPI alerts.", VisibleToViewer = true },
            new { Name = "GreenFarm Logistics", Contact = "Mihai Petrescu", Email = "mihai@greenfarm.ro", Phone = "+40 731 300 102", Status = "Active", Industry = "Logistics", Notes = "Interested in vehicle telemetry and maintenance planning.", VisibleToViewer = true },
            new { Name = "MedCore Clinic", Contact = "Ana Rusu", Email = "ana@medcore.ro", Phone = "+40 731 300 103", Status = "Active", Industry = "Healthcare", Notes = "Prefers phased rollout with audit logging.", VisibleToViewer = false },
            new { Name = "UrbanFit Studio", Contact = "Bogdan Vasile", Email = "bogdan@urbanfit.ro", Phone = "+40 731 300 104", Status = "Lead", Industry = "Fitness", Notes = "Mobile-first booking flow and trainer scheduling.", VisibleToViewer = false },
            new { Name = "Atlas Accounting", Contact = "Roxana Ene", Email = "roxana@atlasacc.ro", Phone = "+40 731 300 105", Status = "Active", Industry = "Accounting", Notes = "Automation around invoices and reminders.", VisibleToViewer = false },
            new { Name = "BrightSchool Online", Contact = "Carmen Ilie", Email = "carmen@brightschool.ro", Phone = "+40 731 300 106", Status = "Paused", Industry = "Education", Notes = "Paused until September budget review.", VisibleToViewer = false },
            new { Name = "Northstar Legal", Contact = "Irina Pavel", Email = "irina@northstarlegal.ro", Phone = "+40 731 300 107", Status = "Lead", Industry = "Legal", Notes = "Interested in contract workflow automation and document search.", VisibleToViewer = false },
            new { Name = "BlueWave Energy", Contact = "Tudor Neagu", Email = "tudor@bluewave-energy.ro", Phone = "+40 731 300 108", Status = "Active", Industry = "Energy", Notes = "Wants reporting dashboards for field interventions and SLAs.", VisibleToViewer = true }
        };

        var baseDate = new DateTime(2026, 6, 3, 9, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < clients.Length; i++)
        {
            var item = clients[i];
            var creator = i % 2 == 0 ? sales : admin;
            var visibleRoles = item.VisibleToViewer
                ? new[] { "Owner", "Admin", "Project Manager", "Developer", "Designer", "Sales", "Finance", "Viewer" }
                : new[] { "Owner", "Admin", "Project Manager", "Developer", "Designer", "Sales", "Finance" };

            await AddEntryWithVisibilityAsync(
                dbContext,
                module,
                creator,
                roles,
                ScaleDemoDate(baseDate.AddDays(i)),
                new Dictionary<string, object?>
                {
                    ["name"] = item.Name,
                    ["contactPerson"] = item.Contact,
                    ["email"] = item.Email,
                    ["phone"] = item.Phone,
                    ["status"] = item.Status,
                    ["industry"] = item.Industry,
                    ["notes"] = item.Notes
                },
                visibleRoles,
                ["Owner", "Admin", "Sales"]);
        }
    }

    private static async Task EnsureProjectDataAsync(
        AppDbContext dbContext,
        IReadOnlyDictionary<string, Module> modules,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Role> roles)
    {
        if (!modules.TryGetValue("Projects", out var module) ||
            await dbContext.Entries.AnyAsync(x => x.ModuleId == module.Id))
        {
            return;
        }

        var manager = users["vlad@bytebridge.local"];
        var projects = new[]
        {
            new { Name = "Nova Retail Dashboard", Client = "Nova Retail SRL", Status = "Active", Manager = "Vlad Georgescu", Start = SeedDate(2026, 6, 1, 9, 0, 0), Deadline = SeedDate(2026, 7, 15, 18, 0, 0), Budget = 18000, Billed = 9200, Notes = "Executive KPI dashboard with permissions refactor.", ViewerVisible = true },
            new { Name = "GreenFarm Fleet Tracker", Client = "GreenFarm Logistics", Status = "Active", Manager = "Vlad Georgescu", Start = SeedDate(2026, 6, 4, 9, 0, 0), Deadline = SeedDate(2026, 7, 25, 18, 0, 0), Budget = 22000, Billed = 11000, Notes = "Fleet monitoring, maintenance alerts, and ETA reporting.", ViewerVisible = true },
            new { Name = "MedCore Appointment Portal", Client = "MedCore Clinic", Status = "Planning", Manager = "Vlad Georgescu", Start = SeedDate(2026, 6, 9, 9, 0, 0), Deadline = SeedDate(2026, 8, 5, 18, 0, 0), Budget = 26000, Billed = 3000, Notes = "Portal kickoff with secure patient role access.", ViewerVisible = false },
            new { Name = "UrbanFit Mobile Booking", Client = "UrbanFit Studio", Status = "On Hold", Manager = "Vlad Georgescu", Start = SeedDate(2026, 6, 12, 9, 0, 0), Deadline = SeedDate(2026, 8, 20, 18, 0, 0), Budget = 14000, Billed = 2500, Notes = "Waiting on client photo assets and trainer schedules.", ViewerVisible = false },
            new { Name = "Atlas Invoice Automation", Client = "Atlas Accounting", Status = "Completed", Manager = "Vlad Georgescu", Start = SeedDate(2026, 5, 10, 9, 0, 0), Deadline = SeedDate(2026, 6, 22, 18, 0, 0), Budget = 16500, Billed = 16500, Notes = "Completed automation flow for invoice reminders and exports.", ViewerVisible = false },
            new { Name = "Northstar Contract Hub", Client = "Northstar Legal", Status = "Planning", Manager = "Vlad Georgescu", Start = SeedDate(2026, 7, 6, 9, 0, 0), Deadline = SeedDate(2026, 8, 28, 18, 0, 0), Budget = 21000, Billed = 0, Notes = "Contract drafting workspace with approval checkpoints.", ViewerVisible = false },
            new { Name = "BlueWave Field Operations", Client = "BlueWave Energy", Status = "Active", Manager = "Vlad Georgescu", Start = SeedDate(2026, 6, 20, 9, 0, 0), Deadline = SeedDate(2026, 8, 12, 18, 0, 0), Budget = 24000, Billed = 8000, Notes = "Field intervention dashboard and service-level tracking.", ViewerVisible = true }
        };

        foreach (var project in projects)
        {
            var visibleRoles = project.ViewerVisible
                ? new[] { "Owner", "Admin", "Project Manager", "Developer", "Designer", "Sales", "Finance", "Viewer" }
                : new[] { "Owner", "Admin", "Project Manager", "Developer", "Designer", "Sales", "Finance" };

            await AddEntryWithVisibilityAsync(
                dbContext,
                module,
                manager,
                roles,
                project.Start,
                new Dictionary<string, object?>
                {
                    ["name"] = project.Name,
                    ["client"] = project.Client,
                    ["status"] = project.Status,
                    ["projectManager"] = project.Manager,
                    ["startTimestamp"] = project.Start,
                    ["deadlineTimestamp"] = project.Deadline,
                    ["budget"] = project.Budget,
                    ["billedAmount"] = project.Billed,
                    ["notes"] = project.Notes
                },
                visibleRoles,
                ["Owner", "Admin", "Project Manager"]);
        }

        await AddEntryWithVisibilityAsync(
            dbContext,
            module,
            manager,
            roles,
            SeedDate(2026, 6, 18, 10, 0, 0),
            new Dictionary<string, object?>
            {
                ["name"] = "MedCore Appointment Portal",
                ["client"] = "MedCore Clinic",
                ["status"] = "Planning",
                ["projectManager"] = "Vlad Georgescu",
                ["startTimestamp"] = SeedDate(2026, 6, 18, 10, 0, 0),
                ["deadlineTimestamp"] = SeedDate(2026, 8, 5, 18, 0, 0),
                ["budget"] = 26000,
                ["billedAmount"] = 3000,
                ["notes"] = "Risk note: integration timeline depends on clinic vendor API approval."
            },
            ["Owner", "Admin", "Project Manager"],
            ["Owner", "Admin", "Project Manager"]);
    }

    private static async Task EnsureTaskDataAsync(
        AppDbContext dbContext,
        IReadOnlyDictionary<string, Module> modules,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Role> roles)
    {
        if (!modules.TryGetValue("Tasks", out var module) ||
            await dbContext.Entries.AnyAsync(x => x.ModuleId == module.Id))
        {
            return;
        }

        var manager = users["vlad@bytebridge.local"];
        var andrei = users["andrei@bytebridge.local"];
        var ioana = users["ioana@bytebridge.local"];
        var elena = users["elena@bytebridge.local"];

        var tasks = new[]
        {
            TaskSeed("Implement login permissions", "In Progress", "Critical", "Andrei Stan", "Nova Retail Dashboard", SeedDate(2026, 6, 6, 17, 0, 0), 12, 6, "JWT and field restrictions in progress.", manager, new[] { "Project Manager", "Developer" }),
            TaskSeed("Design dashboard wireframes", "Review", "High", "Elena Marin", "Nova Retail Dashboard", SeedDate(2026, 6, 5, 15, 0, 0), 8, 7, "Awaiting stakeholder comments.", manager, new[] { "Project Manager", "Designer" }),
            TaskSeed("Fix calendar week view", "Todo", "Medium", "Ioana Radu", "Nova Retail Dashboard", SeedDate(2026, 6, 8, 18, 0, 0), 5, 0, "Regression on responsive layout.", manager, new[] { "Project Manager", "Developer" }),
            TaskSeed("Build expenses chart", "Blocked", "High", "Ioana Radu", "GreenFarm Fleet Tracker", SeedDate(2026, 6, 10, 18, 0, 0), 6, 2, "Waiting for final category labels.", manager, new[] { "Project Manager", "Developer", "Finance" }),
            TaskSeed("Review API validation", "Done", "Medium", "Andrei Stan", "GreenFarm Fleet Tracker", SeedDate(2026, 6, 7, 16, 0, 0), 4, 4, "Completed request validation pass.", manager, new[] { "Project Manager", "Developer" }),
            TaskSeed("Deploy staging backend", "In Progress", "High", "Andrei Stan", "MedCore Appointment Portal", SeedDate(2026, 6, 11, 17, 30, 0), 3, 1, "Need firewall exceptions from vendor.", manager, new[] { "Project Manager", "Developer" }),
            TaskSeed("Prepare client demo", "Todo", "High", "Elena Marin", "MedCore Appointment Portal", SeedDate(2026, 6, 14, 14, 0, 0), 5, 0, "Demo script for appointment flow.", manager, new[] { "Project Manager", "Designer", "Sales" }),
            TaskSeed("Create sales proposal deck", "In Progress", "Medium", "Elena Marin", "UrbanFit Mobile Booking", SeedDate(2026, 6, 18, 12, 0, 0), 6, 2, "Tailored slides for booking funnel.", users["radu@bytebridge.local"], new[] { "Sales", "Designer", "Project Manager" }),
            TaskSeed("Sync project requirements", "Done", "Low", "Ioana Radu", "Atlas Invoice Automation", SeedDate(2026, 6, 3, 12, 0, 0), 2, 2, "Notes archived in project workspace.", manager, new[] { "Project Manager", "Developer" }),
            TaskSeed("Refactor permissions matrix", "In Progress", "Critical", "Andrei Stan", "Nova Retail Dashboard", SeedDate(2026, 6, 12, 18, 0, 0), 10, 4, "Aligning role and field rules.", manager, new[] { "Project Manager", "Developer", "Admin" }),
            TaskSeed("Implement project summary cards", "Todo", "Medium", "Ioana Radu", "GreenFarm Fleet Tracker", SeedDate(2026, 6, 13, 17, 0, 0), 7, 0, "Dashboard cards for ops leaders.", manager, new[] { "Project Manager", "Developer" }),
            TaskSeed("Update mobile booking brand assets", "Review", "Medium", "Elena Marin", "UrbanFit Mobile Booking", SeedDate(2026, 6, 16, 13, 0, 0), 4, 3, "Waiting on final logo export.", manager, new[] { "Project Manager", "Designer" }),
            TaskSeed("Confirm invoice reminder copy", "Done", "Low", "Elena Marin", "Atlas Invoice Automation", SeedDate(2026, 6, 4, 11, 0, 0), 2, 2, "Approved by client finance lead.", manager, new[] { "Project Manager", "Designer", "Finance" }),
            TaskSeed("Clean up migration scripts", "Todo", "Low", "Andrei Stan", "MedCore Appointment Portal", SeedDate(2026, 6, 20, 18, 0, 0), 3, 0, "Prepare deployment package.", manager, new[] { "Project Manager", "Developer" }),
            TaskSeed("Write QA checklist", "In Progress", "Medium", "Ioana Radu", "Nova Retail Dashboard", SeedDate(2026, 6, 15, 16, 0, 0), 5, 2, "Covers auth, clients, and dashboard filters.", manager, new[] { "Project Manager", "Developer" }),
            TaskSeed("Prepare sprint review notes", "Todo", "Low", "Vlad Georgescu", "GreenFarm Fleet Tracker", SeedDate(2026, 6, 17, 17, 0, 0), 2, 0, "Summarize blockers and wins.", manager, new[] { "Project Manager", "Owner", "Admin" }),
            TaskSeed("Audit client permissions", "Blocked", "High", "Andrei Stan", "MedCore Appointment Portal", SeedDate(2026, 6, 19, 17, 30, 0), 6, 1, "Need client matrix sign-off.", manager, new[] { "Project Manager", "Developer", "Admin" }),
            TaskSeed("Draft renewal proposal", "Todo", "Medium", "Radu Matei", "Nova Retail Dashboard", SeedDate(2026, 6, 21, 15, 0, 0), 3, 0, "Maintenance retainer extension.", users["radu@bytebridge.local"], new[] { "Sales", "Project Manager", "Owner", "Admin" }),
            TaskSeed("Budget variance review", "Todo", "High", "Cristina Pavel", "GreenFarm Fleet Tracker", SeedDate(2026, 6, 22, 15, 30, 0), 4, 0, "Cross-check billed vs project budget.", users["cristina@bytebridge.local"], new[] { "Finance", "Project Manager", "Owner", "Admin" }),
            TaskSeed("Validate handoff checklist", "Done", "Medium", "Vlad Georgescu", "Atlas Invoice Automation", SeedDate(2026, 6, 2, 15, 0, 0), 2, 2, "Project handoff completed.", manager, new[] { "Project Manager", "Owner", "Admin", "Viewer" }),
            TaskSeed("Map contract approval stages", "Todo", "Medium", "Sonia Enache", "Northstar Contract Hub", SeedDate(2026, 7, 8, 17, 0, 0), 6, 0, "Outline legal review and signature checkpoints.", manager, new[] { "Project Manager", "Developer", "Admin" }),
            TaskSeed("Build SLA escalation widget", "In Progress", "High", "Ioana Radu", "BlueWave Field Operations", SeedDate(2026, 7, 10, 18, 0, 0), 9, 3, "Dashboard widget for overdue field incidents.", manager, new[] { "Project Manager", "Developer" }),
            TaskSeed("Prepare legal client kickoff notes", "Review", "Low", "Vlad Georgescu", "Northstar Contract Hub", SeedDate(2026, 7, 7, 13, 0, 0), 2, 2, "Shared scope and rollout assumptions.", manager, new[] { "Project Manager", "Owner", "Admin" }),
            TaskSeed("Validate technician filtering rules", "Todo", "Medium", "Andrei Stan", "BlueWave Field Operations", SeedDate(2026, 7, 11, 16, 30, 0), 5, 0, "Need role-based filtering for dispatch board.", manager, new[] { "Project Manager", "Developer" })
        };

        foreach (var task in tasks)
        {
            await AddEntryWithVisibilityAsync(
                dbContext,
                module,
                task.Creator,
                roles,
                task.DueTimestamp.AddDays(-2),
                new Dictionary<string, object?>
                {
                    ["title"] = task.Title,
                    ["status"] = task.Status,
                    ["priority"] = task.Priority,
                    ["assignedTo"] = task.AssignedTo,
                    ["project"] = task.Project,
                    ["dueTimestamp"] = task.DueTimestamp,
                    ["estimatedHours"] = task.EstimatedHours,
                    ["actualHours"] = task.ActualHours,
                    ["notes"] = task.Notes
                },
                task.VisibleRoles,
                task.VisibleRoles.Where(x => x is "Project Manager" or "Developer" or "Designer" or "Owner" or "Admin").ToArray());
        }
    }

    private static async Task EnsureSalesDataAsync(
        AppDbContext dbContext,
        IReadOnlyDictionary<string, Module> modules,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Role> roles)
    {
        if (!modules.TryGetValue("Sales", out var module) ||
            await dbContext.Entries.AnyAsync(x => x.ModuleId == module.Id))
        {
            return;
        }

        var sales = users["radu@bytebridge.local"];
        var entries = new[]
        {
            SaleSeed("Nova Retail SRL", "Nova Retail Dashboard", 9200, "Paid", SeedDate(2026, 6, 5, 10, 0, 0), "Radu Matei", "Initial milestone invoice paid."),
            SaleSeed("GreenFarm Logistics", "GreenFarm Fleet Tracker", 11000, "Paid", SeedDate(2026, 6, 8, 10, 0, 0), "Radu Matei", "Discovery and API planning retained."),
            SaleSeed("MedCore Clinic", "MedCore Appointment Portal", 3000, "Won", SeedDate(2026, 6, 12, 11, 0, 0), "Radu Matei", "Kickoff deposit for portal planning."),
            SaleSeed("UrbanFit Studio", "UrbanFit Mobile Booking", 4800, "Proposal Sent", SeedDate(2026, 6, 14, 15, 0, 0), "Radu Matei", "Proposal sent after design scoping."),
            SaleSeed("Atlas Accounting", "Atlas Invoice Automation", 16500, "Paid", SeedDate(2026, 6, 16, 13, 0, 0), "Radu Matei", "Final invoice settled."),
            SaleSeed("BrightSchool Online", "Student Analytics Dashboard", 6500, "Lead", SeedDate(2026, 6, 18, 9, 30, 0), "Radu Matei", "Lead discussing September rollout."),
            SaleSeed("Nova Retail SRL", "Maintenance Retainer Q3", 4200, "Won", SeedDate(2026, 6, 22, 16, 0, 0), "Radu Matei", "Retainer extension approved."),
            SaleSeed("GreenFarm Logistics", "Support Pack", 2600, "Proposal Sent", SeedDate(2026, 6, 25, 10, 30, 0), "Radu Matei", "Quarterly support pack follow-up."),
            SaleSeed("MedCore Clinic", "Compliance Add-on", 3800, "Lead", SeedDate(2026, 7, 2, 14, 0, 0), "Radu Matei", "Possible audit reporting extension."),
            SaleSeed("UrbanFit Studio", "Booking MVP", 0, "Lost", SeedDate(2026, 7, 4, 17, 0, 0), "Radu Matei", "Paused after budget reallocation."),
            SaleSeed("Northstar Legal", "Northstar Contract Hub", 5200, "Proposal Sent", SeedDate(2026, 7, 7, 11, 30, 0), "Radu Matei", "Proposal sent after discovery and legal workflow mapping."),
            SaleSeed("BlueWave Energy", "BlueWave Field Operations", 8000, "Won", SeedDate(2026, 7, 9, 10, 15, 0), "Radu Matei", "Initial implementation milestone approved."),
            SaleSeed("BlueWave Energy", "SLA Analytics Add-on", 3500, "Lead", SeedDate(2026, 7, 12, 15, 0, 0), "Radu Matei", "Discussing optional reporting add-on.")
        };

        foreach (var entry in entries)
        {
            await AddEntryWithVisibilityAsync(
                dbContext,
                module,
                sales,
                roles,
                entry.Timestamp,
                new Dictionary<string, object?>
                {
                    ["client"] = entry.Client,
                    ["project"] = entry.Project,
                    ["amount"] = entry.Amount,
                    ["status"] = entry.Status,
                    ["saleTimestamp"] = entry.Timestamp,
                    ["owner"] = entry.Owner,
                    ["notes"] = entry.Notes
                },
                ["Owner", "Admin", "Project Manager", "Sales", "Finance"],
                ["Owner", "Admin", "Sales", "Finance"]);
        }
    }

    private static async Task EnsureExpenseDataAsync(
        AppDbContext dbContext,
        IReadOnlyDictionary<string, Module> modules,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Role> roles)
    {
        if (!modules.TryGetValue("Expenses", out var module) ||
            await dbContext.Entries.AnyAsync(x => x.ModuleId == module.Id))
        {
            return;
        }

        var finance = users["cristina@bytebridge.local"];
        var expenses = new[]
        {
            ExpenseSeed("Software", 149, "JetBrains", SeedDate(2026, 6, 3, 9, 0, 0), "Nova Retail Dashboard", "JetBrains licenses for backend team.", ["Owner", "Admin", "Finance"]),
            ExpenseSeed("Software", 96, "GitHub", SeedDate(2026, 6, 4, 9, 0, 0), "Operations", "GitHub team plan renewal.", AllDemoRoleNames),
            ExpenseSeed("Hosting", 220, "Hetzner", SeedDate(2026, 6, 6, 9, 0, 0), "GreenFarm Fleet Tracker", "Hetzner cloud server for staging and VPN.", ["Owner", "Admin", "Finance"]),
            ExpenseSeed("Software", 84, "Figma", SeedDate(2026, 6, 7, 9, 0, 0), "UrbanFit Mobile Booking", "Figma subscription for prototype reviews.", AllDemoRoleNames),
            ExpenseSeed("Hardware", 760, "eMAG Business", SeedDate(2026, 6, 10, 9, 0, 0), "Operations", "Office monitors for the delivery team.", ["Owner", "Admin", "Finance"]),
            ExpenseSeed("Other", 18, "Namecheap", SeedDate(2026, 6, 11, 9, 0, 0), "Operations", "Domain renewal for internal demo space.", AllDemoRoleNames),
            ExpenseSeed("Salaries", 5200, "Payroll Batch", SeedDate(2026, 6, 15, 9, 0, 0), "Operations", "Monthly contractor and salary payout summary.", ["Owner", "Admin", "Finance"]),
            ExpenseSeed("Marketing", 430, "LinkedIn Ads", SeedDate(2026, 6, 17, 9, 0, 0), "Pipeline", "Retargeting campaign for analytics dashboards.", ["Owner", "Admin", "Finance", "Sales"]),
            ExpenseSeed("Travel", 190, "Uber Business", SeedDate(2026, 6, 20, 9, 0, 0), "MedCore Appointment Portal", "Client workshop travel reimbursement.", ["Owner", "Admin", "Finance", "Project Manager"]),
            ExpenseSeed("Hosting", 140, "Cloudflare", SeedDate(2026, 6, 24, 9, 0, 0), "Nova Retail Dashboard", "WAF and caching plan.", ["Owner", "Admin", "Finance"]),
            ExpenseSeed("Software", 65, "Notion", SeedDate(2026, 6, 28, 9, 0, 0), "Operations", "Knowledge base seats.", AllDemoRoleNames),
            ExpenseSeed("Office", 120, "IKEA Business", SeedDate(2026, 7, 2, 9, 0, 0), "Operations", "Desk accessories and cable management.", ["Owner", "Admin", "Finance"]),
            ExpenseSeed("Consulting", 680, "LegalTech Advisors", SeedDate(2026, 7, 7, 9, 0, 0), "Northstar Contract Hub", "External workflow review session.", ["Owner", "Admin", "Finance", "Project Manager"]),
            ExpenseSeed("Travel", 240, "OMV Fuel Cards", SeedDate(2026, 7, 9, 9, 0, 0), "BlueWave Field Operations", "Field workshop travel and client site visit.", ["Owner", "Admin", "Finance", "Project Manager"]),
            ExpenseSeed("Software", 129, "Postman", SeedDate(2026, 7, 11, 9, 0, 0), "Operations", "API collaboration plan for new integrations.", AllDemoRoleNames)
        };

        foreach (var expense in expenses)
        {
            await AddEntryWithVisibilityAsync(
                dbContext,
                module,
                finance,
                roles,
                expense.Timestamp,
                new Dictionary<string, object?>
                {
                    ["category"] = expense.Category,
                    ["amount"] = expense.Amount,
                    ["vendor"] = expense.Vendor,
                    ["expenseTimestamp"] = expense.Timestamp,
                    ["project"] = expense.Project,
                    ["notes"] = expense.Notes
                },
                expense.VisibleRoles,
                ["Owner", "Admin", "Finance"]);
        }
    }

    private static async Task EnsureInventoryDataAsync(
        AppDbContext dbContext,
        IReadOnlyDictionary<string, Module> modules,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Role> roles)
    {
        if (!modules.TryGetValue("Inventory", out var module) ||
            await dbContext.Entries.AnyAsync(x => x.ModuleId == module.Id))
        {
            return;
        }

        var admin = users["maria@bytebridge.local"];
        var items = new[]
        {
            InventorySeed("Dell Latitude 5430", "Laptop", "INV-LAP-001", 1, 5200, 1, "Andrei Stan", "Dell Partner", "Primary backend laptop."),
            InventorySeed("Lenovo ThinkPad T14", "Laptop", "INV-LAP-002", 1, 4900, 1, "Ioana Radu", "Lenovo Partner", "Frontend and QA workstation."),
            InventorySeed("MacBook Pro 14", "Laptop", "INV-LAP-003", 1, 8200, 1, "Elena Marin", "iStyle Business", "Design laptop for Figma and demos."),
            InventorySeed("Dell 27 Monitor", "Monitor", "INV-MON-001", 3, 980, 1, "", "eMAG Business", "Shared 27 inch office displays."),
            InventorySeed("Logitech MX Master", "Peripheral", "INV-PRF-001", 4, 420, 2, "", "Logitech", "Preferred mouse for delivery team."),
            InventorySeed("Ubiquiti Access Point", "Network", "INV-NET-001", 2, 650, 1, "", "Ubiquiti", "Office Wi-Fi upgrades."),
            InventorySeed("Windows Pro License", "License", "INV-LIC-001", 6, 780, 2, "", "Microsoft CSP", "Spare activation keys."),
            InventorySeed("Adobe CC Seat", "License", "INV-LIC-002", 2, 310, 1, "Elena Marin", "Adobe", "Design subscription seats."),
            InventorySeed("Raspberry Pi Test Node", "Other", "INV-OTH-001", 2, 410, 1, "", "PiShop", "Edge telemetry QA devices."),
            InventorySeed("Dell USB-C Dock", "Peripheral", "INV-PRF-002", 3, 690, 1, "", "Dell Partner", "Shared docking stations."),
            InventorySeed("Samsung Galaxy Test Phone", "Device", "INV-DEV-001", 2, 2300, 1, "Ioana Radu", "Orange Business", "Android app QA and booking flow tests."),
            InventorySeed("Zebra Label Printer", "Peripheral", "INV-PRF-003", 1, 1450, 1, "", "Zebra", "Used for inventory tagging and warehouse demos."),
            InventorySeed("MikroTik Router", "Network", "INV-NET-002", 1, 890, 1, "", "MikroTik", "Lab router for VPN and staging network tests.")
        };

        var baseDate = new DateTime(2026, 6, 3, 10, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            await AddEntryWithVisibilityAsync(
                dbContext,
                module,
                admin,
                roles,
                ScaleDemoDate(baseDate.AddDays(i)),
                new Dictionary<string, object?>
                {
                    ["itemName"] = item.ItemName,
                    ["category"] = item.Category,
                    ["sku"] = item.Sku,
                    ["quantity"] = item.Quantity,
                    ["unitCost"] = item.UnitCost,
                    ["reorderLevel"] = item.ReorderLevel,
                    ["assignedTo"] = item.AssignedTo,
                    ["supplier"] = item.Supplier,
                    ["notes"] = item.Notes
                },
                ["Owner", "Admin", "Finance"],
                ["Owner", "Admin"]);
        }
    }

    private static async Task EnsureCalendarDataAsync(
        AppDbContext dbContext,
        IReadOnlyDictionary<string, Module> modules,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Role> roles)
    {
        if (!modules.TryGetValue("Calendar", out var module) ||
            await dbContext.Entries.AnyAsync(x => x.ModuleId == module.Id))
        {
            return;
        }

        var manager = users["vlad@bytebridge.local"];
        var sales = users["radu@bytebridge.local"];
        var developer = users["andrei@bytebridge.local"];
        var finance = users["cristina@bytebridge.local"];

        var events = new[]
        {
            CalendarSeed("Nova Retail sprint planning", SeedDate(2026, 6, 3, 9, 30, 0), SeedDate(2026, 6, 3, 10, 30, 0), "Vlad Georgescu", "Nova Retail Dashboard", "Sprint planning with delivery team.", manager, ["Owner", "Admin", "Project Manager", "Developer", "Designer"]),
            CalendarSeed("GreenFarm API workshop", SeedDate(2026, 6, 5, 11, 0, 0), SeedDate(2026, 6, 5, 12, 30, 0), "Andrei Stan", "GreenFarm Fleet Tracker", "Review telemetry API contracts.", developer, ["Owner", "Admin", "Project Manager", "Developer"]),
            CalendarSeed("MedCore discovery call", SeedDate(2026, 6, 9, 14, 0, 0), SeedDate(2026, 6, 9, 15, 0, 0), "Vlad Georgescu", "MedCore Appointment Portal", "Requirements clarification with clinic ops.", manager, ["Owner", "Admin", "Project Manager", "Sales"]),
            CalendarSeed("UrbanFit proposal review", SeedDate(2026, 6, 12, 13, 0, 0), SeedDate(2026, 6, 12, 14, 0, 0), "Radu Matei", "UrbanFit Mobile Booking", "Internal proposal sign-off before send.", sales, ["Owner", "Admin", "Project Manager", "Sales", "Designer"]),
            CalendarSeed("Atlas handoff retrospective", SeedDate(2026, 6, 16, 16, 0, 0), SeedDate(2026, 6, 16, 17, 0, 0), "Vlad Georgescu", "Atlas Invoice Automation", "Review completed project and reuse notes.", manager, ["Owner", "Admin", "Project Manager", "Developer", "Finance"]),
            CalendarSeed("Finance margin review", SeedDate(2026, 6, 18, 10, 0, 0), SeedDate(2026, 6, 18, 11, 0, 0), "Cristina Pavel", "Operations", "Mid-cycle margin and forecast review.", finance, ["Owner", "Admin", "Finance"]),
            CalendarSeed("Client demo rehearsal", SeedDate(2026, 6, 20, 15, 0, 0), SeedDate(2026, 6, 20, 16, 0, 0), "Elena Marin", "MedCore Appointment Portal", "Dry run before the client walkthrough.", users["elena@bytebridge.local"], ["Owner", "Admin", "Project Manager", "Designer", "Sales"]),
            CalendarSeed("Maintenance retainer call", SeedDate(2026, 6, 23, 10, 0, 0), SeedDate(2026, 6, 23, 10, 45, 0), "Radu Matei", "Nova Retail Dashboard", "Discuss Q3 support scope.", sales, ["Owner", "Admin", "Sales", "Project Manager"]),
            CalendarSeed("Release checklist review", SeedDate(2026, 6, 26, 11, 0, 0), SeedDate(2026, 6, 26, 12, 0, 0), "Ioana Radu", "Nova Retail Dashboard", "Go-live checklist and QA sign-off.", users["ioana@bytebridge.local"], ["Owner", "Admin", "Project Manager", "Developer"]),
            CalendarSeed("Pipeline sync", SeedDate(2026, 6, 30, 9, 0, 0), SeedDate(2026, 6, 30, 9, 45, 0), "Radu Matei", "Sales", "Review open leads and proposal timelines.", sales, ["Owner", "Admin", "Sales"]),
            CalendarSeed("July planning board", SeedDate(2026, 7, 2, 10, 0, 0), SeedDate(2026, 7, 2, 11, 30, 0), "Vlad Georgescu", "Operations", "Cross-team planning for July capacity.", manager, ["Owner", "Admin", "Project Manager", "Developer", "Designer", "Sales", "Finance"]),
            CalendarSeed("BrightSchool intro call", SeedDate(2026, 7, 5, 13, 0, 0), SeedDate(2026, 7, 5, 13, 45, 0), "Radu Matei", "BrightSchool Online", "Discovery for analytics dashboard lead.", sales, ["Owner", "Admin", "Sales", "Viewer"]),
            CalendarSeed("Northstar legal discovery", SeedDate(2026, 7, 7, 10, 0, 0), SeedDate(2026, 7, 7, 11, 0, 0), "Vlad Georgescu", "Northstar Contract Hub", "Map approval flow and compliance checkpoints.", manager, ["Owner", "Admin", "Project Manager", "Sales"]),
            CalendarSeed("BlueWave field ops kickoff", SeedDate(2026, 7, 9, 12, 0, 0), SeedDate(2026, 7, 9, 13, 30, 0), "Andrei Stan", "BlueWave Field Operations", "Kickoff for field dispatch and SLA dashboard.", developer, ["Owner", "Admin", "Project Manager", "Developer", "Sales"]),
            CalendarSeed("Support handoff review", SeedDate(2026, 7, 11, 14, 30, 0), SeedDate(2026, 7, 11, 15, 15, 0), "Paul Dumitrescu", "Operations", "Align support runbook for new client launches.", users["maria@bytebridge.local"], ["Owner", "Admin", "Project Manager", "Viewer"])
        };

        foreach (var item in events)
        {
            await AddEntryWithVisibilityAsync(
                dbContext,
                module,
                item.Creator,
                roles,
                item.Start,
                new Dictionary<string, object?>
                {
                    ["title"] = item.Title,
                    ["startTimestamp"] = item.Start,
                    ["endTimestamp"] = item.End,
                    ["assignedTo"] = item.AssignedTo,
                    ["project"] = item.Project,
                    ["notes"] = item.Notes
                },
                item.VisibleRoles,
                item.VisibleRoles.Where(x => x is "Owner" or "Admin" or "Project Manager" or "Developer" or "Designer" or "Sales" or "Finance").ToArray());
        }
    }

    private static async Task EnsureDashboardDemoAsync(
        AppDbContext dbContext,
        IReadOnlyDictionary<string, Module> modules)
    {
        if (!await dbContext.Visualizations.AnyAsync())
        {
            var visualizations = new List<Visualization>();

            if (modules.TryGetValue("Sales", out var salesModule))
            {
                visualizations.Add(new Visualization
                {
                    Id = Guid.NewGuid(),
                    ModuleId = salesModule.Id,
                    Title = "Monthly Revenue",
                    FieldName = "amount",
                    ChartType = "line",
                    WidgetSize = "medium",
                    AggregationType = "sum",
                    DateRange = "year",
                    SummaryMetric = "sum",
                    CreatedAt = DateTime.UtcNow
                });
                visualizations.Add(new Visualization
                {
                    Id = Guid.NewGuid(),
                    ModuleId = salesModule.Id,
                    Title = "Sales Pipeline",
                    FieldName = "status",
                    ChartType = "bar",
                    WidgetSize = "medium",
                    AggregationType = "count",
                    DateRange = "year",
                    SummaryMetric = "count",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (modules.TryGetValue("Expenses", out var expensesModule))
            {
                visualizations.Add(new Visualization
                {
                    Id = Guid.NewGuid(),
                    ModuleId = expensesModule.Id,
                    Title = "Monthly Expenses",
                    FieldName = "amount",
                    ChartType = "bar",
                    WidgetSize = "medium",
                    AggregationType = "sum",
                    DateRange = "year",
                    SummaryMetric = "sum",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (modules.TryGetValue("Tasks", out var tasksModule))
            {
                visualizations.Add(new Visualization
                {
                    Id = Guid.NewGuid(),
                    ModuleId = tasksModule.Id,
                    Title = "Tasks by Status",
                    FieldName = "status",
                    ChartType = "pie",
                    WidgetSize = "medium",
                    AggregationType = "count",
                    DateRange = "year",
                    SummaryMetric = "count",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (visualizations.Count > 0)
            {
                await dbContext.Visualizations.AddRangeAsync(visualizations);
                await dbContext.SaveChangesAsync();
            }
        }

        if (!await dbContext.DashboardWidgets.AnyAsync())
        {
            var visualizations = await dbContext.Visualizations
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Title, StringComparer.OrdinalIgnoreCase);
            var widgets = new List<DashboardWidget>();
            var now = DateTime.UtcNow;
            var position = 0;

            void AddVisualizationWidget(string title)
            {
                if (!visualizations.TryGetValue(title, out var vis))
                {
                    return;
                }

                widgets.Add(new DashboardWidget
                {
                    Id = Guid.NewGuid(),
                    Type = "visualization",
                    Title = title,
                    VisualizationId = vis.Id,
                    Size = "medium",
                    Width = 6,
                    Height = 3,
                    Position = position++,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            AddVisualizationWidget("Monthly Revenue");
            AddVisualizationWidget("Monthly Expenses");
            AddVisualizationWidget("Tasks by Status");
            AddVisualizationWidget("Sales Pipeline");

            if (modules.TryGetValue("Calendar", out var calendarModule))
            {
                widgets.Add(new DashboardWidget
                {
                    Id = Guid.NewGuid(),
                    Type = "calendar_summary",
                    Title = "Upcoming Meetings",
                    ModuleId = calendarModule.Id,
                    Size = "medium",
                    Width = 6,
                    Height = 3,
                    Position = position++,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            if (modules.TryGetValue("Projects", out var projectsModule))
            {
                widgets.Add(new DashboardWidget
                {
                    Id = Guid.NewGuid(),
                    Type = "tracker_summary",
                    Title = "Active Projects",
                    ModuleId = projectsModule.Id,
                    Size = "medium",
                    Width = 6,
                    Height = 3,
                    Position = position++,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            if (modules.TryGetValue("Inventory", out var inventoryModule))
            {
                widgets.Add(new DashboardWidget
                {
                    Id = Guid.NewGuid(),
                    Type = "tracker_summary",
                    Title = "Low Inventory Items",
                    ModuleId = inventoryModule.Id,
                    Size = "medium",
                    Width = 6,
                    Height = 3,
                    Position = position++,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            widgets.Add(new DashboardWidget
            {
                Id = Guid.NewGuid(),
                Type = "recent_activity",
                Title = "Recent Activity",
                Size = "full-width",
                Width = 12,
                Height = 4,
                Position = position,
                CreatedAt = now,
                UpdatedAt = now
            });

            await dbContext.DashboardWidgets.AddRangeAsync(widgets);
        }
    }

    private static async Task AddEntryWithVisibilityAsync(
        AppDbContext dbContext,
        Module module,
        User creator,
        IReadOnlyDictionary<string, Role> roles,
        DateTime timestamp,
        Dictionary<string, object?> data,
        IEnumerable<string> visibleRoles,
        IEnumerable<string> editableRoles)
    {
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            ModuleId = module.Id,
            CreatedByUserId = creator.Id,
            Timestamp = timestamp,
            Data = JsonSerializer.SerializeToElement(data),
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        await dbContext.Entries.AddAsync(entry);

        var visible = new HashSet<string>(visibleRoles, StringComparer.OrdinalIgnoreCase);
        var editable = new HashSet<string>(editableRoles, StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in AllDemoRoleNames)
        {
            if (!roles.TryGetValue(roleName, out var role))
            {
                continue;
            }

            var canView = roleName is "Owner" or "Admin" || visible.Contains(roleName);
            var canEdit = roleName is "Owner" or "Admin" || (canView && editable.Contains(roleName));

            await dbContext.EntryVisibilityPermissions.AddAsync(new EntryVisibilityPermission
            {
                Id = Guid.NewGuid(),
                EntryId = entry.Id,
                OwnerUserId = creator.Id,
                RoleId = role.Id,
                CanView = canView,
                CanEdit = canEdit
            });
        }
    }

    private sealed record TaskSeedData(
        string Title,
        string Status,
        string Priority,
        string AssignedTo,
        string Project,
        DateTime DueTimestamp,
        int EstimatedHours,
        int ActualHours,
        string Notes,
        User Creator,
        string[] VisibleRoles);

    private sealed record SaleSeedData(
        string Client,
        string Project,
        decimal Amount,
        string Status,
        DateTime Timestamp,
        string Owner,
        string Notes);

    private sealed record ExpenseSeedData(
        string Category,
        decimal Amount,
        string Vendor,
        DateTime Timestamp,
        string Project,
        string Notes,
        string[] VisibleRoles);

    private sealed record InventorySeedData(
        string ItemName,
        string Category,
        string Sku,
        int Quantity,
        decimal UnitCost,
        int ReorderLevel,
        string AssignedTo,
        string Supplier,
        string Notes);

    private sealed record CalendarSeedData(
        string Title,
        DateTime Start,
        DateTime End,
        string AssignedTo,
        string Project,
        string Notes,
        User Creator,
        string[] VisibleRoles);

    private static TaskSeedData TaskSeed(
        string title,
        string status,
        string priority,
        string assignedTo,
        string project,
        DateTime dueTimestamp,
        int estimatedHours,
        int actualHours,
        string notes,
        User creator,
        string[] visibleRoles) =>
        new(title, status, priority, assignedTo, project, dueTimestamp, estimatedHours, actualHours, notes, creator, visibleRoles);

    private static SaleSeedData SaleSeed(
        string client,
        string project,
        decimal amount,
        string status,
        DateTime timestamp,
        string owner,
        string notes) =>
        new(client, project, amount, status, timestamp, owner, notes);

    private static ExpenseSeedData ExpenseSeed(
        string category,
        decimal amount,
        string vendor,
        DateTime timestamp,
        string project,
        string notes,
        string[] visibleRoles) =>
        new(category, amount, vendor, timestamp, project, notes, visibleRoles);

    private static InventorySeedData InventorySeed(
        string itemName,
        string category,
        string sku,
        int quantity,
        decimal unitCost,
        int reorderLevel,
        string assignedTo,
        string supplier,
        string notes) =>
        new(itemName, category, sku, quantity, unitCost, reorderLevel, assignedTo, supplier, notes);

    private static CalendarSeedData CalendarSeed(
        string title,
        DateTime start,
        DateTime end,
        string assignedTo,
        string project,
        string notes,
        User creator,
        string[] visibleRoles) =>
        new(title, start, end, assignedTo, project, notes, creator, visibleRoles);

    private static DateTime SeedDate(int year, int month, int day, int hour, int minute, int second) =>
        ScaleDemoDate(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc));

    private static DateTime ScaleDemoDate(DateTime sourceDate)
    {
        var normalized = sourceDate.Kind == DateTimeKind.Utc
            ? sourceDate
            : DateTime.SpecifyKind(sourceDate, DateTimeKind.Utc);

        var sourceSpan = DemoTimelineSourceEnd - DemoTimelineSourceStart;
        var targetSpan = DemoTimelineTargetEnd - DemoTimelineTargetStart;
        if (sourceSpan <= TimeSpan.Zero || targetSpan <= TimeSpan.Zero)
        {
            return normalized;
        }

        var offset = normalized - DemoTimelineSourceStart;
        var ratio = Math.Clamp(offset.TotalSeconds / sourceSpan.TotalSeconds, 0d, 1d);
        return DemoTimelineTargetStart.AddSeconds(targetSpan.TotalSeconds * ratio);
    }
}
