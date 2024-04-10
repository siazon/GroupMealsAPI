using Microsoft.VisualStudio.TestTools.UnitTesting;
using App.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App.Domain.Common.Customer;

namespace App.Infrastructure.Repository.Tests
{
    [TestClass()]
    public class DbRepository_newTests
    {
        [TestMethod()]
        public void testTest()
        {
            IDbRepository_new<DbCustomer> dbRepository_New =new Repository.DbRepositoryV3<DbCustomer>();
            dbRepository_New.SetUpConnection("https://wiiyadevelop.documents.azure.com:443/", "X8SlC0zDFDE91DguIZ8XVSytAoburu2mZczsnKVgVYKHj4I7fnC9JdE7lLb6TeRcUU7OKxDXinW0KNzvEDMKfA==", "wiiya");
            dbRepository_New.GetOneAsync(r=>r.Id=="");
        }
    }
}