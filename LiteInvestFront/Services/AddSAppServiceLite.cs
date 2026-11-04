
using LiteInvest.Entity.ServerEntity;
using LiteInvestServerDll;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics;

namespace LiteInvestFront.Services
{
    public static class AddSAppServiceLite
    {


        public static JwtOptions? Jwtoptions;

        //public static JwtProvider? Jwtprovider;

        /// <summary>
        /// LiteInvest установка, настройка расширения Swagger 
        /// </summary>
        /// <param name="builder"></param>
        public static void AddSwaggerServiceLite(this WebApplicationBuilder builder)
        {
           
            builder.Services.AddSwaggerGen(option =>
            {
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                option.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
                option.EnableAnnotations();
                

                option.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Scheme = "bearer"
                });

                option.AddSecurityRequirement(new()
                    {

                        {new OpenApiSecurityScheme
                            {Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "bearer"
                                }
                            }, []
                        }
                    });
                option.OperationFilter<AddAuthHeaderOperationFilter>();

            });
        }

        public class AddAuthHeaderOperationFilter : IOperationFilter
        {

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                if (operation.Security == null)
                    operation.Security = new List<OpenApiSecurityRequirement>();


                var scheme = new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearer" } };
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [scheme] = new List<string>()
                });
            }
        }

        public static void AddAutentificationServiceLite(this IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = JwtProvider.GetBasicTokenValidationParameters(Jwtoptions);
                    options.IncludeErrorDetails = true;
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = (context) =>
                        {
                            

                            //и если чисто просто запрос, то можем и из хедера взять..
                            string? token = context.Request.Headers["Authorization"];
                            token = token?.Replace("Bearer ", "");
                            string ip_address = context.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";       // проверка IP адреса
                            if (string.IsNullOrWhiteSpace(token))
                            {

                                string message = $"JWT is null ip={ip_address} ";
                                context.Fail(message);
                             //   Console.WriteLine(message);
                                return Task.CompletedTask;
                            }
                            ServiceDB? serviceDB = context.HttpContext.RequestServices.GetService<ServiceDB>();
                            System.Collections.Concurrent.ConcurrentDictionary<string, User>? users = serviceDB?.GetUsersContext();
                            if (users == null)
                            {
                                string message = "Users load error";
                                context.Fail(message);
                                Console.WriteLine(message);
                                
                                return Task.CompletedTask;
                            }
                            var claims = JwtProvider.getClaimFromJwt(token);
                            if (claims == null)
                            {
                                string message = "Claims token error";
                                context.Fail(message);
                                Console.WriteLine(message);
                                
                                return Task.CompletedTask;
                            }
                            var claimLogin = claims.FirstOrDefault(j => j.Type.EndsWith("sub"));    // ID пользователя, согласно требованиям к структуре токенов Лайт
                            if (claimLogin?.Value == null || !users.ContainsKey(claimLogin.Value) || !users.TryGetValue(claimLogin.Value, out User? user))
                            {
                                string message = $"ID User (sub) claims token error";
                                context.Fail(message);
                                Console.WriteLine(message);
                                
                                return Task.CompletedTask;
                            }
                            if (user.AccessToken != token)
                            {
                                string message = $"{user.Name} Access token error";
                                context.Fail(message);
                                Console.WriteLine(message);

                                return Task.CompletedTask;
                            }
                            
                            if (user.BindIP && user.IPAddress != ip_address)
                            {
                                string message = $" {user.Name} User IP-address changed. Need ip={user.IPAddress}, his ip={ip_address}";
                                context.Fail(message);
                                Console.WriteLine(message);
                                
                                return Task.CompletedTask;
                            }
                            context.Request.Headers.TryGetValue("User-Agent", out var userAgent);
                            if (user.UserAgentBrowser != userAgent.First() && !user.Admin)
                            {
                                string message = $" {user.Name} User-Agent browser changed.";
                                context.Fail(message);
                                Console.WriteLine(message);
                                
                                return Task.CompletedTask;
                            }

                            user.TimeLastLogin = DateTime.Now;
                            
                            return Task.CompletedTask;
                        }
                    };
                
                });
            services.AddAuthorization();
        }



        /// <summary>
        /// LiteInvest установка, настройка JWT 
        /// </summary>
        /// <param name="builder"></param>
        public static void AddJWTAutorizationServiceLite(this WebApplicationBuilder builder)
        {
            Jwtoptions = builder.Configuration.GetSection("JwtOptions").Get<JwtOptions>();
            //Jwtprovider = new JwtProvider(Jwtoptions);
            JwtProvider.jwtoptions = Jwtoptions;
            

        }


        public static void MapSwaggerAppLite(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(_ =>
            {
                _.EnableTryItOutByDefault();
                _.DisplayRequestDuration();
                _.EnablePersistAuthorization();
                _.SwaggerEndpoint("/swagger/v1/swagger.json", "v.1.0");
                _.RoutePrefix = "swagger";
            });
        }

    }
}
