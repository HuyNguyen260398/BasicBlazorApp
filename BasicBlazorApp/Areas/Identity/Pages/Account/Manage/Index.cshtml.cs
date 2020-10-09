using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BasicBlazorApp.Areas.Identity.Data;
using BasicBlazorApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Twilio.Exceptions;
using Twilio.Rest.Lookups.V1;

namespace BasicBlazorApp.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            CountryService countryService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            AvailableCountries = countryService.GetCountries();
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public List<SelectListItem> AvailableCountries { get; }

        public class InputModel
        {
            [DataType(DataType.Text)]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [DataType(DataType.Text)]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [DataType(DataType.Text)]
            [Display(Name = "Address")]
            public string Address { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Phone number country")]
            public string PhoneNumberCountryCode { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                PhoneNumber = phoneNumber
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            //var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            //if (Input.PhoneNumber != phoneNumber)
            //{
            //    var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            //    if (!setPhoneResult.Succeeded)
            //    {
            //        StatusMessage = "Unexpected error when trying to set phone number.";
            //        return RedirectToPage();
            //    }
            //}

            if (Input.FirstName != user.FirstName)
            {
                user.FirstName = Input.FirstName;
            }

            if (Input.LastName != user.LastName)
            {
                user.LastName = Input.LastName;
            }

            if (Input.Address != user.Address)
            {
                user.Address = Input.Address;
            }

            // twilio
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != null && Input.PhoneNumber != phoneNumber)
            {
                try
                {
                    var numberDetails = await PhoneNumberResource.FetchAsync(
                        pathPhoneNumber: new Twilio.Types.PhoneNumber(Input.PhoneNumber),
                        countryCode: Input.PhoneNumberCountryCode,
                        type: new List<string> { "carrier" });

                    // only allow user to set phone number if capable of receiving SMS
                    if (numberDetails?.Carrier != null
                        && numberDetails.Carrier.TryGetValue("type", out var phoneNumberType)
                        && phoneNumberType == "landline")
                    {
                        ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.PhoneNumber)}",
                            $"The number you entered does not appear to be capable of receiving SMS ({phoneNumberType}). Please enter a different value and try again");
                        return Page();
                    }

                    var numberToSave = numberDetails.PhoneNumber.ToString();
                    var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, numberToSave);
                    if (!setPhoneResult.Succeeded)
                    {
                        var userId = await _userManager.GetUserIdAsync(user);
                        throw new InvalidOperationException($"Unexpected error occurred setting phone number for user with ID '{userId}'.");
                    }
                }
                catch (ApiException ex)
                {
                    ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.PhoneNumber)}",
                        $"The number you entered was not valid (Twilio code {ex.Code}), please check it and try again");
                    return Page();
                }
            }

            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
