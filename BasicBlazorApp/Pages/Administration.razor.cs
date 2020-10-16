using BasicBlazorApp.Areas.Identity.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBlazorApp.Pages
{
    public partial class Administration : ComponentBase
    {
        [Inject]
        UserManager<ApplicationUser> _UserManager { get; set; }

        [Inject]
        RoleManager<IdentityRole> _RoleManager { get; set; }

        [Inject]
        AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        //public Administration(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        //{
        //    _UserManager = userManager;
        //    _RoleManager = roleManager;
        //}

        [CascadingParameter]
        private Task<AuthenticationState> authenticationStateTask { get; set; }
        string ADMINISTRATION_ROLE = "Admin";
        System.Security.Claims.ClaimsPrincipal CurrentUser;

        // Property used to add or edit the currently selected user
        ApplicationUser objUser = new ApplicationUser();
        // Tracks the selected role for the currently selected user
        string CurrentUserRole { get; set; } = "Staff";
        // Collection to display the existing users
        List<ApplicationUser> ColUsers = new List<ApplicationUser>();
        // Options to display in the roles dropdown when editing a user
        List<string> Options = new List<string>() { "Staff", "Admin" };
        // To hold any possible errors
        string strError = "";
        // To enable showing the Popup
        bool ShowPopup = false;

        protected override async Task OnInitializedAsync()
        {
            // ensure there is a ADMINISTRATION_ROLE
            var RoleResult = await _RoleManager.FindByNameAsync(ADMINISTRATION_ROLE);
            if (RoleResult == null)
            {
                // Create ADMINISTRATION_ROLE Role
                await _RoleManager.CreateAsync(new IdentityRole(ADMINISTRATION_ROLE));
            }
            // Ensure a user named Admin@BlazorHelpWebsite.com is an Administrator
            var user = await _UserManager.FindByNameAsync("Admin@BlazorHelpWebsite.com");
            if (user != null)
            {
                // Is Admin@BlazorHelpWebsite.com in administrator role?
                var UserResult = await _UserManager.IsInRoleAsync(user, ADMINISTRATION_ROLE);
                if (!UserResult)
                {
                    // Put admin in Administrator role
                    await _UserManager.AddToRoleAsync(user, ADMINISTRATION_ROLE);
                }
            }
            // Get the current logged in user
            CurrentUser = (await authenticationStateTask).User;

            // Get the users
            GetUsers();
        }

        public void GetUsers()
        {
            // clear any error messages
            strError = "";
            // Collection to hold users
            ColUsers = new List<ApplicationUser>();
            // get users from _UserManager
            var user = _UserManager.Users.Select(x => new ApplicationUser
            {
                Id = x.Id,
                UserName = x.UserName,
                Email = x.Email,
                PasswordHash = "*****"
            });
            foreach (var item in user)
            {
                ColUsers.Add(item);
            }
        }

        void AddNewUser()
        {
            // Make new user
            objUser = new ApplicationUser();
            objUser.PasswordHash = "*****";
            // Set Id to blank so we know it is a new record
            objUser.Id = "";
            // Open the Popup
            ShowPopup = true;
        }

        async Task SaveUser()
        {
            try
            {
                // Is this an existing user?
                if (objUser.Id != "")
                {
                    // Get the user
                    var user = await _UserManager.FindByIdAsync(objUser.Id);
                    // Update Email
                    user.Email = objUser.Email;
                    // Update the user
                    await _UserManager.UpdateAsync(user);
                    // Only update password if the current value
                    // is not the default value
                    if (objUser.PasswordHash != "*****")
                    {
                        var resetToken = await _UserManager.GeneratePasswordResetTokenAsync(user);
                        var passworduser = await _UserManager.ResetPasswordAsync(user, resetToken, objUser.PasswordHash);
                        if (!passworduser.Succeeded)
                        {
                            if (passworduser.Errors.FirstOrDefault() != null)
                            {
                                strError = passworduser.Errors.FirstOrDefault().Description;
                            }
                            else
                            {
                                strError = "Pasword error";
                            }
                            // Keep the popup opened
                            return;
                        }
                    }
                    // Handle Roles
                    // Is user in administrator role?
                    var UserResult = await _UserManager.IsInRoleAsync(user, ADMINISTRATION_ROLE);
                    // Is Administrator role selected 
                    // but user is not an Administrator?
                    if (
                        (CurrentUserRole == ADMINISTRATION_ROLE)
                        &
                        (!UserResult))
                    {
                        // Put admin in Administrator role
                        await _UserManager.AddToRoleAsync(user, ADMINISTRATION_ROLE);
                    }
                    else
                    {
                        // Is Administrator role not selected 
                        // but user is an Administrator?
                        if ((CurrentUserRole != ADMINISTRATION_ROLE)
                            &
                            (UserResult))
                        {
                            // Remove user from Administrator role
                            await _UserManager.RemoveFromRoleAsync(user, ADMINISTRATION_ROLE);
                        }
                    }
                }
                else
                {
                    // Insert new user
                    var NewUser = new ApplicationUser
                    {
                        UserName = objUser.UserName,
                        Email = objUser.Email
                    };
                    var CreateResult = await _UserManager.CreateAsync(NewUser, objUser.PasswordHash);
                    if (!CreateResult.Succeeded)
                    {
                        if (CreateResult.Errors.FirstOrDefault() != null)
                        {
                            strError = CreateResult.Errors.FirstOrDefault().Description;
                        }
                        else
                        {
                            strError = "Create error";
                        }
                        // Keep the popup opened
                        return;
                    }
                    else
                    {
                        // Handle Roles
                        if (CurrentUserRole == ADMINISTRATION_ROLE)
                        {
                            // Put admin in Administrator role
                            await _UserManager.AddToRoleAsync(NewUser, ADMINISTRATION_ROLE);
                        }
                    }
                }
                // Close the Popup
                ShowPopup = false;
                // Refresh Users
                GetUsers();
            }
            catch (Exception ex)
            {
                strError = ex.GetBaseException().Message;

            }
        }

        async Task EditUser(ApplicationUser _ApplicationUser)
        {
            // Set the selected user
            // as the current user
            objUser = _ApplicationUser;
            // Get the user
            var user = await _UserManager.FindByIdAsync(objUser.Id);
            if (user != null)
            {
                // Is user in administrator role?
                var UserResult =
                    await _UserManager
                    .IsInRoleAsync(user, ADMINISTRATION_ROLE);
                if (UserResult)
                {
                    CurrentUserRole = ADMINISTRATION_ROLE;
                }
                else
                {
                    CurrentUserRole = "Users";
                }
            }
            // Open the Popup
            ShowPopup = true;
        }

        async Task DeleteUser()
        {
            // Close the Popup
            ShowPopup = false;
            // Get the user
            var user = await _UserManager.FindByIdAsync(objUser.Id);
            if (user != null)
            {
                // Delete the user
                await _UserManager.DeleteAsync(user);
            }
            // Refresh Users
            GetUsers();
        }

        void ClosePopup()
        {
            // Close the Popup
            ShowPopup = false;
        }
    }
}
