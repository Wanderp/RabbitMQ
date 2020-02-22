using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using HealthChecks.UI.Client;

namespace RabbitMq.Test.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitMQConfigurations = new RabbitMqConfigurations();
            new ConfigureFromConfigurationOptions<RabbitMqConfigurations>(
                Configuration.GetSection("RabbitMQConfigurations"))
                    .Configure(rabbitMQConfigurations);
            services.AddSingleton(rabbitMQConfigurations);

            services.AddHealthChecks()
                .AddRabbitMQ(Configuration.GetConnectionString("RabbitMQ"), name: "rabbitMQ");

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapControllers());

            app.UseHealthChecks("/healthchecks-data-ui", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        }
    }
}