﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISLogReader.BLL.Security
{
    public static class Claims
    {
        static Claims()
        {
            AllClaims = new List<string>(new string[] 
            { 
                ConnectionAdd, 
                ConnectionDelete,
                ProjectSave,
                UserAdd,
                UserDelete,
                UserList
            }).AsReadOnly();
        }

        public const string ConnectionAdd = "ConnectionAdd";

        public const string ConnectionDelete = "ConnectionDelete";

        public const string UserAdd = "UserAdd";

        public const string UserDelete = "UserDelete";

        public const string UserList = "UserList";

        public const string ProjectSave = "ProjectSave";

        public static IReadOnlyList<string> AllClaims { get; private set; }
    }
}
