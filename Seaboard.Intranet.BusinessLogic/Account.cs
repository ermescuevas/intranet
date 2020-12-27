using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using Seaboard.Intranet.Data.Repository;
using Seaboard.Intranet.Domain;

namespace Seaboard.Intranet.BusinessLogic
{
    public class Account
    {
        public static User GetAccount(string userId)
        {
            var db = new SeaboContext();

            GenericRepository repository = new GenericRepository(db);
            User user = repository.GetAll<User>(u => u.UserName == userId).FirstOrDefault();

            return user;
        }

        public static List<EmployeeDigitalDocument> GetDocumentForSign(string userId)
        {
            var db = new SeaboContext();
            GenericRepository repository = new GenericRepository(db);
            User user = repository.GetAll<User>(u => u.UserName == userId).FirstOrDefault();
            var list = repository.ExecuteQuery<EmployeeDigitalDocument>($"SELECT A.DocumentId, B.[Name], A.EmployeeId FROM {Helpers.InterCompanyId}.dbo.EHUPR20100 A " +
                $"INNER JOIN {Helpers.InterCompanyId}.dbo.EHUPR10100 B ON A.DocumentId = B.DocumentId WHERE A.Status = 0 AND A.EmployeeId = '{user.EmployeeId}'").ToList();
            return list;
        }

        public static List<Permission> GetAccountPermission(string userId)
        {
            var db = new SeaboContext();
            var repository = new GenericRepository(db);
            string sqlQuery = $"SELECT A.* FROM PERMISSIONS A " +
                $"INNER JOIN USERPERMISSIONS B ON A.PermissionId = B.PermissionId " +
                $"INNER JOIN USERS C ON B.UserId = C.UserId " +
                $"WHERE C.UserName = '{userId}'";
            var permissions = repository.ExecuteQuery<Permission>(sqlQuery).ToList();

            return permissions;
        }
    }
}
