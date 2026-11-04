using CryptoExchange.Net.Interfaces;


using LiteInvest.Entity.ServerEntity;

using LiteInvestServerDll;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Diagnostics.Eventing.Reader;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Claims;

using static LiteInvestFront.Controller.AuthenticationController;
using static LiteInvestFront.Controller.UserController;

namespace LiteInvestFront.Controller
{

    /// <summary>
    /// Контроллер для методов трейдера
    /// </summary>
    [ApiController]
    [Tags("Users")]
    public class UserController : ControllerBase
    {

        /// <summary>
        /// Все трейдеры или один
        /// </summary>
        /// <param name="login">Логин трейдера, не обязательный параметр</param>
        /// <param name="pageNumber">Номер страницы c 1, не обязательный параметр</param>
        /// <param name="pageSize">Размер страницы, не обязательный параметр</param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/users")]
        [Produces("application/json")]
        [Authorize]
        public IActionResult GetUser(string? login, int? pageNumber,  int? pageSize)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                var allU = users?.Values.Where(u => string.IsNullOrWhiteSpace(login) || u.Login == login).ToList();

                if ((allU?.Count() ?? 0) == 0)
                {
                    return new JsonResult(new List<User>());
                }
                
                if (pageNumber == null || pageSize == null)
                {
                    return new JsonResult(allU);
                }
                else
                {
                    pageNumber--;   // отсчет страниц 1 начинать.
                    if (pageNumber < 0)
                    {
                        pageNumber = 0;
                    }

                    int pages = (int)Math.Ceiling(allU.Count() / (decimal)pageSize);

                    Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(pages));

                    int index = (pageSize ?? 10) * (pageNumber ?? 1);
                    if (index > allU.Count())
                    {
                        return new JsonResult(new List<User>());
                    }
                    if (index + (pageSize ?? 10) > allU.Count())
                    {
                        return new JsonResult(allU?.GetRange(index, allU.Count() - index));
                    }
                    return new JsonResult(allU?.GetRange(index, pageSize??10));
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Получить acсess токен для трейдера
        /// </summary>
        /// <param name="login">Логин трейдера</param>
        /// <param name="Bind_IP">Признак привязки к IP-адресу</param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/users/accesToken")]
        [Authorize]
        public IActionResult AddUser(string login, bool Bind_IP)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                    return new UnauthorizedResult();

                if (!users.TryGetValue(login, out var newUser))
                {   
                    newUser = new User(login, Guid.NewGuid().ToString());

                    users.TryAdd(login, newUser);
                    newUser.CanInput = false; ;
                    newUser.Admin = false;
                    newUser.Name = login;
                }
                
                string ip_address = HttpContext.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                
                var accessToken = JwtProvider.GenerateAccessToken(newUser);
                var refreshToken = JwtProvider.GenerateRefreshToken();
                
                newUser.BindIP = Bind_IP;
                newUser.IPAddress = ip_address;
                newUser.TimeRefreshToken = DateTime.Now;
                newUser.RefreshToken = refreshToken;


                if (users.TryGetValue(login, out var user))
                {
                    return new JsonResult(user.AccessToken);
                }

                return new BadRequestResult();

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }


        /// <summary>
        /// Добавить нового трейдера
        /// </summary>
        /// <param name="userParametrsRequest">Параметры нового трейдера</param>
        /// <returns></returns>
        [HttpPost]
        [Route("/api/users/new")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [Authorize]
        public IActionResult SetUser(UserParametrsRequest userParametrsRequest)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                    return new UnauthorizedResult();

                if (users.ContainsKey(userParametrsRequest.login))
                {
                    return new BadRequestObjectResult($"Логин {userParametrsRequest.login} уже занят. Пользователь не создан.");
                }
                
                User newUser = new User(userParametrsRequest.login, Guid.NewGuid().ToString());
                users.TryAdd(userParametrsRequest.login, newUser);
                
                newUser.Admin = false;
                newUser.Name = userParametrsRequest.Name;
                

                string ip_address = HttpContext.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                var accessToken = JwtProvider.GenerateAccessToken(newUser);
                var refreshToken = JwtProvider.GenerateRefreshToken();
                newUser.BindIP = false;
                newUser.IPAddress = ip_address;
                newUser.TimeRefreshToken = DateTime.Now;
                newUser.RefreshToken = refreshToken;
                newUser.City = userParametrsRequest.City;
                newUser.Email = userParametrsRequest.Email;
                newUser.Description = userParametrsRequest.Description;
                newUser.Country = userParametrsRequest.Country;
                newUser.AddressResidential = userParametrsRequest.AddressResidential;
                newUser.PhoneNumber = userParametrsRequest.PhoneNumber;
                newUser.CanInput = userParametrsRequest.canInput;

                if (users.TryGetValue(userParametrsRequest.login, out User? user))
                {
                    DBManager.TryAddObjectToDB(newUser);

                    return new JsonResult(user);
                }

                return new BadRequestResult();

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// User parametrs request
        /// </summary>
        public record UserParametrsRequest
        {

            /// <summary>
            /// Login
            /// </summary>
            public required string login { get; set; }

            /// <summary>
            /// ФИО пользователя
            /// </summary>
            public string Name { get; set; }


            /// <summary>
            /// Bind IP address
            /// </summary>
            //public bool Bind_IP { get; set; }

            /// <summary>
            /// Страна трейдера
            /// </summary>
            public string Country { get; set; }

            /// <summary>
            /// Город трейдера
            /// </summary>
            public string City { get; set; }


            /// <summary>
            /// Адрес места жительства трейдера
            /// </summary>
            public string AddressResidential { get; set; }

            /// <summary>
            /// Телефон трейдера
            /// </summary>
            public string PhoneNumber { get; set; }


            /// <summary>
            /// E-mail трейдера
            /// </summary>
            public string Email { get; set; }

            /// <summary>
            /// Примечание
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Can input
            /// </summary>
            public bool canInput { get; set; }

        }

        /// <summary>
        /// Обновить параметры трейдера
        /// </summary>
        /// <param name="userParametrsRequest">Новые параметры трейдера</param>
        /// <returns></returns>
        [HttpPut]
        [Route("/api/users/update")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [Authorize]
        public IActionResult UserUpdate(UserParametrsUpdateRequest userParametrsRequest)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                    return new UnauthorizedResult();

                if (!users.TryGetValue(userParametrsRequest.login, out User? newUser))
                {
                    return new BadRequestObjectResult("No user"); 
                }

                newUser.CanInput = userParametrsRequest.canInput?? newUser.CanInput;
                newUser.Name = userParametrsRequest.Name?? newUser.Name;
                newUser.City = userParametrsRequest.City?? newUser.City;
                newUser.Email = userParametrsRequest.Email?? newUser.Email;
                newUser.Description = userParametrsRequest.Description ?? newUser.Description;
                newUser.Country = userParametrsRequest.Country ?? newUser.Country;
                newUser.AddressResidential = userParametrsRequest.AddressResidential ?? newUser.AddressResidential;
                newUser.PhoneNumber = userParametrsRequest.PhoneNumber ?? newUser.PhoneNumber;
                

                if (users.TryGetValue(userParametrsRequest.login, out var user))
                {
                    return new JsonResult(user);
                }

                return new BadRequestResult();

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// User parametrs request
        /// </summary>
        public record UserParametrsUpdateRequest
        {

            /// <summary>
            /// Login
            /// </summary>
            public required string login { get; set; }

            /// <summary>
            /// ФИО пользователя
            /// </summary>
            public string? Name { get; set; }


            /// <summary>
            /// Bind IP address
            /// </summary>
            //public bool Bind_IP { get; set; }

            /// <summary>
            /// Страна трейдера
            /// </summary>
            public string? Country { get; set; }

            /// <summary>
            /// Город трейдера
            /// </summary>
            public string? City { get; set; }


            /// <summary>
            /// Адрес места жительства трейдера
            /// </summary>
            public string? AddressResidential { get; set; }

            /// <summary>
            /// Телефон трейдера
            /// </summary>
            public string? PhoneNumber { get; set; }


            /// <summary>
            /// E-mail трейдера
            /// </summary>
            public string? Email { get; set; }

            /// <summary>
            /// Примечание
            /// </summary>
            public string? Description { get; set; }

            /// <summary>
            /// Can input
            /// </summary>
            public bool? canInput { get; set; }

        }



        /// <summary>
        /// Установить новый пароль трейдера
        /// </summary>
        /// <param name="userPasswordRequest">Трейдер и новый пароль</param>
        /// <returns></returns>
        [HttpPut]
        [Route("/api/users/newPassword")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [Authorize]
        public IActionResult SetNewPassword(UserPasswordRequest userPasswordRequest)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                    return new UnauthorizedResult();

                if (!users.TryGetValue(userPasswordRequest.login, out User? newUser))
                {
                    return new BadRequestObjectResult("No user");
                }

               
                newUser.Password = userPasswordRequest.newPassword;


                if (users.TryGetValue(userPasswordRequest.login, out var user))
                {
                    return new JsonResult(user);
                }

                return new BadRequestResult();

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

        }

        /// <summary>
        /// Установить администратора
        /// </summary>
        /// <param name="userAdminRequest">Трейдер и признак администратора</param>
        /// <returns></returns>
        [HttpPut]
        [Route("/api/users/admin")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [Authorize]
        public IActionResult SetAdmin(UserAdminRequest userAdminRequest)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                    return new UnauthorizedResult();

                if (!users.TryGetValue(userAdminRequest.login, out User? newUser))
                {
                    return new BadRequestObjectResult("No user");
                }


                newUser.Admin = userAdminRequest.admin;


                if (users.TryGetValue(userAdminRequest.login, out var user))
                {
                    return new JsonResult(user);
                }

                return new BadRequestResult();

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

        }

        /// <summary>
        /// User can trade request
        /// </summary>
        public record UserAdminRequest
        {
            /// <summary>
            /// Логин трейдера
            /// </summary>
            public string login { get; set; }

            /// <summary>
            /// Admin
            /// </summary>
            public bool admin { get; set; }
        }


        public record UserPasswordRequest
        {
            /// <summary>
            /// Логин трейдера
            /// </summary>
            public string login { get; set; }

            /// <summary>
            /// Password
            /// </summary>
            public string newPassword { get; set; }
        }



        /// <summary>
        /// User can trade request
        /// </summary>
        public record UserCanTradeRequest
        {
            /// <summary>
            /// Логин трейдера
            /// </summary>
            public string login { get; set; }

            /// <summary>
            /// Can input
            /// </summary>
            public string canInput { get; set; }
        }

       


        ServerService serverService;

        ServiceDB serviceDB;

        ConcurrentDictionaryDB<User> users;

        /// <summary>
        /// Dependency Injection
        /// </summary>
        /// <param name="_serverService"></param>
        /// <param name="_serviceDB"></param>
        public UserController(ServerService _serverService, ServiceDB _serviceDB)
        {
            serviceDB = _serviceDB;
            serverService = _serverService;
            users = serviceDB.GetUsersContext();
        }
    }


    
}
