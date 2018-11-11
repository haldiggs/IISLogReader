﻿using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using IISLogReader.BLL.Models;
using IISLogReader.BLL.Data.Stores;
using IISLogReader.BLL.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemWrapper.IO;

namespace Test.IISLogReader.BLL.Data.Stores
{
    [TestFixture]
    public class UserStoreTest
    {
        private IUserStore _userStore;
        private IFileWrap _fileWrap;
        private IDirectoryWrap _dirWrap;
        private IPasswordProvider _passwordProvider;
        private string _path = "dummypath";

        [SetUp]
        public void UserStoreTest_SetUp()
        {
            _fileWrap = Substitute.For<IFileWrap>();
            _dirWrap = Substitute.For<IDirectoryWrap>();
            _passwordProvider = Substitute.For<IPasswordProvider>();

            _userStore = new UserStore(_path, _fileWrap, _dirWrap, _passwordProvider);
        }

        #region Load Tests

        [Test]
        public void Load_FileExists_LoadsAndPopulatesStore()
        {
            // setup
            UserModel user = new UserModel();
            user.Id = Guid.NewGuid();
            user.UserName = "test";
            user.Password = "password";
            user.Role = Roles.Admin;

            byte[] key = new byte[20];
            new Random().NextBytes(key);

            UserStore store = new UserStore(_path, _fileWrap, _dirWrap, _passwordProvider);
            store.Users.Add(user);
            string sUsers = JsonConvert.SerializeObject(store);

            _fileWrap.Exists(_path).Returns(true);
            _fileWrap.ReadAllText(_path).Returns(sUsers);

            // execute
            _userStore.Load();

            // assert
            _fileWrap.Received(1).Exists(_path);
            _fileWrap.Received(1).ReadAllText(_path);
            Assert.AreEqual(1, _userStore.Users.Count);
            Assert.AreEqual(user.Id, _userStore.Users[0].Id);
            Assert.AreEqual(user.UserName, _userStore.Users[0].UserName);
            Assert.AreEqual(Roles.Admin, _userStore.Users[0].Role);
            Assert.IsNotNull(_userStore.Users[0].Password);
            Assert.IsNotEmpty(_userStore.Users[0].Password);
        }

        [Test]
        public void Load_FileDoesNotExist_CollectionCreatedWithAdminUser()
        {
            // setup
            _fileWrap.Exists(_path).Returns(false);

            // execute
            _userStore.Load();

            // assert
            _fileWrap.Received(1).Exists(_path);
            _fileWrap.DidNotReceive().ReadAllText(Arg.Any<String>());

            _passwordProvider.Received(1).HashPassword("admin", Arg.Any<string>());
            _passwordProvider.Received(1).GenerateSalt();

            Assert.AreEqual(1, _userStore.Users.Count);
            Assert.AreEqual("admin", _userStore.Users[0].UserName);

        }

        #endregion

        #region Save Tests

        [Test]
        public void Save_OnExecute_SavesContentsToDisk()
        {
            // setup 
            Random r = new Random();

            List<UserModel> users = new List<UserModel>();
            for (int i = 0; i < r.Next(3, 7); i++)
            {
                _userStore.Users.Add(new UserModel()
                {
                    Id = Guid.NewGuid(),
                    Password = Guid.NewGuid().ToString(),
                    UserName = Guid.NewGuid().ToString(),
                    Role = Guid.NewGuid().ToString(),
                });
            }

            // note that this doesn't actually map to a real drive on the current machine!
            _userStore.FilePath = "C:\\Temp\\Test\\users.json";

            // execute 
            _userStore.Save();

            // assert
            string contents = JsonConvert.SerializeObject(_userStore, Formatting.Indented);
            _dirWrap.Received(1).CreateDirectory("C:\\Temp\\Test");
            _fileWrap.Received(1).WriteAllText(_userStore.FilePath, contents);
        }
        #endregion
    }
}
