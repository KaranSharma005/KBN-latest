using KBN.Models;
using KBN.RepoHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KBN.Controllers
{
    [Authorize]
    public class LinksController : Controller
    {
        private readonly LinkHelper _linkHelper;

        private readonly UserManager<ApplicationUser> _userManager;
        public LinksController (
            LinkHelper linkHelper,
            UserManager<ApplicationUser> userManager
        )
        {
            _linkHelper = linkHelper;
            _userManager = userManager;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public IActionResult LinkPartial()
        {
            return PartialView(_linkHelper.SubscriberList());
        }

        [HttpPost]
        public async Task<IActionResult> AddLink(LinkModal modal)
        {
            if(modal.sub_id == 0)
            {
                return Json(new { message = "Please select a subscriber" });
            }
            if(!_linkHelper.IsValid(modal.didNo))
            {
                return Json(new { message = "DID not exist" });
            }
            var result = _linkHelper.IsAlreadyLinked(modal);
            if(result.DidExists && result.SubExists)
            {
                return Json(new { message = "Both the did and subscriberId already linked" });
            }
            else if(result.DidExists)
            {
                return Json(new { message = "This did id already linked with subscriber select another one" });
            }
            else if(result.SubExists) 
            {
                return Json(new { message = "This subscriber is already linked with did select another one" });
            }
                var user = await _userManager.GetUserAsync(User);
            _linkHelper.LinkOne(modal, user.Email);
            return Json("Linked Successfully");
        }

        [HttpPost]
        public IActionResult PaginatedLinks(LinkPaginationModal modal)
        {
            ViewBag.totalLinks = _linkHelper.GetTotalLinks();
            return PartialView(_linkHelper.GetPaginatedLinks(modal));   
        }

        [HttpPost]
        public IActionResult UnlinkSubscriber(int id)
        {
            _linkHelper.UnlinkOne(id);
            return Json("Successfully unlink subscriber");
        }

        public IActionResult Alias()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AliasPartial(PaginatedAliasModal modal)
        {
            return PartialView(_linkHelper.GetDIDSubscriberAlias(modal));
        }
    }
}
