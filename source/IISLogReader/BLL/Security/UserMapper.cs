﻿using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using IISLogReader.BLL.Models;
using IISLogReader.BLL.Data.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISLogReader.BLL.Security
{
    public class UserMapper : IUserMapper
    {
        private IUserStore _userStore;

        public UserMapper(IUserStore userStore)
        {
            this._userStore = userStore;
        }

        public virtual IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            UserIdentity ui = null;
            UserModel user = _userStore.Users.SingleOrDefault(x => x.Id == identifier);
            if (user != null)
            {
                ui = new UserIdentity();
                ui.Id = user.Id;
                ui.UserName = user.UserName;
                if (user.Role == Roles.Admin)
                {
                    ui.Claims = Claims.AllClaims;
                }
                else
                {
                    ui.Claims = user.Claims;
                }
            }
            return ui;
        }
    }
}
