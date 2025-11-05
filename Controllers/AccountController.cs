using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using secureAuth0.Models;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace secureAuth0.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Reusable method to get Auth0 Management API access token
        private async Task<string> GetAuth0AccessTokenAsync()
        {
            var domain = _configuration["Auth0API:Domain"];
            var clientId = _configuration["Auth0API:ClientId"];
            var clientSecret = _configuration["Auth0API:ClientSecret"];
            var audience = _configuration["Auth0API:Audience"];

            var tokenClient = new HttpClient();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{domain}/oauth/token");
            tokenRequest.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("audience", audience)
            });
            var tokenResponse = await tokenClient.SendAsync(tokenRequest);
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenObj = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            return tokenObj.access_token;
        }

        // Reusable method to get current user's email from claims
        private string GetCurrentUserEmail()
        {
            return User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        }

        // Reusable method to get current user's Auth0 user ID from claims
        private string GetCurrentUserId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        }

        // Reusable method to call Auth0 Management API
        private async Task<T> CallAuth0ApiAsync<T>(string endpoint, string accessToken)
        {
            var domain = _configuration["Auth0API:Domain"];
            var apiClient = new HttpClient();
            var apiRequest = new HttpRequestMessage(HttpMethod.Get, $"https://{domain}{endpoint}");
            apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var apiResponse = await apiClient.SendAsync(apiRequest);
            var responseJson = await apiResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseJson);
        }

        public async Task Login(string returnUrl)
        {
            returnUrl = "/Home/Index";
            var authnticationProperties = new LoginAuthenticationPropertiesBuilder()
                .WithRedirectUri(returnUrl).Build();
            await HttpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authnticationProperties);
        }

        [Authorize]
        public IActionResult Profile()
        {
            var userProfile = new secureAuth0.Models.UserProfile
            {
                Name = User.Identity.Name,
                Email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                ProfileImageUrl = User.Claims.FirstOrDefault(c => c.Type == "picture")?.Value
            };
            return View(userProfile);
        }

        [Authorize]
        public async Task Logout()
        {
            var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
                .WithRedirectUri(Url.Action("Index", "Home"))
                .Build();
            await HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Auth0UserInfo()
        {
            // Print all claims to the console for debugging
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"CLAIM TYPE: {claim.Type} | VALUE: {claim.Value}");
            }

            try
            {
                var accessToken = await GetAuth0AccessTokenAsync();
                var userId = GetCurrentUserId();
                // var userInfo = await CallAuth0ApiAsync<Auth0UserInfo>($"/api/v2/users/{userId}", accessToken);
                var userInfo = await CallAuth0ApiAsync<UserProfile>($"/api/v2/users/{userId}", accessToken);
                return View(userInfo);
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Error in Auth0UserInfo: {ex.Message}");
                Console.WriteLine($"Error in UserProfile: {ex.Message}");
                // return View(new Auth0UserInfo());
                return View(new UserProfile());
            }
        }

        [Authorize]
        public async Task<IActionResult> MyPrivacy()
        {
            try
            {
                var accessToken = await GetAuth0AccessTokenAsync();
                var userId = GetCurrentUserId();
                // var userInfo = await CallAuth0ApiAsync<Auth0UserInfo>($"/api/v2/users/{userId}", accessToken);
                var userInfo = await CallAuth0ApiAsync<UserProfile>($"/api/v2/users/{userId}", accessToken);

                bool consentGiven = userInfo.app_metadata != null && userInfo.app_metadata.privacy_policies == true;
                ViewBag.ConsentGiven = consentGiven;
                return View("~/Views/Home/Privacy.cshtml");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MyPrivacy: {ex.Message}");
                ViewBag.ConsentGiven = false;
                return View("~/Views/Home/Privacy.cshtml");
            }
        }

        [Authorize]
        public async Task<IActionResult> UsersByEmail()
        {
            try
            {
                var accessToken = await GetAuth0AccessTokenAsync();
                var email = GetCurrentUserEmail();

                if (string.IsNullOrEmpty(email))
                {
                    // return View(new List<Auth0UserInfo>());
                    return View(new List<UserProfile>());
                }

                // var users = await CallAuth0ApiAsync<List<Auth0UserInfo>>($"/api/v2/users-by-email?email={email}", accessToken);
                var users = await CallAuth0ApiAsync<List<UserProfile>>($"/api/v2/users-by-email?email={email}", accessToken);
                return View("UsersByEmail", users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UsersByEmail: {ex.Message}");
                // return View("UsersByEmail", new List<Auth0UserInfo>());
                return View("UsersByEmail", new List<UserProfile>());
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> LinkAccount(string primaryUserId, string secondaryUserId, string provider)
        {
            try
            {
                var accessToken = await GetAuth0AccessTokenAsync();
                var domain = _configuration["Auth0API:Domain"];

                // Call Auth0 Management API to link accounts
                var apiClient = new HttpClient();
                var apiRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{domain}/api/v2/users/{primaryUserId}/identities");
                apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var linkData = new
                {
                    provider = provider,
                    user_id = secondaryUserId
                };
                var jsonContent = JsonConvert.SerializeObject(linkData);
                apiRequest.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var apiResponse = await apiClient.SendAsync(apiRequest);
                var responseJson = await apiResponse.Content.ReadAsStringAsync();

                if (apiResponse.IsSuccessStatusCode)
                {
                    // Success - force re-authentication with primary account
                    var returnUrl = Url.Action("Index", "Home");
                    var authProperties = new LoginAuthenticationPropertiesBuilder()
                        .WithRedirectUri(returnUrl)
                        .WithParameter("prompt", "login") // Force fresh login
                        .Build();

                    await HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme);
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
                }
                else
                {
                    // Handle error
                    ViewBag.ErrorMessage = $"Failed to link accounts: {responseJson}";
                    return RedirectToAction("UsersByEmail");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LinkAccount: {ex.Message}");
                ViewBag.ErrorMessage = $"Error linking accounts: {ex.Message}";
                return RedirectToAction("UsersByEmail");
            }
        }

        // Method to check if user has linked accounts
        //private bool HasLinkedAccounts(List<Auth0UserInfo> users)
        private bool HasLinkedAccounts(List<UserProfile> users)
        {
            if (users == null || users.Count == 0) return false;

            // If there's only one user but it has multiple identities, it has linked accounts
            if (users.Count == 1 && users[0].identities != null && users[0].identities.Count > 1)
            {
                return true;
            }

            // If there are multiple users, they might be unlinked
            return users.Count > 1;
        }

        // Method to get primary account (the one with linked identities)
        //private Auth0UserInfo GetPrimaryAccount(List<Auth0UserInfo> users)
        private UserProfile GetPrimaryAccount(List<UserProfile> users)
        {
            if (users == null || users.Count == 0) return null;

            // If there's only one user, it's the primary
            if (users.Count == 1) return users[0];

            // Find the user with multiple identities (linked accounts)
            var primaryUser = users.FirstOrDefault(u => u.identities != null && u.identities.Count > 1);
            if (primaryUser != null) return primaryUser;

            // If no user has multiple identities, return the first one
            return users[0];
        }

        // Method to get linked account details
        //private List<object> GetLinkedAccountDetails(Auth0UserInfo primaryUser)
        private List<object> GetLinkedAccountDetails(UserProfile primaryUser)
        {
            var linkedAccounts = new List<object>();

            if (primaryUser?.identities == null) return linkedAccounts;

            foreach (var identity in primaryUser.identities)
            {
                linkedAccounts.Add(new
                {
                    Provider = identity.provider,
                    UserId = identity.user_id,
                    Connection = identity.connection,
                    IsSocial = identity.isSocial,
                    ProfileData = identity.profileData
                });
            }

            return linkedAccounts;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CheckUnlinkedMultipleAccounts()
        {
            try
            {
                Console.WriteLine("CheckUnlinkedMultipleAccounts called");
                var accessToken = await GetAuth0AccessTokenAsync();
                var email = GetCurrentUserEmail();

                Console.WriteLine($"User email: {email}");
                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine("Email is null or empty");
                    return Json(new { hasMultipleAccounts = false, message = "" });
                }

                // var users = await CallAuth0ApiAsync<List<Auth0UserInfo>>($"/api/v2/users-by-email?email={email}", accessToken);
                var users = await CallAuth0ApiAsync<List<UserProfile>>($"/api/v2/users-by-email?email={email}", accessToken);
                Console.WriteLine($"Users API response: {JsonConvert.SerializeObject(users)}");

                // Check for unlinked multiple accounts: userList.length > 1
                bool hasUnlinkedMultipleAccounts = users != null && users.Count > 1;
                string message = hasUnlinkedMultipleAccounts
                    ? $"We found {users.Count} accounts associated with your email address <strong>{email}</strong>. You can link these accounts to merge them into a single account. Click 'Go to Linked Accounts' to manage your account linking."
                    : "";

                Console.WriteLine($"Has unlinked multiple accounts: {hasUnlinkedMultipleAccounts}");
                Console.WriteLine($"Message: {message}");

                return Json(new { hasMultipleAccounts = hasUnlinkedMultipleAccounts, message, accountCount = users?.Count ?? 0 });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckUnlinkedMultipleAccounts: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { hasMultipleAccounts = false, message = "", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CheckMultipleAccounts()
        {
            try
            {
                Console.WriteLine("CheckMultipleAccounts called");
                var accessToken = await GetAuth0AccessTokenAsync();
                var email = GetCurrentUserEmail();

                Console.WriteLine($"User email: {email}");
                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine("Email is null or empty");
                    return Json(new { hasMultipleAccounts = false, message = "" });
                }

                // var users = await CallAuth0ApiAsync<List<Auth0UserInfo>>($"/api/v2/users-by-email?email={email}", accessToken);
                var users = await CallAuth0ApiAsync<List<UserProfile>>($"/api/v2/users-by-email?email={email}", accessToken);
                Console.WriteLine($"Users API response: {JsonConvert.SerializeObject(users)}");

                // Check for linked accounts: userList.length === 1 && identities.length > 1
                bool hasLinkedAccounts = false;
                if (users != null && users.Count == 1)
                {
                    var user = users[0];
                    if (user.identities != null && user.identities.Count > 1)
                    {
                        hasLinkedAccounts = true;
                    }
                }

                string message = hasLinkedAccounts
                    ? $"Your account is linked with multiple identities. You can view your linked accounts information."
                    : "";

                Console.WriteLine($"Has linked accounts: {hasLinkedAccounts}");
                Console.WriteLine($"Message: {message}");

                return Json(new { hasMultipleAccounts = hasLinkedAccounts, message, accountCount = users?.Count ?? 0 });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckMultipleAccounts: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { hasMultipleAccounts = false, message = "", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CheckLinkedAccounts()
        {
            try
            {
                var accessToken = await GetAuth0AccessTokenAsync();
                var email = GetCurrentUserEmail();

                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { hasLinkedAccounts = false, primaryAccount = (object)null, linkedAccounts = new List<object>() });
                }

                // var users = await CallAuth0ApiAsync<List<Auth0UserInfo>>($"/api/v2/users-by-email?email={email}", accessToken);
                var users = await CallAuth0ApiAsync<List<UserProfile>>($"/api/v2/users-by-email?email={email}", accessToken);

                bool hasLinkedAccounts = HasLinkedAccounts(users);
                var primaryAccount = GetPrimaryAccount(users);
                var linkedAccountDetails = GetLinkedAccountDetails(primaryAccount);

                return Json(new
                {
                    hasLinkedAccounts,
                    primaryAccount = new
                    {
                        userId = primaryAccount?.user_id,
                        // name = primaryAccount?.name,
                        name = primaryAccount?.Name,
                        // email = primaryAccount?.email,
                        email = primaryAccount?.Email,
                        picture = primaryAccount?.picture
                    },
                    linkedAccounts = linkedAccountDetails,
                    totalAccounts = users?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckLinkedAccounts: {ex.Message}");
                return Json(new { hasLinkedAccounts = false, primaryAccount = (object)null, linkedAccounts = new List<object>(), error = ex.Message });
            }
        }

        [Authorize]
        public async Task<IActionResult> LinkedAccountsInfo()
        {
            try
            {
                var accessToken = await GetAuth0AccessTokenAsync();
                var email = GetCurrentUserEmail();

                if (string.IsNullOrEmpty(email))
                {
                    return View(new { hasLinkedAccounts = false, primaryAccount = (object)null, linkedAccounts = new List<object>(), error = (string)null });
                }

                // var users = await CallAuth0ApiAsync<List<Auth0UserInfo>>($"/api/v2/users-by-email?email={email}", accessToken);
                var users = await CallAuth0ApiAsync<List<UserProfile>>($"/api/v2/users-by-email?email={email}", accessToken);

                bool hasLinkedAccounts = HasLinkedAccounts(users);
                var primaryAccount = GetPrimaryAccount(users);
                var linkedAccountDetails = GetLinkedAccountDetails(primaryAccount);

                var viewModel = new
                {
                    hasLinkedAccounts,
                    primaryAccount = new
                    {
                        userId = primaryAccount?.user_id,
                        // name = primaryAccount?.name,
                        name = primaryAccount?.Name,
                        // email = primaryAccount?.email,
                        email = primaryAccount?.Email,
                        picture = primaryAccount?.picture,
                        created_at = primaryAccount?.created_at,
                        updated_at = primaryAccount?.updated_at
                    },
                    linkedAccounts = linkedAccountDetails,
                    totalAccounts = users?.Count ?? 0,
                    error = (string)null
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LinkedAccountsInfo: {ex.Message}");
                return View(new { hasLinkedAccounts = false, primaryAccount = (object)null, linkedAccounts = new List<object>(), error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var accessToken = await GetAuth0AccessTokenAsync();
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri($"https://{_configuration["Auth0:Domain"]}/");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    var response = await client.DeleteAsync($"/api/v2/users/{userId}");
                    if (response.IsSuccessStatusCode)
                    {
                        // Optionally: Sign out the user if they deleted themselves
                        await HttpContext.SignOutAsync();
                        return Json(new { success = true, message = "User deleted successfully." });
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        return Json(new { success = false, message = $"Failed to delete user: {error}" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Exception: {ex.Message}" });
            }
        }
    }
}
