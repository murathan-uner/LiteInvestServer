using LiteInvestServerDll;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiteInvest.Entity.ServerEntity;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace LiteInvestFront.Controller
{
    /// <summary>
    /// </summary>
    [ApiController]
    [Route("/api/blazor-tab")]
    public class TabController : ControllerBase
    {
        private readonly ServerService serverService;
        private readonly ServiceDB serviceDB;
        private readonly ConcurrentDictionary<string, User>? users;

        public TabController(ServerService _serverService, ServiceDB _serviceDB)
        {
            serviceDB = _serviceDB;
            serverService = _serverService;
            users = serviceDB.GetUsersContext();
        }

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("close-master")]
        [AllowAnonymous]
        //[Authorize]
        public IActionResult CloseMasterTab([FromBody] MasterTabCloseRequest data)
        {
            var token = data.Token;
            var tabProfile = data.TabProfile;
            var children = data.ChildTabProfiles;

            if (users != null)
            {
                var user = users.Values.FirstOrDefault(x => string.Equals(x.RefreshToken, token));
                if (user != null)
                {
                    Console.WriteLine($"Saving Tabs: {user.Login}");
                    if (tabProfile != null)
                    {
                        serverService.SaveMasterTab(user.Login, tabProfile);
                    }
                    if (children != null && children.Count > 0)
                    {
                        serverService.SaveTabs(user.Login, children);
                    }
                    return Ok();
                }
            }

            return Unauthorized();
        }

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("close-child")]
        [AllowAnonymous]
        //[Authorize]
        public IActionResult CloseChildTab([FromBody] ChildTabCloseRequest data)
        {
            var token = data.Token;
            var tabId = data.TabId;

            if (users != null)
            {
                var user = users.Values.FirstOrDefault(x => string.Equals(x.RefreshToken, token));
                if (user != null)
                {
                    Console.WriteLine($"Removing Tab: {user.Login} {tabId}");
                    serverService.DeleteTab(user.Login, tabId);
                    serverService.DeleteUserInstrumentOrdersAsync(user.Login, tabId);
                    serverService.DeleteUserInstrumentsAsync(user.Login, tabId);
                    return Ok();
                }
            }

            return Ok(tabId);
        }
    }

    public class MasterTabCloseRequest
    {
        public string Token { get; set; } = string.Empty;
        public TabProfile TabProfile { get; set; }
        public List<TabProfile> ChildTabProfiles { get; set; }
    }

    public class ChildTabCloseRequest
    {
        public string Token { get; set; } = string.Empty;
        public string TabId { get; set; } = string.Empty;
    }
}
