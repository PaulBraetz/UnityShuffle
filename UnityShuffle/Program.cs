using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PBApp.Configuration;
using PBServer.Configuration;
using UnityShuffle.Data;
using UnityShuffle.Data.Mappings;
using UnityShuffle.Events;
using UnityShuffle.Services.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var configuration = builder.Configuration;

builder.Services.ConfigurePBApp(ca =>
{
	ca.ConfigurePBCommon(cc =>
		{
			cc.ConfigureSettingsInitializer(si =>
			{
				si.Url = configuration.GetSection("SetupData").GetSection("URL").GetValue<String>("Production");
#if DEBUG
				si.Url = configuration.GetSection("SetupData").GetSection("URL").GetValue<String>("Development");
#endif

			});
		})
		.ConfigurePBDataAccess(cda =>
		{
			cda.ConfigureSettingsInitializer(si =>
			{

			});
			cda.UseMappingConfiguration(m => m.AddFromAssemblyOf<Program>());
			String connectionString = configuration.GetConnectionString("Production");
#if DEBUG
			Console.WriteLine("Use Production Connection? (Y)");
			if (!(Console.ReadLine()?.Equals("Y") ?? false))
			{
				connectionString = configuration.GetConnectionString("Development");
			}
#endif

			cda.UseConnectionString(connectionString);
			if (configuration.GetSection("SetupData").GetValue<Boolean>("WipeDB"))
			{
				cda.DoWipe();
			}
#if DEBUG
			else
			{
				Console.WriteLine("Wipe Database? (Y)");
				if (Console.ReadLine()?.Equals("Y") ?? false)
				{
					cda.DoWipe();
				}
			}
#endif
			if (configuration.GetSection("SetupData").GetValue<Boolean>("UpdateDB"))
			{
				cda.DoUpdate();
			}
#if DEBUG
			else
			{
				Console.WriteLine("Update Database? (Y)");
				if (Console.ReadLine()?.Equals("Y") ?? false)
				{
					cda.DoUpdate();
				}
			}
#endif
		})
		.ConfigurePBApplication(ca =>
		{
			ca.ConfigureSettingsInitializer(si =>
			{

			});
			ca.SetChangeNotifierFactoryImplementation<NotifierFactory>();
			IConfigurationSection adminData = configuration.GetSection("SuperAdminData");
			ca.CreateSuperAdmin(adminData.GetValue<String>("Name"), adminData.GetValue<String>("Email"), adminData.GetValue<String>("Password"));
		})
		.ConfigurePBShared(cs =>
		{
			cs.ConfigureSettingsInitializer(si =>
			{

			});
		})
		.ConfigurePBServer(cs =>
		{
			cs.ConfigureSettingsInitializer(si =>
			{
				IConfigurationSection? emailSection = configuration.GetSection("NoreplyEmailConfig");
				si.NoreplyPop3Configuration = new EmailConfiguration(emailSection.GetSection("Pop3"));
				si.NoreplySmtpConfiguration = new EmailConfiguration(emailSection.GetSection("Smtp"));
				si.UseEmailService = true;
			});

			cs.SetDBConnectedServiceContextImplementation<ServiceContext>()
				.SetUseDefaultControllers(false);
		})
		.ConfigurePBFrontend(cf =>
		{
			cf.ConfigureSettingsInitializer(si =>
			{

			});
		});
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
