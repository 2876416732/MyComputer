﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MusicStore.ViewModels;
using MusicStoreEntity;
using MusicStoreEntity.UserAndRole;
using UserAndRole;

namespace MusicStore.Controllers
{
    public class AccountController : Controller
    {
        /// <summary>
        /// 填写注册信息
        /// </summary>
        /// <returns></returns>
        // GET: Account
        public ActionResult Register()
        {
            return View();
        }

        public static RegisterViewModel modelTest;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            Random random = new Random();
            //如果验证码为空，发送验证码邮件
            if (string.IsNullOrEmpty(model.VerificationCode))
            {
                string VC = (random.Next(111111, 999999)).ToString();
                var mailService = new Mafly.Mail.Mail();

                //参数：接收者邮箱、内容
                mailService.Send(model.Email, "验证码是：" + VC);
                modelTest = model;
                return View("VerificationCodeView");              
            }
            else
            {
            //    if (ModelState.IsValid)
            //{
                var person = new Person()
                {
                    FirstName = modelTest.FullName.Substring(0, 1),
                    LastName = modelTest.FullName.Substring(1, modelTest.FullName.Length - 1),
                    Name = modelTest.FullName,
                    CredentialsCode = "",
                    Birthday = DateTime.Now,
                    Sex = true,
                    MobileNumber = "18000010001",
                    Email = modelTest.Email,
                    TelephoneNumber = "18000010001",
                    Description = "",
                    CreateDateTime = DateTime.Now,
                    UpdateTime = DateTime.Now,
                    InquiryPassword = "未设置",
                };
                var user = new ApplicationUser()
                {
                    UserName = modelTest.UserName,
                    FirstName = modelTest.FullName.Substring(0, 1),
                    LastName = modelTest.FullName.Substring(1, modelTest.FullName.Length - 1),
                    ChineseFullName = modelTest.FullName,
                    MobileNumber = "18000010001",
                    Email = modelTest.Email,
                    Person = person,
                };

                var idManager = new IdentityManager();
                idManager.CreateUser(user, model.PassWord);
                idManager.AddUserToRole(user.Id, "RegisterUser");

                //return RedirectToAction();
                return Content("<script>alert('恭喜注册成功');location.href'" + Url.Action("login", "Account") + "</script>");
            //}
            //用户的保存Person ApplicationUser
            //return View();
            }
        }

        /// <summary>
        /// 登录方法
        /// </summary>
        /// <param name="returnUrl">登录成功后跳转地址</param>
        /// <returns></returns>
        public ActionResult Login(string returnUrl=null)
        {
            if (string.IsNullOrEmpty(returnUrl))
                ViewBag.ReturnUrl = Url.Action("index", "home");
            else
                ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        [HttpPost]   //此Action用来接收用户提交
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            //判断实体是否校验通过
            if (ModelState.IsValid)
            {
                var loginStatus = new LoginUserStatus()
                {
                     IsLogin =  false,
                    Message =  "用户或密码错误",
                };
                //登录处理
                var userManage =
                    new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new EntityDbContext()));
                var user = userManage.Find(model.UserName, model.PassWord);
                if (user != null)
                {
                    var roleName = "";
                    var context = new EntityDbContext();
                    foreach (var  role in user.Roles)
                    {
                        roleName += (context.Roles.Find(role.RoleId) as ApplicationRole).DisplayName + ",";
                    }

                    loginStatus.IsLogin = true;
                    loginStatus.Message = "登录成功！用户的角色：" + roleName;
                    loginStatus.GotoController = "home";
                    loginStatus.GotoAction = "index";
                    //把登录状态保存到会话
                    Session["loginStatus"] = loginStatus;

                    var loginUserSessionModel = new LoginUserSessionModel()
                    {
                        User = user,
                        Person = user.Person,
                        RoleName = roleName,
                    };
                    //把登录成功后用户信息保存到会话
                    Session["LoginUserSessionModel"] = loginUserSessionModel;

                    //identity登录处理,创建aspnet的登录令牌Token
                    var identity = userManage.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
                    return Redirect(returnUrl);
                }
                else
                {
                    if (string.IsNullOrEmpty(returnUrl))
                        ViewBag.ReturnUrl = Url.Action("index", "home");
                    else
                        ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginUserStatus = loginStatus;
                    return View();
                }
            }
            if (string.IsNullOrEmpty(returnUrl))
                ViewBag.ReturnUrl = Url.Action("index", "home");
            else
                ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public ActionResult LoginOut()
        {
            Session.Remove("loginStatus");
            Session.Remove("LoginUserSessionModel");
            return RedirectToAction("index", "Home");
        }

        public ActionResult ChangePassWord()
        {
            //用户得先登录才能修改
            if (Session["LoginUserSessionModel"]==null)
            {
                return RedirectToAction("login");
            }
            return View();
        }

        [HttpPost]
        public ActionResult ChangePassWord(ChangePassWordViewModel model)
        {
            bool changePwdSuccessed;
            try
            {
                var userManage =
                 new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new EntityDbContext()));
                var username=(Session["LoginUserSessionModel"]as LoginUserSessionModel).User.UserName;
                var user = userManage.Find(username, model.PassWord);
                if(user==null)
                {
                    ModelState.AddModelError("", "原密码不正确");
                    return View();
                }
                else
                {
                    //修改密码
                    changePwdSuccessed=userManage.ChangePassword(user.Id, model.PassWord, model.NewPassWord)
                        .Succeeded;

                    if(changePwdSuccessed)
                    {
                        return Content("<script>alert('恭喜修改密码成功');location.href='" + 
                            Url.Action("Index", "Home") + "'</script>");
                    }
                    else
                    {
                        ModelState.AddModelError("", "修改密码失败，请重试");
                    }
                }
            }
            catch
            {
                ModelState.AddModelError("", "修改密码失败，请重试");
            }
            return View(model);
        }            
    }
}