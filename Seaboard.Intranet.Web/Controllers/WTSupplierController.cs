using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;
using System;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Seaboard.Intranet.Data.Repository;

namespace Seaboard.Intranet.Web.Controllers
{
    [Authorize]
    public class WtSupplierController : Controller
    {
        private readonly SeaboContext _db;
        private GenericRepository _repository;

        public WtSupplierController()
        {
            _db = new SeaboContext();
            _repository = new GenericRepository(_db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "WTSupplier", "Index"))
                return RedirectToAction("NotPermission", "Home");

            string sqlQuery = "SELECT DISTINCT " +
                              "LTRIM(RTRIM(A.TransferId)) AS[TransferId], " +
                              "LTRIM(RTRIM(A.supplierId)) AS[supplierId], " +
                              "CONVERT(DATE,A.Date) AS [Date], " +
                              "(CASE  WHEN A.ValueDate = '1900-01-01 00:00:00.000' THEN NULL ELSE CONVERT(DATE,A.ValueDate) END) AS[ValueDate], " +
                              "LTRIM(RTRIM(ISNULL(A.Location, ''))) AS[Location], " +
                              "LTRIM(RTRIM(ISNULL(A.Subject, ''))) AS[Subject], " +
                              "LTRIM(RTRIM(ISNULL(B.VENDNAME,''))) AS[Vendname], " +
                              "LTRIM(RTRIM(A.BankName)) AS[BankName], " +
                              "LTRIM(RTRIM(A.ABANumber)) AS[ABANumber], " +
                              "LTRIM(RTRIM(A.SwiftOrBankCod)) AS[SwiftOrBankCod], " +
                              "LTRIM(RTRIM(ISNULL(A.Address, ''))) AS[Address], " +
                              "LTRIM(RTRIM(A.AccountName)) AS[AccountName], " +
                              "LTRIM(RTRIM(A.AccountNumber)) AS[AccountNumber], " +
                              "LTRIM(RTRIM(ISNULL(A.City_State, ''))) AS[City_State], " +
                              "LTRIM(RTRIM(ISNULL(A.FurtherCredit, ''))) AS[FurtherCredit], " +
                              "LTRIM(RTRIM(ISNULL(A.ABANumber2, ''))) AS[ABANumber2], " +
                              " '' AS[Amount], " +
                              "LTRIM(RTRIM(ISNULL(A.wireReference, ''))) AS[wireReferences] " +
                              "FROM " + Helpers.InterCompanyId + ".dbo.TRANSFER_AUTHORIZATION A " +
                              "LEFT JOIN (SELECT VENDORID, VENDNAME FROM " + Helpers.InterCompanyId +
                              ".dbo.PM10300 UNION ALL " +
                              "SELECT VENDORID, VNDCHKNM AS[VENDNAME] FROM " + Helpers.InterCompanyId +
                              ".dbo.PM30200) B ON A.supplierId = B.VENDORID";

            var transferRequestList = _repository.ExecuteQuery<WtSupplierViewModel>(sqlQuery);

            return View(transferRequestList);
        }

        public ActionResult Edit(string id)
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "WTSupplier", "Index"))
                return RedirectToAction("NotPermission", "Home");

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            string sqlQuery = "SELECT " +
                              "LTRIM(RTRIM(A.TransferId)) AS[TransferId], " +
                              "LTRIM(RTRIM(A.supplierId)) AS[supplierId], " +
                              "CONVERT(DATE,A.Date) AS [Date], " +
                              "(CASE  WHEN A.ValueDate = '1900-01-01 00:00:00.000' THEN NULL ELSE CONVERT(DATE,A.ValueDate) END) AS[ValueDate], " +
                              "LTRIM(RTRIM(ISNULL(A.Location, ''))) AS[Location], " +
                              "LTRIM(RTRIM(ISNULL(A.Subject, ''))) AS[Subject], " +
                              "LTRIM(RTRIM(A.BankName)) AS[BankName], " +
                              "LTRIM(RTRIM(A.ABANumber)) AS[ABANumber], " +
                              "LTRIM(RTRIM(A.SwiftOrBankCod)) AS[SwiftOrBankCod], " +
                              "LTRIM(RTRIM(ISNULL(A.Address, ''))) AS[Address], " +
                              "LTRIM(RTRIM(A.AccountName)) AS[AccountName], " +
                              "LTRIM(RTRIM(A.AccountNumber)) AS[AccountNumber], " +
                              "LTRIM(RTRIM(ISNULL(A.City_State, ''))) AS[City_State], " +
                              "LTRIM(RTRIM(ISNULL(A.FurtherCredit, ''))) AS[FurtherCredit], " +
                              "LTRIM(RTRIM(ISNULL(A.ABANumber2, ''))) AS[ABANumber2], " +
                              "LTRIM(RTRIM(ISNULL(A.wireReference, ''))) AS[wireReferences] " +
                              "FROM " + Helpers.InterCompanyId + ".dbo.TRANSFER_AUTHORIZATION A " +
                              "WHERE A.TransferId = '" + id + "'";

            WtSupplierRequest transf = _repository.ExecuteScalarQuery<WtSupplierRequest>(sqlQuery);
            if (transf == null)
                return HttpNotFound();

            return View(transf);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TransferId,supplierId,Date,Location,Subject,BankName,ABANumber,SwiftOrBankCod,Address,City_State,AccountName,AccountNumber,FurtherCredit,ABANumber2,wireReferences,ValueDate")]WtSupplierRequest transf)
        {
            if (ModelState.IsValid)
            {
                _repository.ExecuteCommand(String.Format(
                    "INTRANET.dbo.TRANSFER_AUTHO_UPDATE '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}'",
                    Helpers.InterCompanyId, transf.TransferId, transf.SupplierId, transf.Date, transf.Location,
                    transf.Subject, transf.BankName, transf.AbaNumber, transf.SwiftOrBankCod, transf.Address,
                    transf.CityState,
                    transf.AccountName, transf.AccountNumber, transf.FurtherCredit, transf.AbaNumber2,
                    transf.WireReferences, transf.ValueDate));

                return RedirectToAction("Index");
            }

            return View(transf);
        }

        public ActionResult Delete(string id)
        {
            try
            {
                _repository.ExecuteCommand(String.Format("INTRANET.dbo.TRANSFER_AUTHO_DELETE '{0}','{1}'",
                    Helpers.InterCompanyId, id));

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return View(ex.Message);
            }
        }

        public ActionResult Create()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "WTSupplier",
                "Index"))
            {
                return RedirectToAction("NotPermission", "Home");
            }

            return View();
        }

        [HttpPost]
        public ActionResult Create(WtSupplierViewModel transf)
        {
            int status = 0;

            try
            {
                string query = "SELECT TOP 1 COUNT(AccountNumber) FROM " + Helpers.InterCompanyId
                             + ".dbo.TRANSFER_AUTHORIZATION "
                             + "WHERE AccountNumber = '" + transf.AccountNumber + "'";
                status = _repository.ExecuteScalarQuery<int>(query);

                if (status == 0)
                {
                    if (ModelState.IsValid)
                    {
                        _repository.ExecuteCommand(
                            String.Format(
                                "INTRANET.dbo.TRANSFER_AUTHO_INSERT '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}'",
                                Helpers.InterCompanyId, transf.SupplierId, transf.Date, transf.Location, transf.Subject,
                                transf.BankName, transf.AbaNumber, transf.SwiftOrBankCod, transf.Address,
                                transf.CityState,
                                transf.AccountName, transf.AccountNumber, transf.FurtherCredit, transf.AbaNumber2,
                                transf.WireReferences, transf.ValueDate));
                    }
                }
            }
            catch
            {

            }

            return new JsonResult { Data = new { status } };
        }

        public ActionResult Print(string vendid, string accountNum, string paymentid)
        {
            string transferid = "";
            try
            {
                string query = "SELECT LTRIM(RTRIM(A.TransferId)) AS [TransferId] FROM " + Helpers.InterCompanyId +
                               ".dbo.TRANSFER_AUTHORIZATION A inner join " +
                               "(SELECT PMNTNMBR, VENDORID FROM " + Helpers.InterCompanyId + ".dbo.PM10300 " +
                               "UNION ALL SELECT VCHRNMBR, VENDORID FROM " + Helpers.InterCompanyId +
                               ".dbo.PM30200) B " +
                               "ON A.supplierId = B.VENDORID WHERE B.VENDORID = '" + vendid + "' " +
                               "AND A.AccountNumber = '" + accountNum + "' AND B.PMNTNMBR = '" + paymentid + "'";

                TransferbyId id = _repository.ExecuteScalarQuery<TransferbyId>(query);

                transferid = id.TransferId + "" + paymentid;

                ReportHelper.Export(Helpers.ReportPath + "Transferencia",
                    Server.MapPath("~/PDF/Transferencia/") + transferid + ".pdf",
                    String.Format("INTRANET.dbo.TR000000Rpt '{0}','{1}','{2}','{3}'", Helpers.InterCompanyId, vendid,
                        accountNum, paymentid), 11, ref transferid);
            }
            catch (Exception ex)
            {
                return View(ex.Message);
            }
            return File("~/PDF/Transferencia/" + transferid + ".pdf", "application/pdf");
        }
    }
}