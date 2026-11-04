using Binance.Net.Objects.Models.Spot.ConvertTransfer;


using LiteInvest.Entity.ServerEntity;

using LiteInvestFront.Services;

using LiteInvestServerDll;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Net;

using static LiteInvestFront.Controller.AuthenticationController;

namespace LiteInvestFront.Controller
{
    /// <summary>
    /// JWT Token
    /// </summary>
    [ApiController]
    [Tags("Login")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class AuthenticationController : ControllerBase
    {
        /// <summary>
        /// Получить access и refresh токены авторизации администратора
        /// </summary>
        /// <returns></returns>
        [HttpPost]                                           
        [Route("/api/auth/login")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public IActionResult GetAuthentication(UserLogin userCredentials)
        {
            try
            {
                if (users == null || !users.TryGetValue(userCredentials.login, out User? user) || user?.Password != userCredentials.password)
                {
                    return new UnauthorizedResult();
                }
                string ip_address = HttpContext.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                if (userCredentials.bind_ip && string.IsNullOrEmpty(ip_address))
                {
                    return new BadRequestObjectResult($"Error IP Address");
                }
                var accessToken = JwtProvider.GenerateAccessToken(user);
                var refreshToken = JwtProvider.GenerateRefreshToken();
                user.BindIP = userCredentials.bind_ip;
                user.IPAddress = ip_address;
                user.TimeRefreshToken = DateTime.Now;
                user.RefreshToken = refreshToken;
                
                HttpContext.Request.Headers.TryGetValue("User-Agent", out var userAgent);
                user.UserAgentBrowser = userAgent;

                var t = new AuthLoginResponse()
                {
                    access_token = accessToken,
                    refresh_token = refreshToken,
                    expires_in = JwtProvider.jwtoptions.ExpireMinuts,
                    ip_address = ip_address
                };

                return new JsonResult(t);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Проверить валидность access токена
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("/api/auth/check")]
        //[Produces("application/json")]
        //[Consumes("application/json")]
        [Authorize]
        public IActionResult CheckAccess()
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                return new JsonResult("OK");
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }



        /// <summary>
        /// Обновить  access токен по данным refresh токена
        /// </summary>
        /// <param name="refreshToken">Refresh token </param>
        /// <returns></returns>
        [HttpPost]                                      
        [Route("/api/auth/refresh")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [Authorize]
        public IActionResult GetRefreshToken(RefreshToken refreshToken, IConfiguration configuration)
        {
            try
            {
                var t = refreshToken.refresh_Token;
                if (string.IsNullOrWhiteSpace(t))
                {
                    return new UnauthorizedResult();
                }
                string ip_address = HttpContext.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                var u = users?.FirstOrDefault(u => u.Value.RefreshToken == refreshToken.refresh_Token);
                if (u?.Key == null || u?.Value == null)
                {
                    return new UnauthorizedResult();
                }
                User user = u?.Value;
                if (user.BindIP && (user.IPAddress != ip_address || string.IsNullOrWhiteSpace(ip_address)))
                {
                    user.AccessToken = "";
                    user.RefreshToken = "";
                    return new UnauthorizedResult();
                }

                int hours = JwtProvider.jwtoptions.LifeTimeRefreshTokenHours;

                if (!user.BindIP && user.TimeRefreshToken.AddHours(hours) < DateTime.Now)    // need update Refresh Token
                {
                    user.AccessToken = "";
                    user.RefreshToken = "";
                    return new UnauthorizedResult();
                }

                var accessToken = JwtProvider.GenerateAccessToken(user);
                var newRefreshToken = JwtProvider.GenerateRefreshToken();
                user.RefreshToken = newRefreshToken;

                var res = new AuthLoginResponse()
                {
                    access_token = accessToken,
                    refresh_token = newRefreshToken,
                    expires_in = JwtProvider.jwtoptions.ExpireMinuts,
                    ip_address = ip_address
                };

                return new JsonResult(res); ;
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Logout
        /// </summary>
        /// <param name="refreshToken">Refresh token </param>
        /// <returns></returns>
        [HttpPost]
        [Route("/api/auth/logout")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public IActionResult Logout(RefreshToken refreshToken)
        {
            try
            {
                var t = refreshToken.refresh_Token;
                if (string.IsNullOrWhiteSpace(t))
                {
                    return new UnauthorizedResult();
                }
                var u = users?.FirstOrDefault(u => u.Value.RefreshToken == refreshToken.refresh_Token);
                if (u?.Key == null || u?.Value == null)
                {
                    return new UnauthorizedResult();
                }
                User user = u?.Value;
                user.AccessToken = "";
                user.RefreshToken = "";
                

                return new JsonResult(new Message { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }



        ServerService serverService;

        ServiceDB serviceDB;

        System.Collections.Concurrent.ConcurrentDictionary<string, LiteInvest.Entity.ServerEntity.User>? users;

        /// <summary>
        /// Dependency Injection 
        /// </summary>
        /// <param name="_serverService"></param>
        /// <param name="_serviceDB"></param>
        public AuthenticationController(ServerService _serverService, ServiceDB _serviceDB)
        {
            serviceDB = _serviceDB;
            serverService = _serverService;
            users = serviceDB.GetUsersContext();
        }

        public record RefreshToken
        {
            /// <summary>
            /// Refresh token
            /// </summary>
            public required string refresh_Token { get; set; }
        }

        public record Message
        { 
            /// <summary>
            /// Message
            /// </summary>
            public required string message { get; set; }

        }


        /// <summary>
        /// Login user authentication
        /// </summary>
        public record UserLogin
        {
            /// <summary>
            /// Login
            /// </summary>
            public required string login { get; set; }

            /// <summary>
            /// Password
            /// </summary>
            public required string password { get; set; }


            /// <summary>
            /// Bind username to IP 
            /// </summary>
            public required bool bind_ip { get; set; }
        }

        /// <summary>
        /// Response user Authentication
        /// </summary>
        public record AuthLoginResponse
        {
            /// <summary>
            /// Access Token
            /// </summary>
            public required string access_token { get; set; }

            /// <summary>
            /// Refresh Token
            /// </summary>
            public required string refresh_token { get; set; }

            /// <summary>
            /// Expires In
            /// </summary>
            public required int expires_in { get; set; }

            /// <summary>
            /// IP Address 
            /// </summary>
            public required string ip_address { get; set; } 
        }
    }
    
}
