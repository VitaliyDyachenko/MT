﻿using System;
using System.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using MT.DataAccess.EntityFramework;
using MT.DomainLogic;
using MT.ModelEntities.Entities;
using MT.Web.ViewModels;

namespace MT.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAccountService _accountService;
        private readonly IUserLoginService _userLoginService;

        public AccountController(IUnitOfWork unitOfWork, IAccountService accountService, IUserLoginService userLoginService)
        {
            _unitOfWork = unitOfWork;
            _accountService = accountService;
            _userLoginService = userLoginService;
        }

        public ActionResult Index()
        {
            return View();
        }

        //
        // GET: /Account/
        public ActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="user">a new user which is passed from user form</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Register(RegisterViewModel user)
        {
            if (ModelState.IsValid)
            {
                _accountService.RegisterUser(new User() { Email = user.Email, Password = user.Password });
                _unitOfWork.Commit();
            }

            return View(user);
        }

        /// <summary>
        /// verifies the existence of such E-mail in the database 
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public JsonResult CheckEmail(string email)
        {
            var result = _accountService.CheckEmail(email);
            return Json(!result, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// Provides the authorization functionality for registered users
        /// </summary>
        /// <param name="userAuth">The UserAuthorization instance that is sent from the login form</param>
        /// <param name="returnUrl">The Url where user is went from</param>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Login(UserAuthorization userAuth, string returnUrl)
        {
            if (!ModelState.IsValid) return View(userAuth);

            var user = _userLoginService.GetUserFromEmail(userAuth.Email);
            var message = new { text = "Login success!", status = true };

            if (user == null)
            {
                message = new { text = "The user name or password provided is incorrect", status = false };
                return Json(message);
            }

            var userBan = _userLoginService.GetUserBan(user.Id);
            var validateUser = _userLoginService.ValidateUser(userAuth.Email, userAuth.Password);
            var banTime = _userLoginService.GetBanTime(userBan);
            var banInterval = Int32.Parse(ConfigurationManager.AppSettings["BanInterval"]);
            var maxAttemptValue = Int32.Parse(ConfigurationManager.AppSettings["MaxAttemptValue"]);
            var userLoginHistory = new UserLoginHistory { UserId = user.Id, LoginDate = DateTime.Now, LoginResult = false };

            if (userBan.UserIsBan)
            {
                if (banTime < banInterval)
                {
                    message = new { text = "Current user is banned", status = false };
                    return Json(message);
                }
            }

            if (!validateUser)
            {
                _userLoginService.UserLoginHistory(userLoginHistory);

                if (userBan.AttemptCount < maxAttemptValue) userBan.AttemptCount++;

                if (userBan.AttemptCount == maxAttemptValue)
                {
                    userBan.UserIsBan = true;
                    userBan.StartBanTime = DateTime.Now;
                    userBan.AttemptCount = 0;
                    _unitOfWork.Commit();

                    message = new { text = "Current user is banned", status = false };
                    return Json(message);
                }

                _unitOfWork.Commit();

                message = new { text = "The user name or password provided is incorrect", status = false };
                return Json(message);
            }

            FormsAuthentication.SetAuthCookie(userAuth.Email, false);
            if (banTime > banInterval) userBan.UserIsBan = false;

            userBan.AttemptCount = 0;
            userLoginHistory.LoginResult = true;
            _userLoginService.UserLoginHistory(userLoginHistory);
            _unitOfWork.Commit();

            return Json(message);
        }
    }
}

