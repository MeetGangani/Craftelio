using Craftelio.Models;
using Craftelio.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Craftelio.DataAccess.DbInitializer
{
	public class DbInitializer : IDbInitializer
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly ApplicationDbContext _db;

		public DbInitializer(
			UserManager<IdentityUser> userManager,
			RoleManager<IdentityRole> roleManager,
			ApplicationDbContext db)
		{
			_roleManager = roleManager;
			_userManager = userManager;
			_db = db;
		}

		public void Initialize()
		{
			throw new NotImplementedException();
		}

		public async Task InitializeAsync()
		{
			try
			{
				if (_db.Database.GetPendingMigrations().Any())
				{
					await _db.Database.MigrateAsync();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An error occurred while applying migrations: {ex.Message}");
			}

			if (!await _roleManager.RoleExistsAsync(SD.Role_Admin))
			{
				await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
				await _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee));
				await _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Indi));
				await _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Comp));

				var adminUser = new ApplicationUser
				{
					UserName = "admin@craftelio.com",
					Email = "admin@craftelio.com",
					Name = "Meet Gangani",
					PhoneNumber = "9601810456",
					StreetAddress = "test 123 Ave",
					State = "gj",
					PostalCode = "23422",
					City = "Surat"
				};

				var result = await _userManager.CreateAsync(adminUser, "Admin123*");
				if (result.Succeeded)
				{
					await _userManager.AddToRoleAsync(adminUser, SD.Role_Admin);
					Console.WriteLine("Admin user created successfully.");
				}
				else
				{
					Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
				}
			}
		}
	}
}