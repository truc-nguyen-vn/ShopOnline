﻿using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using ShopOnline.Core;
using ShopOnline.Core.Entities;
using ShopOnline.Core.Exceptions;
using ShopOnline.Core.Helpers;
using ShopOnline.Core.Models;
using ShopOnline.Core.Models.Account;
using ShopOnline.Core.Models.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using static ShopOnline.Core.Models.Enum.AppEnum;

namespace ShopOnline.Business.Logic
{
    public class UserBusiness : IUserBusiness
    {
        private readonly MyDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;


        public UserBusiness(MyDbContext context,
                            IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<ClaimsPrincipal> LoginAsync(AccountLoginModel accountLogin)
        {
            Expression<Func<IBaseUserEntity, BaseInforAccountModel>> selectBaseInforAccount = x => new BaseInforAccountModel
            {
                Email = x.Email,
                Password = x.Password,
                FullName = x.FullName,
                TypeAcc = x.TypeAcc,
            };

            var inforAccount = await _context.Customers.Where(x => x.Email == accountLogin.Email && !x.IsDeleted)
                                        .Select(selectBaseInforAccount)
                                        .FirstOrDefaultAsync();
            if (inforAccount == null)
                inforAccount = await _context.Staffs.Where(x => x.Email == accountLogin.Email && !x.IsDeleted)
                                        .Select(selectBaseInforAccount)
                                        .FirstOrDefaultAsync();
            if (inforAccount == null)
                inforAccount = await _context.Shippers.Where(x => x.Email == accountLogin.Email && !x.IsDeleted)
                                        .Select(selectBaseInforAccount)
                                        .FirstOrDefaultAsync();
            if (inforAccount == null)
                throw new UserFriendlyException(ErrorCode.WrongEmail);

            string password;
            bool loginSuccess = false;

            switch (inforAccount.TypeAcc)
            {
                case TypeAcc.Customer:
                    HashPasswordHelper.HashPasswordStrategy = new HashMD5Strategy();
                    password = HashPasswordHelper.DoHash(accountLogin.Password);
                    if (inforAccount.Password == password)
                        loginSuccess = true;
                    break;
                case TypeAcc.Shipper:
                    HashPasswordHelper.HashPasswordStrategy = new HashSHA1Strategy();
                    password = HashPasswordHelper.DoHash(accountLogin.Password);
                    if (inforAccount.Password == password)
                        loginSuccess = true;
                    break;
                default: // Admin || Staff
                    HashPasswordHelper.HashPasswordStrategy = new HashSHA256Strategy();
                    password = HashPasswordHelper.DoHash(accountLogin.Password);
                    if (inforAccount.Password == password)
                        loginSuccess = true;
                    break;
            }

            if (!loginSuccess)
                throw new UserFriendlyException(ErrorCode.WrongPassword);

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, inforAccount.Email),
                    new Claim(ClaimTypes.Name, inforAccount.FullName),
                    new Claim(ClaimTypes.Role, inforAccount.TypeAcc.ToString().ToLower()),
                };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            return claimsPrincipal;
        }

        public async Task<bool> RegisterAsync(AccountRegisterModel accountRegister)
        {
            bool isExistingEmail = await _context.Customers.AnyAsync(x => x.Email == accountRegister.Email);

            if (isExistingEmail)
            {
                throw new UserFriendlyException(ErrorCode.EmailExisted);
            }

            HashPasswordHelper.HashPasswordStrategy = new HashMD5Strategy();
            var newAccountCustomer = new CustomerEntity()
            {
                FullName = accountRegister.FullName,
                Email = accountRegister.Email,
                PhoneNumber = accountRegister.PhoneNumber,
                Password = HashPasswordHelper.DoHash(accountRegister.Password),
                TypeAcc = TypeAcc.Customer
            };

            _context.Customers.Add(newAccountCustomer);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task ResetPasswordAsync(ResetPasswordModel resetPasswordModel)
        {
            var accountReset = await _context.Customers.Where(x => x.Email == resetPasswordModel.Email).FirstOrDefaultAsync();

            if (accountReset == null)
            {
                throw new UserFriendlyException(ErrorCode.EmailNotExisted);
            }

            if (accountReset.PhoneNumber != resetPasswordModel.PhoneNumber)
            {
                throw new UserFriendlyException(ErrorCode.PhoneNotMatch);
            }

            string newPassword = UserHelper.GetNewRandomPassword();

            HashPasswordHelper.HashPasswordStrategy = new HashMD5Strategy();
            accountReset.Password = HashPasswordHelper.DoHash(newPassword);

            MimeMessage message = new();

            MailboxAddress from = new("Dreams Store", "dreamsstore.ss@gmail.com");
            message.From.Add(from);

            MailboxAddress to = new(accountReset.FullName, accountReset.Email);
            message.To.Add(to);

            message.Subject = "Cấp lại mật khẩu thành công";
            BodyBuilder bodyBuilder = new()
            {
                HtmlBody = $"<h1>Mật khẩu của bạn đã được reset, mật khẩu mới: {newPassword}  </h1>",
                TextBody = "Mật Khẩu của bạn đã được thay đổi "
            };
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                SmtpClient client = new();
                //connect (smtp address, port , true)
                await client.ConnectAsync("smtp.gmail.com", 465, true);
                await client.AuthenticateAsync("dreamsstore.ss@gmail.com", "4thanggay");

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                client.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new UserFriendlyException(ErrorCode.NotResponse);
            }

            _context.Customers.Update(accountReset);
            await _context.SaveChangesAsync();
        }

        public async Task<UserInfor> GetUserInforByClaimAsync(ClaimsPrincipal user)
        {
            string email = user.FindFirst(ClaimTypes.Email).Value;
            var userInfor = new UserInfor();
            if (email == null)
                return userInfor;

            string role = user.FindFirst(ClaimTypes.Role).Value;
            role =  char.ToUpper(role[0]) + role.Substring(1);
            Enum.TryParse(role, out TypeAcc enumRole);

            switch (enumRole)
            {
                case TypeAcc.Customer:
                    userInfor = await _context.Customers
                                        .Where(x => x.Email == email && !x.IsDeleted)
                                        .Select(x => new UserInfor
                                        {
                                            Id = x.Id,
                                            FullName = x.FullName,
                                            Email = x.Email,
                                            PhoneNumber = x.PhoneNumber,
                                            Address = x.Address,
                                            Avatar = x.Avatar,
                                            TypeAcc = x.TypeAcc
                                        })
                                        .FirstOrDefaultAsync();
                    break;
                case TypeAcc.Shipper:
                    userInfor = await _context.Shippers
                                        .Where(x => x.Email == email && !x.IsDeleted)
                                        .Select(x => new UserInfor
                                        {
                                            Id = x.Id,
                                            FullName = x.FullName,
                                            Email = x.Email,
                                            PhoneNumber = x.PhoneNumber,
                                            Address = x.Address,
                                            Avatar = x.Avatar,
                                            TypeAcc = x.TypeAcc
                                        })
                                        .FirstOrDefaultAsync();
                    break;
                case TypeAcc.Staff:
                    userInfor = await _context.Staffs
                                        .Where(x => x.Email == email && !x.IsDeleted && x.TypeAcc == TypeAcc.Staff)
                                        .Select(x => new UserInfor
                                        {
                                            Id = x.Id,
                                            FullName = x.FullName,
                                            Email = x.Email,
                                            PhoneNumber = x.PhoneNumber,
                                            Address = x.Address,
                                            Avatar = x.Avatar,
                                            TypeAcc = x.TypeAcc
                                        })
                                        .FirstOrDefaultAsync();
                    break;
                default: // Admin
                    userInfor = await _context.Customers
                                        .Where(x => x.Email == email && !x.IsDeleted && x.TypeAcc == TypeAcc.Admin)
                                        .Select(x => new UserInfor
                                        {
                                            Id = x.Id,
                                            FullName = x.FullName,
                                            Email = x.Email,
                                            PhoneNumber = x.PhoneNumber,
                                            Address = x.Address,
                                            Avatar = x.Avatar,
                                            TypeAcc = x.TypeAcc
                                        })
                                        .FirstOrDefaultAsync();
                    break;
            }

            return userInfor;
        }

        public async Task<bool> UpdateProfileAsync(UserInfor userInfor)
        {
            switch (userInfor.TypeAcc)
            {
                case TypeAcc.Customer:
                    var customerProfile = await _context.Customers.Where(x => x.Id == userInfor.Id && !x.IsDeleted)
                                        .FirstOrDefaultAsync();

                    if (customerProfile == null)
                        throw new UserFriendlyException(ErrorCode.NotFoundUser);

                    customerProfile.FullName = userInfor.FullName;
                    customerProfile.Address = userInfor.Address;
                    customerProfile.PhoneNumber = userInfor.PhoneNumber;

                    if (userInfor.UploadAvt != null)
                    {
                        customerProfile.Avatar = await UserHelper.UploadImageAvatarHanlderAsync(userInfor.UploadAvt, _hostEnvironment);
                    }
                    _context.Customers.Update(customerProfile);

                    break;
                case TypeAcc.Shipper:
                    var shipperProfile = await _context.Shippers.Where(x => x.Id == userInfor.Id && !x.IsDeleted)
                                        .FirstOrDefaultAsync();

                    if (shipperProfile == null)
                        throw new UserFriendlyException(ErrorCode.NotFoundUser);

                    shipperProfile.FullName = userInfor.FullName;
                    shipperProfile.Address = userInfor.Address;
                    shipperProfile.PhoneNumber = userInfor.PhoneNumber;

                    if (userInfor.UploadAvt != null)
                    {
                        shipperProfile.Avatar = await UserHelper.UploadImageAvatarHanlderAsync(userInfor.UploadAvt, _hostEnvironment);
                    }
                    _context.Shippers.Update(shipperProfile);

                    break;
                default: // Admin
                    var staffProfile = await _context.Staffs.Where(x => x.Id == userInfor.Id && !x.IsDeleted && x.TypeAcc == userInfor.TypeAcc)
                                        .FirstOrDefaultAsync();

                    if (staffProfile == null)
                        throw new UserFriendlyException(ErrorCode.NotFoundUser);

                    staffProfile.FullName = userInfor.FullName;
                    staffProfile.Address = userInfor.Address;
                    staffProfile.PhoneNumber = userInfor.PhoneNumber;

                    if (userInfor.UploadAvt != null)
                    {
                        staffProfile.Avatar = await UserHelper.UploadImageAvatarHanlderAsync(userInfor.UploadAvt, _hostEnvironment);
                    }
                    _context.Staffs.Update(staffProfile);

                    break;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
