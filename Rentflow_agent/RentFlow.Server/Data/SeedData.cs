using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentFlow.Shared.Models;

namespace RentFlow.Server.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            if (await context.Users.AnyAsync())
                return;

            var hasher = new PasswordHasher<User>();
            var password = "RentFlow@2024";

            var admin = new User { FullName = "Platform Admin", Email = "admin@rentflow.io", Role = "Admin" };
            admin.PasswordHash = hasher.HashPassword(admin, password);

            var landlord1 = new User { FullName = "Ahmed Raza", Email = "ahmed.landlord@rentflow.io", Role = "Landlord" };
            landlord1.PasswordHash = hasher.HashPassword(landlord1, password);

            var landlord2 = new User { FullName = "Sara Malik", Email = "sara.landlord@rentflow.io", Role = "Landlord" };
            landlord2.PasswordHash = hasher.HashPassword(landlord2, password);

            var tenant1 = new User { FullName = "Ali Hassan", Email = "ali.tenant@rentflow.io", Role = "Tenant" };
            tenant1.PasswordHash = hasher.HashPassword(tenant1, password);

            var tenant2 = new User { FullName = "Fatima Noor", Email = "fatima.tenant@rentflow.io", Role = "Tenant" };
            tenant2.PasswordHash = hasher.HashPassword(tenant2, password);

            var tenant3 = new User { FullName = "Bilal Khan", Email = "bilal.tenant@rentflow.io", Role = "Tenant" };
            tenant3.PasswordHash = hasher.HashPassword(tenant3, password);

            var tenant4 = new User { FullName = "Zara Siddiqui", Email = "zara.tenant@rentflow.io", Role = "Tenant" };
            tenant4.PasswordHash = hasher.HashPassword(tenant4, password);

            context.Users.AddRange(admin, landlord1, landlord2, tenant1, tenant2, tenant3, tenant4);
            await context.SaveChangesAsync();

            var prop1 = new Property { LandlordId = landlord1.Id, Name = "Gulberg Heights", Address = "12-B, Gulberg III", City = "Lahore", TotalUnits = 4, Latitude = 31.5204, Longitude = 74.3587 };
            var prop2 = new Property { LandlordId = landlord1.Id, Name = "Blue Area Plaza", Address = "Plot 7, Blue Area", City = "Islamabad", TotalUnits = 3, Latitude = 33.7104, Longitude = 73.0645 };
            var prop3 = new Property { LandlordId = landlord2.Id, Name = "Clifton Residency", Address = "House 22, Clifton Block 4", City = "Karachi", TotalUnits = 2, Latitude = 24.8138, Longitude = 67.0310 };

            context.Properties.AddRange(prop1, prop2, prop3);
            await context.SaveChangesAsync();

            var units = new[]
            {
                new Unit { PropertyId = prop1.Id, UnitNumber = "101", MonthlyRent = 50000, Bedrooms = 2, IsOccupied = true },
                new Unit { PropertyId = prop1.Id, UnitNumber = "102", MonthlyRent = 45000, Bedrooms = 1, IsOccupied = true },
                new Unit { PropertyId = prop2.Id, UnitNumber = "A1", MonthlyRent = 60000, Bedrooms = 2, IsOccupied = true },
                new Unit { PropertyId = prop2.Id, UnitNumber = "A2", MonthlyRent = 55000, Bedrooms = 1, IsOccupied = true },
                new Unit { PropertyId = prop3.Id, UnitNumber = "G1", MonthlyRent = 70000, Bedrooms = 3, IsOccupied = false },
                new Unit { PropertyId = prop3.Id, UnitNumber = "G2", MonthlyRent = 75000, Bedrooms = 3, IsOccupied = false }
            };

            context.Units.AddRange(units);
            await context.SaveChangesAsync();

            var leases = new[]
            {
                new Lease { UnitId = units[0].Id, TenantId = tenant1.Id, StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), MonthlyRent = units[0].MonthlyRent },
                new Lease { UnitId = units[1].Id, TenantId = tenant2.Id, StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), MonthlyRent = units[1].MonthlyRent },
                new Lease { UnitId = units[2].Id, TenantId = tenant3.Id, StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), MonthlyRent = units[2].MonthlyRent },
                new Lease { UnitId = units[3].Id, TenantId = tenant4.Id, StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), MonthlyRent = units[3].MonthlyRent }
            };

            context.Leases.AddRange(leases);
            await context.SaveChangesAsync();

            // Create 4 months of payments for each lease (Jan-Apr 2024)
            foreach (var lease in leases)
            {
                for (int month = 1; month <= 4; month++)
                {
                    var dueDate = new DateTime(2024, month, 1, 0, 0, 0, DateTimeKind.Utc);
                    var status = month == 4 ? "Late" : "Paid";
                    var paidDate = month == 4 ? (DateTime?)null : dueDate.AddDays(2);
                    var lateFee = month == 4 ? lease.MonthlyRent * 0.05m : 0;

                    context.Payments.Add(new Payment
                    {
                        LeaseId = lease.Id,
                        TenantId = lease.TenantId,
                        DueDate = dueDate,
                        PaidDate = paidDate,
                        Amount = lease.MonthlyRent,
                        LateFee = lateFee,
                        Status = status
                    });
                }
            }

            await context.SaveChangesAsync();

            var tickets = new[]
            {
                new MaintenanceTicket { TenantId = tenant1.Id, PropertyId = prop1.Id, UnitId = units[0].Id, Category = "Plumbing", Description = "Sink is leaking", Status = "Open", Urgency = 2 },
                new MaintenanceTicket { TenantId = tenant2.Id, PropertyId = prop1.Id, UnitId = units[1].Id, Category = "Electrical", Description = "Lights flickering", Status = "Open", Urgency = 1 },
                new MaintenanceTicket { TenantId = tenant3.Id, PropertyId = prop2.Id, UnitId = units[2].Id, Category = "HVAC", Description = "AC not cooling", Status = "InProgress", AssignedTo = "Ali Tech", Urgency = 2 },
                new MaintenanceTicket { TenantId = tenant4.Id, PropertyId = prop2.Id, UnitId = units[3].Id, Category = "Other", Description = "Door handle broken", Status = "Resolved", Urgency = 1 }
            };

            context.MaintenanceTickets.AddRange(tickets);
            await context.SaveChangesAsync();

            var alerts = new[]
            {
                new WeatherAlertLog { PropertyId = prop1.Id, AlertType = "Freeze", Message = "Freezing temps detected. Drip faucets and insulate pipes." },
                new WeatherAlertLog { PropertyId = prop2.Id, AlertType = "Storm", Message = "Storm warning. Secure outdoor furniture." },
                new WeatherAlertLog { PropertyId = prop3.Id, AlertType = "Heatwave", Message = "Extreme heat. Check HVAC units and ventilation." }
            };

            context.WeatherAlertLogs.AddRange(alerts);
            await context.SaveChangesAsync();
        }
    }
}
