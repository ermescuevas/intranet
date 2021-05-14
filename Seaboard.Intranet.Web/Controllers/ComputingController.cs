using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Data.Repository;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using Seaboard.Intranet.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Seaboard.Intranet.Web
{
    [Authorize]
    public class ComputingController : Controller
    {
        private readonly GenericRepository _repository;
        public ComputingController()
        {
            var db = new SeaboContext();
            _repository = new GenericRepository(db);
        }

        public ActionResult Index()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "Index"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        #region Equipos

        public ActionResult EquipmentIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "Equipment"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = $"SELECT AcquireMode, AsignedUser, Brand, CanUseData, Cost, DeliveryDate, Description, MobileTechnology, Model, Operator, PhoneNumber, " +
                    $"SerialNumber, SimCard, Status, DeviceCode, AcquiredDate, RTRIM(C.DSCRIPTN) Department " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIIV00101 A " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.UPR00100  B ON SUBSTRING(A.AsignedUser, 1, 6) = RTRIM(B.EMPLOYID) " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.UPR40300  C ON RTRIM(B.DEPRTMNT) = RTRIM(C.DEPRTMNT) ";
            var equipments = _repository.ExecuteQuery<Equipment>(sqlQuery).ToList();
            return View(equipments);
        }

        public ActionResult Equipment(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "Equipment"))
                return RedirectToAction("NotPermission", "Home");
            Equipment equipment;
            if (string.IsNullOrEmpty(id))
                equipment = new Equipment()
                {
                    Status = 6,
                    DeviceCode = HelperLogic.AsignaciónSecuencia("EIIV00101", Account.GetAccount(User.Identity.GetUserName()).UserId),
                    AcquiredDate = DateTime.Now,
                    DeliveryDate = new DateTime(1900, 1, 1),
                    UsedPoints = 0
                };
            else
            {
                string sqlQuery = $"SELECT AcquireMode, AsignedUser, Brand, CanUseData, Cost, DeliveryDate, Description, MobileTechnology, Model, Operator, PhoneNumber, UsedPoints, " +
                    $"SerialNumber, SimCard, Status, DeviceCode, AcquiredDate " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WHERE DeviceCode = '{id}'";
                equipment = _repository.ExecuteScalarQuery<Equipment>(sqlQuery);
            }
            var brands = new List<SelectListItem>()
            {
                new SelectListItem { Value = "samsung", Text = "Samsung" },
                new SelectListItem { Value = "apple", Text = "Apple" },
                new SelectListItem { Value = "lg", Text = "LG" },
                new SelectListItem { Value = "motorola", Text = "Motorola" },
                new SelectListItem { Value = "huawei", Text = "Huawei" },
                new SelectListItem { Value = "nokia", Text = "Nokia" },
                new SelectListItem { Value = "alcatel", Text = "Alcatel" },
                new SelectListItem { Value = "htc", Text = "HTC" },
                new SelectListItem { Value = "blackberry", Text = "BlackBerry" },
                new SelectListItem { Value = "zte", Text = "ZTE" },
                new SelectListItem { Value = "BLU", Text = "BLU" },
                new SelectListItem { Value = "microsoft", Text = "Microsoft" },
                new SelectListItem { Value = "xiaomi", Text = "Xiaomi" },
                new SelectListItem { Value = "altice", Text = "Altice" },
                new SelectListItem { Value = "otro", Text = "Otros" }
            };
            var operators = new List<SelectListItem>()
            {
                new SelectListItem { Value = "claro", Text = "CLARO" },
                new SelectListItem { Value = "altice", Text = "ALTICE" },
                new SelectListItem { Value = "viva", Text = "VIVA" },
                new SelectListItem { Value = "wind", Text = "Wind Telecom" },
                new SelectListItem { Value = "desbloqueado", Text = "DESBLOQUEADO" }
            };
            var technologies = new List<SelectListItem>()
            {
                new SelectListItem { Value = "1", Text = "3G" },
                new SelectListItem { Value = "2", Text = "4G" },
                new SelectListItem { Value = "3", Text = "5G" }
            };
            var status = new List<SelectListItem>()
            {
                new SelectListItem { Value = "1", Text = "Asignado" },
                new SelectListItem { Value = "2", Text = "Prestado" },
                new SelectListItem { Value = "3", Text = "Perdido" },
                new SelectListItem { Value = "4", Text = "Dañado" },
                new SelectListItem { Value = "5", Text = "En Reparacion" },
                new SelectListItem { Value = "6", Text = "En stock" }
            };
            ViewBag.Brands = brands;
            ViewBag.Operators = operators;
            ViewBag.Technologies = technologies;
            ViewBag.Status = status;
            return View(equipment);
        }

        [HttpPost]
        public JsonResult SaveEquipment(Equipment equipment)
        {
            string xStatus;
            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WHERE DeviceCode = '{equipment.DeviceCode}'");
                if (count > 0)
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00101 SET AcquireMode = '{Convert.ToInt32(equipment.AcquireMode)}', AsignedUser = '{equipment.AsignedUser}', Brand = '{equipment.Brand}', " +
                        $"CanUseData = '{equipment.CanUseData}', Cost = '{equipment.Cost}', Description = '{equipment.Description}', MobileTechnology = '{equipment.MobileTechnology}', Model = '{equipment.Model}', " +
                        $"Operator = '{equipment.Operator}', UsedPoints = '{equipment.UsedPoints}', PhoneNumber = '{equipment.PhoneNumber}', SerialNumber = '{equipment.SerialNumber}', SimCard = '{equipment.SimCard}', Status = '{equipment.Status}', " +
                        $"AcquiredDate = '{equipment.AcquiredDate.ToString("yyyyMMdd")}', DeliveryDate = '{equipment.DeliveryDate.ToString("yyyyMMdd")}', ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                        $"WHERE DeviceCode = '{equipment.DeviceCode}'");
                else
                {
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.EIIV00101 (AcquireMode, AsignedUser, Brand, CanUseData, Cost, Description, MobileTechnology, Model, " +
                        $"Operator, PhoneNumber, SerialNumber, SimCard, Status, DeviceCode, AcquiredDate, DeliveryDate, UsedPoints, CreatedDate, ModifiedDate, LastUserId) " +
                        $"VALUES ('{Convert.ToInt32(equipment.AcquireMode)}', '{equipment.AsignedUser}', '{equipment.Brand}', '{equipment.CanUseData}', '{equipment.Cost}', " +
                        $"'{equipment.Description}', '{equipment.MobileTechnology}', '{equipment.Model}', '{equipment.Operator}', '{equipment.PhoneNumber}', '{equipment.SerialNumber}', '{equipment.SimCard}', " +
                        $"'{equipment.Status}', '{equipment.DeviceCode}', '{equipment.AcquiredDate.ToString("yyyyMMdd")}', '{equipment.DeliveryDate.ToString("yyyyMMdd")}', '{equipment.UsedPoints}', " +
                        $"GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
                    if (equipment.AcquireMode == MobileAcquireMode.Fidepuntos)
                        SaveEarnedPointEntity(new EarnedPoint
                        {
                            Description = "Puntos redimidos de la compra del equipo " + equipment.Model,
                            DocumentDate = DateTime.Now,
                            Points = equipment.UsedPoints,
                            SummaryType = 1
                        });
                }
                if (!string.IsNullOrEmpty(equipment.SimCard))
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00201 SET AsignedEquipment = '{equipment.DeviceCode + " - " + equipment.Model}' WHERE SimCardCode = '{equipment.SimCard}'");

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteEquipment(string id)
        {
            string xStatus;
            try
            {
                string sqlQuery = $"SELECT AcquireMode, AsignedUser, Brand, CanUseData, Cost, DeliveryDate, Description, MobileTechnology, Model, Operator, PhoneNumber, " +
                    $"SerialNumber, SimCard, Status, DeviceCode, AcquiredDate " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WHERE DeviceCode = '{id}'";
                var equipment = _repository.ExecuteScalarQuery<Equipment>(sqlQuery);
                if (!string.IsNullOrEmpty(equipment.AsignedUser))
                    xStatus = "Este equipo esta asignado a un usuario, no se puede eliminar equipos asignados, vaya al formulario de desasignacion para proceder";
                else
                {
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EIIV00101 WHERE DeviceCode = '{id}'");
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00201 SET AsignedEquipment = '' WHERE SimCardCode = '{equipment.SimCard}'");
                    xStatus = "OK";
                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Tarjeta SIM

        public ActionResult SimCardIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "SimCard"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = $"SELECT DISTINCT A.SimCardCode, A.SerialNumber, A.Operator, A.PhoneNumber, A.AcquiredDate, A.HasData, A.DataQuantity, A.MinuteOpen, A.QuantityMinutes, A.AsignedEquipment, " +
                   $"A.DeliveryDate, A.ChangePoints, A.Status, B.AsignedUser AssignedUser, RTRIM(D.DSCRIPTN) Department " +
                   $"FROM {Helpers.InterCompanyId}.dbo.EIIV00201 A " +
                   $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EIIV00101 B ON A.SimCardCode = B.SimCard " +
                   $"LEFT JOIN {Helpers.InterCompanyId}.dbo.UPR00100  C ON SUBSTRING(B.AsignedUser, 1, 6) = RTRIM(C.EMPLOYID) " +
                   $"LEFT JOIN {Helpers.InterCompanyId}.dbo.UPR40300  D ON RTRIM(C.DEPRTMNT) = RTRIM(D.DEPRTMNT) ";
            var simCards = _repository.ExecuteQuery<SimCard>(sqlQuery).ToList();
            return View(simCards);
        }

        public ActionResult SimCard(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "SimCard"))
                return RedirectToAction("NotPermission", "Home");
            SimCard simCard;
            if (string.IsNullOrEmpty(id))
                simCard = new SimCard()
                {
                    Status = 2,
                    SimCardCode = HelperLogic.AsignaciónSecuencia("EIIV00201", Account.GetAccount(User.Identity.GetUserName()).UserId),
                    AcquiredDate = DateTime.Now,
                    DeliveryDate = new DateTime(1900, 1, 1)
                };
            else
            {
                string sqlQuery = $"SELECT A.SimCardCode, A.SerialNumber, A.Operator, A.PhoneNumber, A.AcquiredDate, A.HasData, A.DataQuantity, A.MinuteOpen, A.QuantityMinutes, A.AsignedEquipment, " +
                   $"A.DeliveryDate, A.ChangePoints, A.Status, B.AsignedUser AssignedUser, RTRIM(D.DSCRIPTN) Department " +
                   $"FROM {Helpers.InterCompanyId}.dbo.EIIV00201 A " +
                   $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EIIV00101 B ON A.SimCardCode = B.SimCard " +
                   $"LEFT JOIN {Helpers.InterCompanyId}.dbo.UPR00100  C ON SUBSTRING(B.AsignedUser, 1, 6) = RTRIM(C.EMPLOYID) " +
                   $"LEFT JOIN {Helpers.InterCompanyId}.dbo.UPR40300  D ON RTRIM(C.DEPRTMNT) = RTRIM(D.DEPRTMNT) " +
                   $"WHERE SimCardCode = '{id}'";
                simCard = _repository.ExecuteScalarQuery<SimCard>(sqlQuery);
            }
            var operators = new List<SelectListItem>()
            {
                new SelectListItem { Value = "claro", Text = "CLARO" },
                new SelectListItem { Value = "altice", Text = "ALTICE" },
                new SelectListItem { Value = "viva", Text = "VIVA" },
                new SelectListItem { Value = "wind", Text = "WIND" }
            };
            var status = new List<SelectListItem>()
            {
                new SelectListItem { Value = "1", Text = "Asignado" },
                new SelectListItem { Value = "2", Text = "En stock" },
                new SelectListItem { Value = "3", Text = "Dañado" },
                new SelectListItem { Value = "4", Text = "Perdido" }
            };
            var minute = new List<SelectListItem>()
            {
                new SelectListItem { Value = "Abierta", Text = "Abierta" },
                new SelectListItem { Value = "Cerrada", Text = "Cerrada" }
            };
            ViewBag.Operators = operators;
            ViewBag.Status = status;
            ViewBag.Minute = minute;
            return View(simCard);
        }

        [HttpPost]
        public JsonResult SaveSimCard(SimCard simCard)
        {
            string xStatus;
            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EIIV00201 WHERE SimCardCode = '{simCard.SimCardCode}'");
                if (count > 0)
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00201 SET ChangePoints = '{Convert.ToInt32(simCard.ChangePoints)}', AsignedEquipment = '{simCard.AsignedEquipment}', " +
                        $"HasData = '{simCard.HasData}', DataQuantity = '{simCard.DataQuantity}', MinuteOpen = '{simCard.MinuteOpen}', QuantityMinutes = '{simCard.QuantityMinutes}', " +
                        $"Operator = '{simCard.Operator}', PhoneNumber = '{simCard.PhoneNumber}', SerialNumber = '{simCard.SerialNumber}', Status = '{simCard.Status}', " +
                        $"AcquiredDate = '{simCard.AcquiredDate.ToString("yyyyMMdd")}', ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                        $"WHERE SimCardCode = '{simCard.SimCardCode}'");
                else
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.EIIV00201 (ChangePoints, AsignedEquipment, HasData, DataQuantity, MinuteOpen, AcquiredDate, QuantityMinutes,  " +
                        $"Operator, PhoneNumber, SerialNumber, Status, SimCardCode, CreatedDate, ModifiedDate, LastUserId) " +
                        $"VALUES ('{Convert.ToInt32(simCard.ChangePoints)}', '{simCard.AsignedEquipment}', '{simCard.HasData}', '{simCard.DataQuantity}', '{simCard.MinuteOpen}', '{simCard.AcquiredDate.ToString("yyyyMMdd")}', " +
                        $"'{simCard.QuantityMinutes}', '{simCard.Operator}', '{simCard.PhoneNumber}', '{simCard.SerialNumber}', '{simCard.Status}', '{simCard.SimCardCode}', GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteSimCard(string id)
        {
            string xStatus;
            try
            {
                string sqlQuery = $"SELECT SimCardCode, SerialNumber, Operator, PhoneNumber, AcquiredDate, HasData, DataQuantity, MinuteOpen, QuantityMinutes, AsignedEquipment, " +
                  $"DeliveryDate, ChangePoints, Status " +
                  $"FROM {Helpers.InterCompanyId}.dbo.EIIV00201 WHERE SimCardCode = '{id}'";
                var equipment = _repository.ExecuteScalarQuery<SimCard>(sqlQuery);
                if (!string.IsNullOrEmpty(equipment.AsignedEquipment))
                    xStatus = "Este equipo esta asignado a un equipo, no se puede eliminar equipos asignados, vaya al formulario de desasignacion para proceder";
                else
                {
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EIIV00201 WHERE SimCardCode = '{id}'");
                    xStatus = "OK";
                }
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Attachments

        [HttpPost]
        public JsonResult AttachFile(HttpPostedFileBase fileData, string id)
        {
            bool status = false;

            try
            {
                byte[] fileStream = null;
                using (var binaryReader = new BinaryReader(fileData.InputStream))
                    fileStream = binaryReader.ReadBytes(fileData.ContentLength);

                string fileName = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].ToString();
                string fileType = fileData.FileName.Split('\\')[fileData.FileName.Split('\\').Count() - 1].Split('.')[1].ToString();

                _repository.ExecuteCommand(String.Format("INTRANET.dbo.AttachmentInsert '{0}','{1}','{2}',{3},'{4}','{5}','{6}'",
                    Helpers.InterCompanyId, id, fileName, "0x" + BitConverter.ToString(fileStream).Replace("-", String.Empty),
                    fileType, Account.GetAccount(User.Identity.GetUserName()).UserId, "NOTAS"));
                status = true;
            }
            catch
            {
                status = false;
            }

            return new JsonResult { Data = new { status = status } };
        }

        public class AttachmentViewModel
        {
            public HttpPostedFileBase FileData { get; set; }
        }

        [HttpPost]
        public ActionResult LoadAttachmentFiles(string id)
        {
            try
            {
                List<string> files = new List<string>();
                string sqlQuery = "SELECT RTRIM(fileName) FileName FROM " + Helpers.InterCompanyId + ".dbo.CO00105 WHERE DOCNUMBR = '" + id + "' AND DELETE1 = 0";
                files = _repository.ExecuteQuery<string>(sqlQuery).ToList();
                return Json(files, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public ActionResult DeleteFile(string id, string fileName)
        {
            string status;
            try
            {
                string sqlQuery = "UPDATE " + Helpers.InterCompanyId + ".dbo.CO00105 SET DELETE1 = 1 WHERE DOCNUMBR = '" + id + "' AND RTRIM(fileName) = '" + fileName + "'";
                _repository.ExecuteCommand(sqlQuery);

                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status = status } };
        }

        public ActionResult Download(string id, string FileName)
        {
            string sqlQuery = "SELECT BinaryBlob, FileType, A.fileName "
                              + "FROM " + Helpers.InterCompanyId + ".dbo.CO00105 A "
                              + "INNER JOIN " + Helpers.InterCompanyId +
                              ".dbo.coAttachmentItems B ON A.Attachment_ID = B.Attachment_ID "
                              + "WHERE A.DOCNUMBR = '" + id + "' AND A.DELETE1 = 0 AND RTRIM(A.fileName) = '" +
                              FileName + "'";

            DataTable adjunto = ConnectionDb.GetDt(sqlQuery);
            byte[] contents = null;
            string fileType = "";
            string fileName = "";

            if (adjunto.Rows.Count > 0)
            {
                contents = (byte[])adjunto.Rows[0][0];
                fileType = adjunto.Rows[0][1].ToString();
                fileName = adjunto.Rows[0][2].ToString();
            }

            return File(contents, fileType.Trim(), fileName.Trim());
        }

        #endregion

        #region Solicitud de equipos

        public ActionResult EquipmentRequestIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentRequest"))
                return RedirectToAction("NotPermission", "Home");
            var sqlQuery = "SELECT DISTINCT RTRIM(A.DEPRTMDS) FROM " + Helpers.InterCompanyId + ".dbo.LPPOP40100 A "
                              + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 B ON A.DEPRTMID = B.DEPRTMID "
                              + "WHERE RTRIM(B.USERID) = '" + Account.GetAccount(User.Identity.GetUserName()).UserId + "'";
            var filter = "";

            var departments = _repository.ExecuteQuery<string>(sqlQuery).ToArray();

            foreach (var item in departments)
                if (filter.Length == 0)
                    filter = "'" + item + "'";
                else
                    filter += ",'" + item + "'";

            sqlQuery = $"SELECT RequestId, RequestType, DocumentDate, DepartmentId, Requester, RequestBy, HasData, OpenMinutes, Note, Status " +
                   $"FROM {Helpers.InterCompanyId}.dbo.EIPM10000 " +
                   $"WHERE DepartmentId IN ({filter})";
            var equipmentRequests = _repository.ExecuteQuery<EquipmentRequest>(sqlQuery).ToList();
            return View(equipmentRequests);
        }

        public ActionResult EquipmentRequest(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentRequest"))
                return RedirectToAction("NotPermission", "Home");
            EquipmentRequest equipmentRequest;
            var account = Account.GetAccount(User.Identity.GetUserName());
            if (string.IsNullOrEmpty(id))
                equipmentRequest = new EquipmentRequest()
                {
                    RequestId = HelperLogic.AsignaciónSecuencia("EIPM10000", Account.GetAccount(User.Identity.GetUserName()).UserId),
                    Status = 0,
                    DepartmentId = account.Department,
                    Requester = account.EmployeeId.Trim() + " - " + account.FirstName.Trim() + " " + account.LastName.Trim(),
                    OpenMinutes = "Abierta"
                };
            else
            {
                string sqlQuery = $"SELECT RequestId, RequestType, DocumentDate, DepartmentId, Requester, RequestBy, HasData, OpenMinutes, Note, Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIPM10000 WHERE RequestId = '{id}'";
                equipmentRequest = _repository.ExecuteScalarQuery<EquipmentRequest>(sqlQuery);
            }

            ViewBag.Logs = _repository.ExecuteQuery<EquipmentRequestLog>($"SELECT RequestId, LogDate, UserId, Note FROM {Helpers.InterCompanyId}.dbo.EIPM10001 WHERE RequestId = '{id}' ORDER BY LogDate").ToList();
            return View(equipmentRequest);
        }

        public ActionResult EquipmentRequestHandlingIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentRequest"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = $"SELECT RequestId, RequestType, DocumentDate, DepartmentId, Requester, RequestBy, HasData, OpenMinutes, Note, Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIPM10000 " +
                    $"WHERE Status > 3 ";
            var equipmentRequests = _repository.ExecuteQuery<EquipmentRequest>(sqlQuery).ToList();
            return View(equipmentRequests);
        }

        public ActionResult EquipmentRequestHandling(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentRequest"))
                return RedirectToAction("NotPermission", "Home");
            EquipmentRequest equipmentRequest;
            var account = Account.GetAccount(User.Identity.GetUserName());
            if (string.IsNullOrEmpty(id))
                equipmentRequest = new EquipmentRequest()
                {
                    RequestId = HelperLogic.AsignaciónSecuencia("EIPM10000", Account.GetAccount(User.Identity.GetUserName()).UserId),
                    Status = 0,
                    DepartmentId = account.Department,
                    Requester = account.EmployeeId.Trim() + " - " + account.FirstName.Trim() + " " + account.LastName.Trim(),
                    OpenMinutes = "Abierta"
                };
            else
            {
                string sqlQuery = $"SELECT RequestId, RequestType, DocumentDate, DepartmentId, Requester, RequestBy, HasData, OpenMinutes, Note, Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIPM10000 WHERE RequestId = '{id}'";
                equipmentRequest = _repository.ExecuteScalarQuery<EquipmentRequest>(sqlQuery);
            }

            ViewBag.Logs = _repository.ExecuteQuery<EquipmentRequestLog>($"SELECT RequestId, LogDate, UserId, Note FROM {Helpers.InterCompanyId}.dbo.EIPM10001 WHERE RequestId = '{id}' ORDER BY LogDate").ToList();
            return View(equipmentRequest);
        }

        [HttpPost]
        public JsonResult SaveEquipmentRequest(EquipmentRequest equipmentRequest, int postType = 0)
        {
            string xStatus = "";
            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EIPM10000 WHERE RequestId = '{equipmentRequest.RequestId}'");
                if (count > 0)
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10000 SET Note = '{equipmentRequest.Note}', RequestType = '{equipmentRequest.RequestType}', " +
                        $"Requester = '{equipmentRequest.Requester}', RequestBy = '{equipmentRequest.RequestBy}', HasData = '{equipmentRequest.HasData}', OpenMinutes = '{equipmentRequest.OpenMinutes}', " +
                        $"DepartmentId = '{equipmentRequest.DepartmentId}', ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                        $"WHERE RequestId = '{equipmentRequest.RequestId}'");
                else
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.EIPM10000 (RequestId, RequestType, DocumentDate, DepartmentId, Note, Requester, RequestBy, HasData, " +
                        $"OpenMinutes, Status, CreatedDate, ModifiedDate, LastUserId) " +
                        $"VALUES ('{equipmentRequest.RequestId}', '{equipmentRequest.RequestType}', '{DateTime.Now.ToString("yyyyMMdd")}', '{equipmentRequest.DepartmentId}', '{equipmentRequest.Note}', " +
                        $"'{equipmentRequest.Requester}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}', '{equipmentRequest.HasData}', '{equipmentRequest.OpenMinutes}', 1, GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");

                if (postType == 1)
                {
                    //ProcessLogic.SendToSharepoint(equipmentRequest.RequestId, 13, Account.GetAccount(User.Identity.GetUserName()).Email, ref xStatus);
                    Task.Run(() => ProcessLogic.SendToSharepointAsync(equipmentRequest.RequestId, 13, Account.GetAccount(User.Identity.GetUserName()).Email));
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10000 SET Status = 2 WHERE RequestId = '{equipmentRequest.RequestId}'");
                    LogRequest(equipmentRequest.RequestId, Account.GetAccount(User.Identity.GetUserName()).FirstName + " " + Account.GetAccount(User.Identity.GetUserName()).LastName, "Enviado a flujo de aprobacion");
                }

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteEquipmentRequest(string id)
        {
            string xStatus;
            try
            {
                string sqlQuery = $"SELECT RequestId, RequestType, DocumentDate, DepartmentId, Requester, RequestBy, HasData, OpenMinutes, Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIPM10000 WHERE RequestId = '{id}'";
                var equipment = _repository.ExecuteScalarQuery<EquipmentRequest>(sqlQuery);
                if (equipment.Status != 1)
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10000 SET Status = 5 WHERE RequestId = '{id}'");
                else
                {
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EIPM10000 WHERE RequestId = '{id}'");
                    _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EIPM10001 WHERE RequestId = '{id}'");
                }
                xStatus = "OK";

            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SendEquipmentRepair(string id)
        {
            string xStatus;
            string url = "";
            try
            {
                string sqlQuery = $"SELECT RequestId, RequestType, DocumentDate, DepartmentId, Requester, RequestBy, HasData, OpenMinutes, Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIPM10000 WHERE RequestId = '{id}'";
                var equipmentRequest = _repository.ExecuteScalarQuery<EquipmentRequest>(sqlQuery);
                var simCardCode = _repository.ExecuteScalarQuery<string>($"SELECT SimCard FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WHERE SUBSTRING(AsignedUser, 1, 6) = '{equipmentRequest.Requester.Substring(0, 6)}'");
                var simCard = _repository.ExecuteScalarQuery<string>($"SELECT SimCardCode + ' - ' + PhoneNumber FROM {Helpers.InterCompanyId}.dbo.EIIV00201 WHERE SimCardCode = '{simCardCode}'");
                var equipmentDelivery = new EquipmentRepair()
                {
                    BaseDocumentNumber = equipmentRequest.RequestId,
                    Status = 0,
                    RequestId = HelperLogic.AsignaciónSecuencia("EIPM10300", Account.GetAccount(User.Identity.GetUserName()).UserId)
                };
                xStatus = "OK";
                HttpContext.Cache["EquipmentRepair"] = equipmentDelivery;
                url = Url.Action("EquipmentRepair", "Computing", new { id = "-1" });
                LogRequest(equipmentRequest.RequestId, Account.GetAccount(User.Identity.GetUserName()).FirstName + " " + Account.GetAccount(User.Identity.GetUserName()).LastName, "Enviado a gestion de reparación de equipo");
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, url }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SendEquipmentDelivery(string id)
        {
            string xStatus;
            string url = "";
            try
            {
                string sqlQuery = $"SELECT RequestId, RequestType, DocumentDate, DepartmentId, Requester, RequestBy, HasData, OpenMinutes, Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIPM10000 WHERE RequestId = '{id}'";
                var equipmentRequest = _repository.ExecuteScalarQuery<EquipmentRequest>(sqlQuery);
                var simCardCode = _repository.ExecuteScalarQuery<string>($"SELECT SimCard FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WHERE SUBSTRING(AsignedUser, 1, 6) = '{equipmentRequest.Requester.Substring(0, 6)}'");
                var simCard = _repository.ExecuteScalarQuery<string>($"SELECT SimCardCode + ' - ' + PhoneNumber FROM {Helpers.InterCompanyId}.dbo.EIIV00201 WHERE SimCardCode = '{simCardCode}'");
                var equipmentDelivery = new EquipmentDelivery()
                {
                    BaseDocumentNumber = equipmentRequest.RequestId,
                    AssignedUser = equipmentRequest.Requester,
                    SimCard = simCard,
                    AmountCoverable = 0,
                    CostAmount = 0,
                    InvoiceOwner = "TCC",
                    PropertyBy = "TCC",
                    RequestId = HelperLogic.AsignaciónSecuencia("EIPM10200", Account.GetAccount(User.Identity.GetUserName()).UserId)
                };
                xStatus = "OK";
                HttpContext.Cache["EquipmentDelivery"] = equipmentDelivery;
                url = Url.Action("EquipmentDelivery", "Computing", new { id = "-1" });
                LogRequest(equipmentRequest.RequestId, Account.GetAccount(User.Identity.GetUserName()).FirstName + " " + Account.GetAccount(User.Identity.GetUserName()).LastName, "Enviado a entrega de equipo");
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, url }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveEquipmentRequestNote(string id, string note)
        {
            string xStatus;
            try
            {
                note = "NOTA:" + note;
                   var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EIPM10001 WHERE RequestId = '{id}' AND Note = '{note}'");
                if (count == 0)
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.EIPM10001 (RequestId, UserId, Note, LogDate, CreatedDate, ModifiedDate, LastUserId) " +
                        $"VALUES ('{id}', '{Account.GetAccount(User.Identity.GetUserName()).UserId}', '{note}', GETDATE(), GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public JsonResult EquipmentRequestReport(string id)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "EquipmentRequestReport.pdf",
                    $"INTRANET.dbo.EquipmentRequestReport '{Helpers.InterCompanyId}','{id}'", 44, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult CloseEquipmentRequest(string id)
        {
            string xStatus = "";
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10000 SET Status = 7 WHERE RequestId = '{id}'");
                LogRequest(id, Account.GetAccount(User.Identity.GetUserName()).FirstName + " " + Account.GetAccount(User.Identity.GetUserName()).LastName, "Solicitud marcada como completada");
                Task.Run(() => ProcessLogic.SendToSharepointAsync(id, 16, Account.GetAccount(User.Identity.GetUserName()).Email));
                //ProcessLogic.SendToSharepoint(id, 16, Account.GetAccount(User.Identity.GetUserName()).Email, ref xStatus);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Entrega de equipos

        public ActionResult EquipmentDeliveryIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentDelivery"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = $"SELECT RequestId, Device, DocumentDate, AssignedUser, DeliveryType, SimCard, PropertyBy, CostAmount, InvoiceOwner, AmountPayable, Accesories, AmountCoverable, Note, BaseDocumentNumber, Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIPM10200";
            var equipmentRequests = _repository.ExecuteQuery<EquipmentDelivery>(sqlQuery).ToList();
            return View(equipmentRequests);
        }

        public ActionResult EquipmentDelivery(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentDelivery"))
                return RedirectToAction("NotPermission", "Home");
            EquipmentDelivery equipmentDelivery;
            if (string.IsNullOrEmpty(id))
                equipmentDelivery = new EquipmentDelivery()
                {
                    RequestId = "",
                    Status = 0
                };
            else if (id == "-1")
            {
                equipmentDelivery = HttpContext.Cache["EquipmentDelivery"] as EquipmentDelivery;
                HttpContext.Cache.Remove("EquipmentDelivery");
            }
            else
            {
                string sqlQuery = $"SELECT RequestId, Device, DocumentDate, AssignedUser, DeliveryType, SimCard, PropertyBy, CostAmount, Accesories, InvoiceOwner, AmountPayable, AmountCoverable, Note, BaseDocumentNumber, Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIPM10200 WHERE RequestId = '{id}'";
                equipmentDelivery = _repository.ExecuteScalarQuery<EquipmentDelivery>(sqlQuery);
            }
            return View(equipmentDelivery);
        }

        [HttpPost]
        public JsonResult SaveEquipmentDelivery(EquipmentDelivery equipmentRequest, int postType = 0)
        {
            string xStatus = "";
            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EIPM10200 WHERE RequestId = '{equipmentRequest.RequestId}'");
                if (count > 0)
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10200 SET Device = '{equipmentRequest.Device}', AssignedUser = '{equipmentRequest.AssignedUser}', " +
                        $"DeliveryType = '{equipmentRequest.DeliveryType}', SimCard = '{equipmentRequest.SimCard}', PropertyBy = '{equipmentRequest.PropertyBy}', CostAmount = '{equipmentRequest.CostAmount}', " +
                        $"InvoiceOwner = '{equipmentRequest.InvoiceOwner}', AmountPayable = '{equipmentRequest.AmountPayable}', AmountCoverable = '{equipmentRequest.AmountCoverable}', " +
                        $"Status = '{equipmentRequest.Status}', Note = '{equipmentRequest.Note}', Accesories = '{equipmentRequest.Accessories}', ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                        $"WHERE RequestId = '{equipmentRequest.RequestId}'");
                else
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.EIPM10200 (RequestId, Device, DocumentDate, AssignedUser, DeliveryType, SimCard, PropertyBy, CostAmount, " +
                        $"InvoiceOwner, AmountPayable, AmountCoverable, BaseDocumentNumber, Status, Accesories, Note, CreatedDate, ModifiedDate, LastUserId) " +
                        $"VALUES ('{equipmentRequest.RequestId}', '{equipmentRequest.Device}', '{DateTime.Now.ToString("yyyyMMdd")}', '{equipmentRequest.AssignedUser}', " +
                        $"'{equipmentRequest.DeliveryType}', '{equipmentRequest.SimCard}', '{equipmentRequest.PropertyBy}', '{equipmentRequest.CostAmount}', '{equipmentRequest.InvoiceOwner}', " +
                        $"'{equipmentRequest.AmountPayable}', '{equipmentRequest.AmountCoverable}', '{equipmentRequest.BaseDocumentNumber}', 1, '{equipmentRequest.Accessories}', " +
                        $"'{equipmentRequest.Note}', GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10000 SET Status = 6 WHERE RequestId = '{equipmentRequest.BaseDocumentNumber}'");
                if (postType == 1)
                {
                    Task.Run(() => ProcessLogic.SendToSharepointAsync(equipmentRequest.RequestId, 14, Account.GetAccount(User.Identity.GetUserName()).Email));
                    //ProcessLogic.SendToSharepoint(equipmentRequest.RequestId, 14, Account.GetAccount(User.Identity.GetUserName()).Email, ref xStatus);
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10200 SET Status = 2 WHERE RequestId = '{equipmentRequest.RequestId}'");
                }

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult VerifyAssignedEquipment(string id)
        {
            string xStatus;
            try
            {
                string sqlQuery = $"SELECT COUNT(*) " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WHERE SUBSTRING(AsignedUser, 1, 6) = '{id}'";
                var countEquipment = _repository.ExecuteScalarQuery<int>(sqlQuery);
                if (countEquipment > 0)
                    xStatus = "ASIGNADO";
                else
                    xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeliverEquipment(string id, bool unAssign)
        {
            string xStatus = "";
            try
            {
                string sqlQuery = $"SELECT RequestId, Device, DocumentDate, AssignedUser, DeliveryType, SimCard, PropertyBy, CostAmount, InvoiceOwner, AmountPayable, AmountCoverable, BaseDocumentNumber, Status " +
                   $"FROM {Helpers.InterCompanyId}.dbo.EIPM10200 WHERE RequestId = '{id}'";
                var equipmentDelivery = _repository.ExecuteScalarQuery<EquipmentDelivery>(sqlQuery);

                var requestType = _repository.ExecuteScalarQuery<string>($"SELECT RequestType FROM {Helpers.InterCompanyId}.dbo.EIPM10000 WHERE RequestId = '{equipmentDelivery.BaseDocumentNumber}'");

                sqlQuery = $"SELECT DeviceCode " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WHERE SUBSTRING(AsignedUser, 1, 6) = '{equipmentDelivery.AssignedUser.PadLeft(6, '0').Substring(0, 6)}'";
                var deviceCode = _repository.ExecuteScalarQuery<string>(sqlQuery) ?? "";
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10200 SET Status = 6 WHERE RequestId = '{equipmentDelivery.RequestId}'");

                if (equipmentDelivery.DeliveryType == "20")
                    LogRequest(equipmentDelivery.BaseDocumentNumber, Account.GetAccount(User.Identity.GetUserName()).FirstName + " " + Account.GetAccount(User.Identity.GetUserName()).LastName, "Equipo entregado en prestamo");
                else
                {
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10000 SET Status = 7 WHERE RequestId = '{equipmentDelivery.BaseDocumentNumber}'");
                    LogRequest(equipmentDelivery.BaseDocumentNumber, Account.GetAccount(User.Identity.GetUserName()).FirstName + " " + Account.GetAccount(User.Identity.GetUserName()).LastName, "Equipo entregado");
                    Task.Run(() => ProcessLogic.SendToSharepointAsync(equipmentDelivery.BaseDocumentNumber, 16, Account.GetAccount(User.Identity.GetUserName()).Email));
                    //ProcessLogic.SendToSharepoint(equipmentDelivery.BaseDocumentNumber, 16, Account.GetAccount(User.Identity.GetUserName()).Email, ref xStatus);
                }

                if (unAssign && !string.IsNullOrEmpty(deviceCode))
                {
                    if (requestType == "60")
                        _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00101 SET AsignedUser = '', SimCard = '', Status = 3 WHERE DeviceCode = '{deviceCode}'");
                    else
                        _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00101 SET AsignedUser = '', SimCard = '', Status = 6 WHERE DeviceCode = '{deviceCode}'");
                }

                if (equipmentDelivery.DeliveryType == "20")
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00101 SET AsignedUser = '{equipmentDelivery.AssignedUser}', SimCard = '{(equipmentDelivery.SimCard ?? "").PadLeft(6, ' ').Substring(0, 6).Trim()}', Status = 2 WHERE DeviceCode = '{equipmentDelivery.Device.Split('-')[0].Trim()}'");
                else
                {
                    var simCard = _repository.ExecuteScalarQuery<SimCard>($"SELECT SimCardCode, SerialNumber, Operator, PhoneNumber, AcquiredDate, HasData, DataQuantity, MinuteOpen, QuantityMinutes, AsignedEquipment, " +
                    $"DeliveryDate, ChangePoints, Status FROM {Helpers.InterCompanyId}.dbo.EIIV00201 WHERE SimCardCode = '{(equipmentDelivery.SimCard ?? "").PadLeft(6, ' ').Substring(0, 6).Trim()}'");
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00101 SET AsignedUser = '{equipmentDelivery.AssignedUser}', DeliveryDate = GETDATE(), " +
                        $"SimCard = '{(equipmentDelivery.SimCard ?? "").PadLeft(6, ' ').Substring(0, 6).Trim()}', Status = 1, PhoneNumber = '{simCard?.PhoneNumber ?? ""}' " +
                        $"WHERE DeviceCode = '{equipmentDelivery.Device.Split('-')[0].Trim()}'");
                }
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00201 SET AsignedEquipment = '{equipmentDelivery.Device}', DeliveryDate = GETDATE(), Status = 1 WHERE SimCardCode = '{equipmentDelivery.SimCard.Split('-')[0].Trim()}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public JsonResult EquipmentDeliveryReport(string id)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "EquipmentDeliveryReport.pdf",
                    $"INTRANET.dbo.EquipmentDeliveryReport '{Helpers.InterCompanyId}','{id}'", 45, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult DeleteEquipmentDelivery(string id)
        {
            string xStatus = "";
            try
            {
                string sqlQuery = $"SELECT BaseDocumentNumber FROM {Helpers.InterCompanyId}.dbo.EIPM10200 WHERE RequestId = '{id}'";
                var baseDocument = _repository.ExecuteScalarQuery<string>(sqlQuery);
                sqlQuery = $"SELECT DeliveryType FROM {Helpers.InterCompanyId}.dbo.EIPM10200 WHERE RequestId = '{id}'";
                var type = _repository.ExecuteScalarQuery<string>(sqlQuery);
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10200 SET Status = 5 WHERE RequestId = '{id}'");
                LogRequest(baseDocument, Account.GetAccount(User.Identity.GetUserName()).FirstName + " " + Account.GetAccount(User.Identity.GetUserName()).LastName, "Solicitud de entrega de equipo anulada");
                if (type == "10")
                {
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10000 SET Status = 7 WHERE RequestId = '{baseDocument}'");
                    Task.Run(() => ProcessLogic.SendToSharepointAsync(baseDocument, 16, Account.GetAccount(User.Identity.GetUserName()).Email));
                    //ProcessLogic.SendToSharepoint(baseDocument, 16, Account.GetAccount(User.Identity.GetUserName()).Email, ref xStatus);
                }
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Reparacion de equipos

        public ActionResult EquipmentRepairIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentRepair"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = $"SELECT RequestId, Device, Diagnostics, Supplier, Cost, DocumentDate, BaseDocumentNumber, Note, Status FROM {Helpers.InterCompanyId}.dbo.EIPM10300";
            var equipmentRepairs = _repository.ExecuteQuery<EquipmentRepair>(sqlQuery).ToList();
            return View(equipmentRepairs);
        }

        public ActionResult EquipmentRepair(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentRepair"))
                return RedirectToAction("NotPermission", "Home");
            EquipmentRepair equipmentRepair;
            if (string.IsNullOrEmpty(id))
                equipmentRepair = new EquipmentRepair()
                {
                    RequestId = HelperLogic.AsignaciónSecuencia("EIPM10300", Account.GetAccount(User.Identity.GetUserName()).UserId),
                    Status = 0
                };
            else if (id == "-1")
            {
                equipmentRepair = HttpContext.Cache["EquipmentRepair"] as EquipmentRepair;
                HttpContext.Cache.Remove("EquipmentRepair");
            }
            else
            {
                string sqlQuery = $"SELECT RequestId, Device, Diagnostics, DocumentDate, Supplier, Cost, BaseDocumentNumber, Note, Status FROM {Helpers.InterCompanyId}.dbo.EIPM10300 WHERE RequestId = '{id}'";
                equipmentRepair = _repository.ExecuteScalarQuery<EquipmentRepair>(sqlQuery);
            }
            return View(equipmentRepair);
        }

        [HttpPost]
        public JsonResult SaveEquipmentRepair(EquipmentRepair equipmentRepair, int postType = 0)
        {
            string xStatus = "";
            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EIPM10300 WHERE RequestId = '{equipmentRepair.RequestId}'");
                if (count > 0)
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10300 SET Note = '{equipmentRepair.Note}', Device = '{equipmentRepair.Device}', Diagnostics = '{equipmentRepair.Diagnostics}', " +
                        $"Supplier = '{equipmentRepair.Supplier}', Cost = '{equipmentRepair.Cost}', ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                        $"WHERE RequestId = '{equipmentRepair.RequestId}'");
                else
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.EIPM10300 (RequestId, Device, DocumentDate, Diagnostics, Supplier, Cost, BaseDocumentNumber, Note, Status, CreatedDate, ModifiedDate, LastUserId) " +
                        $"VALUES ('{equipmentRepair.RequestId}', '{equipmentRepair.Device}', '{DateTime.Now.ToString("yyyyMMdd")}', '{equipmentRepair.Diagnostics}', " +
                        $"'{equipmentRepair.Supplier}', '{equipmentRepair.Cost}', '{equipmentRepair.BaseDocumentNumber}', '{equipmentRepair.Note}', 1, GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");

                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10000 SET Status = 6 WHERE RequestId = '{equipmentRepair.BaseDocumentNumber}'");
                if (postType == 1)
                {
                    Task.Run(() => ProcessLogic.SendToSharepointAsync(equipmentRepair.RequestId, 15, Account.GetAccount(User.Identity.GetUserName()).Email));
                    //ProcessLogic.SendToSharepoint(equipmentRepair.RequestId, 15, Account.GetAccount(User.Identity.GetUserName()).Email, ref xStatus);
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10300 SET Status = 2 WHERE RequestId = '{equipmentRepair.RequestId}'");
                }

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SendEquipmentDeliveryFromRepair(string id, string device, string repairId)
        {
            string xStatus;
            string url = "";
            try
            {
                string sqlQuery = $"SELECT RequestId, RequestType, DocumentDate, DepartmentId, Requester, RequestBy, HasData, OpenMinutes, Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIPM10000 WHERE RequestId = '{id}'";
                var equipmentRequest = _repository.ExecuteScalarQuery<EquipmentRequest>(sqlQuery);
                var cost = _repository.ExecuteScalarQuery<decimal?>($"SELECT Cost FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WHERE DeviceCode = '{device.PadLeft(6, ' ').Substring(0, 6).Trim()}'") ?? 0;
                var simCardCode = _repository.ExecuteScalarQuery<string>($"SELECT SimCard FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WHERE SUBSTRING(AsignedUser, 1, 6) = '{equipmentRequest.Requester.Substring(0, 6)}'");
                var simCard = _repository.ExecuteScalarQuery<string>($"SELECT SimCardCode + ' - ' + PhoneNumber FROM {Helpers.InterCompanyId}.dbo.EIIV00201 WHERE SimCardCode = '{simCardCode}'");
                var equipmentDelivery = new EquipmentDelivery()
                {
                    BaseDocumentNumber = equipmentRequest.RequestId,
                    AssignedUser = equipmentRequest.Requester,
                    SimCard = simCard,
                    Device = device,
                    CostAmount = cost,
                    AmountPayable = 0,
                    AmountCoverable = cost,
                    InvoiceOwner = "TCC",
                    PropertyBy = "TCC",
                    RequestId = HelperLogic.AsignaciónSecuencia("EIPM10200", Account.GetAccount(User.Identity.GetUserName()).UserId),
                    DeliveryType = "10",
                    DocumentDate = DateTime.Now,
                    Note = "",
                    Status = 4,
                    Accessories = ""
                };
                SaveEquipmentDelivery(equipmentDelivery);
                xStatus = "OK";
                url = Url.Action("EquipmentDelivery", "Computing", new { id = equipmentDelivery.RequestId });
                LogRequest(equipmentRequest.RequestId, Account.GetAccount(User.Identity.GetUserName()).FirstName + " " + Account.GetAccount(User.Identity.GetUserName()).LastName, "Enviado a entrega de equipo desde reparacion");
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10300 SET Status = 6 WHERE RequestId = '{repairId}'");
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus, url }, JsonRequestBehavior.AllowGet);
        }

        private void SaveEquipmentDelivery(EquipmentDelivery equipmentRequest)
        {
            var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EIPM10200 WHERE RequestId = '{equipmentRequest.RequestId}'");
            if (count > 0)
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10200 SET Device = '{equipmentRequest.Device}', AssignedUser = '{equipmentRequest.AssignedUser}', " +
                    $"DeliveryType = '{equipmentRequest.DeliveryType}', SimCard = '{equipmentRequest.SimCard}', PropertyBy = '{equipmentRequest.PropertyBy}', CostAmount = '{equipmentRequest.CostAmount}', " +
                    $"InvoiceOwner = '{equipmentRequest.InvoiceOwner}', AmountPayable = '{equipmentRequest.AmountPayable}', AmountCoverable = '{equipmentRequest.AmountCoverable}', Status = '{equipmentRequest.Status}', " +
                    $"ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                    $"WHERE RequestId = '{equipmentRequest.RequestId}'");
            else
                _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.EIPM10200 (RequestId, Device, DocumentDate, AssignedUser, DeliveryType, SimCard, PropertyBy, CostAmount, " +
                    $"InvoiceOwner, AmountPayable, AmountCoverable, BaseDocumentNumber, Status, CreatedDate, ModifiedDate, LastUserId) " +
                    $"VALUES ('{equipmentRequest.RequestId}', '{equipmentRequest.Device}', '{DateTime.Now.ToString("yyyyMMdd")}', '{equipmentRequest.AssignedUser}', " +
                    $"'{equipmentRequest.DeliveryType}', '{equipmentRequest.SimCard}', '{equipmentRequest.PropertyBy}', '{equipmentRequest.CostAmount}', '{equipmentRequest.InvoiceOwner}', " +
                    $"'{equipmentRequest.AmountPayable}', '{equipmentRequest.AmountCoverable}', '{equipmentRequest.BaseDocumentNumber}', 4, GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");

            _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10000 SET Status = 6 WHERE RequestId = '{equipmentRequest.BaseDocumentNumber}'");
        }

        [HttpPost]
        public JsonResult RepairEquipment(string id)
        {
            string xStatus;
            try
            {
                var equipmentRequestId = _repository.ExecuteScalarQuery<string>($"SELECT BaseDocumentNumber FROM {Helpers.InterCompanyId}.dbo.EIPM10300 WHERE RequestId = '{id}'");
                var deviceCode = _repository.ExecuteScalarQuery<string>($"SELECT SUBSTRING(Device, 1, 6) FROM {Helpers.InterCompanyId}.dbo.EIPM10300 WHERE RequestId = '{id}'");
                LogRequest(equipmentRequestId, Account.GetAccount(User.Identity.GetUserName()).FirstName + " " + Account.GetAccount(User.Identity.GetUserName()).LastName, "Equipo enviado a reparacion con el suplidor");
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10300 SET Status = 7 WHERE RequestId = '{id}'");
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00101 SET Status = 5 WHERE DeviceCode = '{deviceCode}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public JsonResult EquipmentRepairReport(string id)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "EquipmentRepairReport.pdf",
                    $"INTRANET.dbo.EquipmentRepairReport '{Helpers.InterCompanyId}','{id}'", 46, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        [HttpPost]
        public JsonResult DeleteEquipmentRepair(string id)
        {
            string xStatus = "";
            try
            {
                string sqlQuery = $"SELECT BaseDocumentNumber FROM {Helpers.InterCompanyId}.dbo.EIPM10300 WHERE RequestId = '{id}'";
                var baseDocument = _repository.ExecuteScalarQuery<string>(sqlQuery);
                sqlQuery = $"SELECT RequestType FROM {Helpers.InterCompanyId}.dbo.EIPM10000 WHERE RequestId = '{baseDocument}'";
                var type = _repository.ExecuteScalarQuery<string>(sqlQuery);
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10300 SET Status = 5 WHERE RequestId = '{id}'");
                LogRequest(baseDocument, Account.GetAccount(User.Identity.GetUserName()).FirstName + " " + Account.GetAccount(User.Identity.GetUserName()).LastName, "Solicitud de reparacion del equipo anulada");
                if (type == "20")
                {
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10000 SET Status = 7 WHERE RequestId = '{baseDocument}'");
                    Task.Run(() => ProcessLogic.SendToSharepointAsync(baseDocument, 16, Account.GetAccount(User.Identity.GetUserName()).Email));
                    //ProcessLogic.SendToSharepoint(baseDocument, 16, Account.GetAccount(User.Identity.GetUserName()).Email, ref xStatus);
                }
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Desasignacion

        public ActionResult EquipmentUnassignIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentUnassign"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = $"SELECT RequestId, DocumentDate, DeviceSimCard, DeviceType, EmployeeId, Reason, Note FROM {Helpers.InterCompanyId}.dbo.EIPM10400 ";
            var equipmentUnassing = _repository.ExecuteQuery<EquipmentUnassign>(sqlQuery).ToList();
            return View(equipmentUnassing);
        }

        public ActionResult EquipmentUnassign(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentUnassign"))
                return RedirectToAction("NotPermission", "Home");
            EquipmentUnassign equipmentUnassign;
            if (string.IsNullOrEmpty(id))
                equipmentUnassign = new EquipmentUnassign()
                {
                    RequestId = HelperLogic.AsignaciónSecuencia("EIPM10400", Account.GetAccount(User.Identity.GetUserName()).UserId)
                };
            else
            {
                string sqlQuery = $"SELECT RequestId, DocumentDate, DeviceSimCard, DeviceType, EmployeeId, Reason, Note FROM {Helpers.InterCompanyId}.dbo.EIPM10400 WHERE RequestId = '{id}'";
                equipmentUnassign = _repository.ExecuteScalarQuery<EquipmentUnassign>(sqlQuery);
            }
            return View(equipmentUnassign);
        }

        [HttpPost]
        public JsonResult SaveEquipmentUnassign(EquipmentUnassign equipmentUnassign)
        {
            string xStatus = "";
            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EIPM10400 WHERE RequestId = '{equipmentUnassign.RequestId}'");
                if (count > 0)
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10400 SET DeviceSimCard = '{equipmentUnassign.DeviceSimCard}', Reason = '{equipmentUnassign.Reason}', " +
                        $"EmployeeId = '{equipmentUnassign.EmployeeId}', DeviceType = '{equipmentUnassign.DeviceType}', Note = '{equipmentUnassign.Note}', ModifiedDate = GETDATE(), " +
                        $"LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' WHERE RequestId = '{equipmentUnassign.RequestId}'");
                else
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.EIPM10400 (RequestId, DeviceSimCard, DocumentDate, Reason, EmployeeId, DeviceType, Note, CreatedDate, ModifiedDate, LastUserId) " +
                        $"VALUES ('{equipmentUnassign.RequestId}', '{equipmentUnassign.DeviceSimCard}', '{DateTime.Now.ToString("yyyyMMdd")}', '{equipmentUnassign.Reason}', " +
                        $"'{equipmentUnassign.EmployeeId}', '{equipmentUnassign.DeviceType}', '{equipmentUnassign.Note}', GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
                if (equipmentUnassign.DeviceType == "10")
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00101 SET AsignedUser = '', Status = 6 WHERE DeviceCode = '{equipmentUnassign.DeviceSimCard.Substring(0, 6).Trim()}' ");
                else
                {
                    var equipment = _repository.ExecuteScalarQuery<string>($"SELECT SUBSTRING(AsignedEquipment, 1, 6) FROM {Helpers.InterCompanyId}.dbo.EIIV00201 " +
                        $"WHERE SimCardCode = '{equipmentUnassign.DeviceSimCard.Substring(0, 6).Trim()}'");
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00101 SET SimCard = '', PhoneNumber = '' WHERE DeviceCode = '{equipment}' ");
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00201 SET AsignedEquipment = '' WHERE SimCardCode = '{equipmentUnassign.DeviceSimCard.Substring(0, 6).Trim()}' ");
                }

                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public JsonResult EquipmentUnassignReport(string id)
        {
            string xStatus;
            try
            {
                xStatus = "OK";
                ReportHelper.Export(Helpers.ReportPath + "Reportes", Server.MapPath("~/PDF/Reportes/") + "EquipmentUnassignReport.pdf",
                    $"INTRANET.dbo.EquipmentUnassignReport '{Helpers.InterCompanyId}','{id}'", 47, ref xStatus);
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        #endregion

        #region Fidepuntos

        public ActionResult EarnedPoints()
        {
            var sqlQuery = $"SELECT Description, Points, DocumentDate, SummaryType FROM {Helpers.InterCompanyId}.dbo.EIIV00301 ORDER BY DocumentDate DESC ";
            var list = _repository.ExecuteQuery<EarnedPoint>(sqlQuery).ToList();
            ViewBag.Total = list.Sum(x => x.Points);
            return View(list);
        }
        [HttpPost]
        public JsonResult SaveEarnedPoint(EarnedPoint point)
        {
            string status;
            try
            {
                SaveEarnedPointEntity(point);
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return new JsonResult { Data = new { status } };
        }

        public void SaveEarnedPointEntity(EarnedPoint point)
        {
            string sqlQuery;
            var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EIIV00301 WHERE Description = '{point.Description}'");
            if (count == 0)
                sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EIIV00301 (Description, Points, DocumentDate, SummaryType, CreatedDate, ModifiedDate, LastUserId) " +
                     $"VALUES ('{point.Description}','{(point.SummaryType == 0 ? point.Points : point.Points * -1)}','{DateTime.Now.ToString("yyyyMMdd")}','{point.SummaryType}', GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}') ";
            else
                sqlQuery = $"UPDATE {Helpers.InterCompanyId}.dbo.EIIV00301 SET Points = '{(point.SummaryType == 0 ? point.Points : point.Points * -1)}', " +
                    $"SummaryType = '{point.SummaryType}', ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' " +
                    $"WHERE Description = '{point.Description}'";
            _repository.ExecuteCommand(sqlQuery);
        }
        [HttpPost]
        public JsonResult DeleteEarnedPoint(string id)
        {
            string xStatus;

            try
            {
                var sqlQuery = $"DELETE {Helpers.InterCompanyId}.dbo.EIIV00301 WHERE Description = '{id}'";
                _repository.ExecuteCommand(sqlQuery);
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus } };
        }

        #endregion

        #region Casos

        public ActionResult CaseIndex()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "Case"))
                return RedirectToAction("NotPermission", "Home");
            string sqlQuery = $"SELECT CaseNumber, Description, EmployeeId, Diagnostics, Note, PhoneNumber, DocumentDate, Status FROM {Helpers.InterCompanyId}.dbo.EIPM10500 ";
            var caseEntity = _repository.ExecuteQuery<Case>(sqlQuery).ToList();
            return View(caseEntity);
        }

        public ActionResult Case(string id = "")
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "Case"))
                return RedirectToAction("NotPermission", "Home");
            Case caseEntity;
            if (string.IsNullOrEmpty(id))
                caseEntity = new Case()
                {
                    Status = 0,
                    DocumentDate = DateTime.Now
                };
            else
            {
                string sqlQuery = $"SELECT CaseNumber, Description, EmployeeId, Diagnostics, Note, PhoneNumber, DocumentDate, Status FROM {Helpers.InterCompanyId}.dbo.EIPM10500 WHERE CaseNumber = '{id}'";
                caseEntity = _repository.ExecuteScalarQuery<Case>(sqlQuery);
            }
            return View(caseEntity);
        }

        [HttpPost]
        public JsonResult SaveCase(Case caseEntity)
        {
            string xStatus = "";
            try
            {
                var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EIPM10500 WHERE CaseNumber = '{caseEntity.CaseNumber}'");
                if (count > 0)
                    _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10500 SET Diagnostics = '{caseEntity.Diagnostics}', Description = '{caseEntity.Description}', " +
                        $"EmployeeId = '{caseEntity.EmployeeId}', PhoneNumber = '{caseEntity.PhoneNumber}', Note = '{caseEntity.Note}', ModifiedDate = GETDATE(), " +
                        $"LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' WHERE CaseNumber = '{caseEntity.CaseNumber}'");
                else
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.EIPM10500 (CaseNumber, Description, DocumentDate, EmployeeId, PhoneNumber, Diagnostics, Note, Status, CreatedDate, ModifiedDate, LastUserId) " +
                        $"VALUES ('{caseEntity.CaseNumber}', '{caseEntity.Description}', '{caseEntity.DocumentDate.ToString("yyyyMMdd")}', '{caseEntity.EmployeeId}', '{caseEntity.PhoneNumber}', " +
                        $"'{caseEntity.Diagnostics}', '{caseEntity.Note}', 1, GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteCase(string id)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand($"DELETE {Helpers.InterCompanyId}.dbo.EIPM10500 WHERE CaseNumber = '{id}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CloseCase(string id)
        {
            string xStatus;
            try
            {
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10500 SET Status = 2 WHERE CaseNumber = '{id}'");
                xStatus = "OK";
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }
            return Json(new { status = xStatus }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Consultas

        public ActionResult EquipmentInquiry()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "Equipment"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult EquipmentInquiry(int status, string dateRange, string department)
        {
            string xStatus;
            var equipments = new List<Equipment>();
            try
            {
                xStatus = "OK";
                string sqlQuery = $"SELECT AcquireMode, AsignedUser, Brand, CanUseData, Cost, DeliveryDate, Description, MobileTechnology, Model, Operator, PhoneNumber, " +
                    $"SerialNumber, SimCard, Status, DeviceCode, AcquiredDate, RTRIM(C.DSCRIPTN) Department " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIIV00101 A " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.UPR00100  B ON SUBSTRING(A.AsignedUser, 1, 6) = RTRIM(B.EMPLOYID) " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.UPR40300  C ON RTRIM(B.DEPRTMNT) = RTRIM(C.DEPRTMNT) ";

                var filters = new List<string>();

                if (status != 0)
                    filters.Add($"A.Status = '{status}' ");
                if (!string.IsNullOrEmpty(dateRange))
                    filters.Add($"A.DeliveryDate BETWEEN '{DateTime.ParseExact(dateRange.Split('-')[0].Trim(), "MM/dd/yyyy", null)}' AND '{DateTime.ParseExact(dateRange.Split('-')[1].Trim(), "MM/dd/yyyy", null)}' ");
                if (!string.IsNullOrEmpty(department))
                    filters.Add($"RTRIM(C.DSCRIPTN) = '{department.Trim()}' ");
                if (filters.Count > 0)
                    sqlQuery += "WHERE " + filters.FirstOrDefault();
                foreach (var item in filters)
                    sqlQuery += " AND " + item;
                equipments = _repository.ExecuteQuery<Equipment>(sqlQuery).ToList();
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = equipments } };
        }

        public ActionResult SimCardInquiry()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "SimCard"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult SimCardInquiry(int status, string dateRange, string department)
        {
            string xStatus;
            var equipments = new List<SimCard>();
            try
            {
                xStatus = "OK";
                string sqlQuery = $"SELECT DISTINCT A.SimCardCode, A.SerialNumber, A.Operator, A.PhoneNumber, A.AcquiredDate, A.HasData, A.DataQuantity, A.MinuteOpen, A.QuantityMinutes, A.AsignedEquipment, " +
                    $"A.DeliveryDate, A.ChangePoints, A.Status, B.AsignedUser AssignedUser, RTRIM(D.DSCRIPTN) Department " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIIV00201 A " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.EIIV00101 B ON A.SimCardCode = B.SimCard " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.UPR00100  C ON SUBSTRING(B.AsignedUser, 1, 6) = RTRIM(C.EMPLOYID) " +
                    $"LEFT JOIN {Helpers.InterCompanyId}.dbo.UPR40300  D ON RTRIM(C.DEPRTMNT) = RTRIM(D.DEPRTMNT) ";

                 var filters = new List<string>();

                if (status != 0)
                    filters.Add($"A.Status = '{status}' ");
                if (!string.IsNullOrEmpty(dateRange))
                    filters.Add($"A.DeliveryDate BETWEEN '{DateTime.ParseExact(dateRange.Split('-')[0].Trim(), "MM/dd/yyyy", null)}' AND '{DateTime.ParseExact(dateRange.Split('-')[1].Trim(), "MM/dd/yyyy", null)}' ");
                if (!string.IsNullOrEmpty(department))
                    filters.Add($"RTRIM(D.DSCRIPTN) = '{department.Trim()}' ");
                if (filters.Count > 0)
                    sqlQuery += "WHERE " + filters.FirstOrDefault();
                foreach (var item in filters)
                    sqlQuery += " AND " + item;
                equipments = _repository.ExecuteQuery<SimCard>(sqlQuery).ToList();
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = equipments } };
        }

        public ActionResult EquipmentRequestInquiry()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentRequest"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult EquipmentRequestInquiry(string requestType, int status, string department, string employee, string dateRange)
        {
            string xStatus;
            var list = new List<EquipmentRequest>();
            try
            {
                xStatus = "OK";
                string sqlQuery = $"SELECT RequestId, RequestType, DocumentDate, DepartmentId, Requester, RequestBy, HasData, OpenMinutes, Note, Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIPM10000 ";

                var filters = new List<string>();

                if (requestType != "0")
                    filters.Add($"RequestType = '{requestType}' ");
                if (status != 0)
                    filters.Add($"Status = '{status}' ");
                if (!string.IsNullOrEmpty(department))
                    filters.Add($"DepartmentId = '{department}' ");
                if (!string.IsNullOrEmpty(dateRange))
                    filters.Add($"DocumentDate BETWEEN '{DateTime.ParseExact(dateRange.Split('-')[0].Trim(), "MM/dd/yyyy", null)}' AND '{DateTime.ParseExact(dateRange.Split('-')[1].Trim(), "MM/dd/yyyy", null)}' ");
                if (!string.IsNullOrEmpty(employee))
                    filters.Add($"Requester = '{employee}' ");

                if (filters.Count > 0)
                    sqlQuery += "WHERE " + filters.FirstOrDefault();
                foreach (var item in filters)
                    sqlQuery += " AND " + item;
                list = _repository.ExecuteQuery<EquipmentRequest>(sqlQuery).ToList();
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = list } };
        }

        public ActionResult EquipmentDeliveryInquiry()
        {
            if (!HelperLogic.GetPermission(Account.GetAccount(User.Identity.GetUserName()).UserId, "Computing", "EquipmentDelivery"))
                return RedirectToAction("NotPermission", "Home");
            return View();
        }

        [OutputCache(Duration = 0)]
        [HttpPost]
        public ActionResult EquipmentDeliveryInquiry(string deliveryType, int status, string department, string employee, string dateRange)
        {
            string xStatus;
            var list = new List<EquipmentDelivery>();
            try
            {
                xStatus = "OK";
                string sqlQuery = $"SELECT A.RequestId, A.Device, A.DocumentDate, A.AssignedUser, A.DeliveryType, A.SimCard, A.PropertyBy, A.CostAmount, A.InvoiceOwner, " +
                    $"A.AmountPayable, A.Accesories, A.AmountCoverable, A.Note, A.BaseDocumentNumber, A.Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIPM10200 A " +
                    $"INNER JOIN {Helpers.InterCompanyId}.dbo.EIPM10000 B ON A.BaseDocumentNumber = B.RequestId ";

                var filters = new List<string>();

                if (deliveryType != "0")
                    filters.Add($"A.DeliveryType = '{deliveryType}' ");
                if (status != 0)
                    filters.Add($"Status = '{status}' ");
                if (!string.IsNullOrEmpty(department))
                    filters.Add($"B.DepartmentId = '{department}' ");
                if (!string.IsNullOrEmpty(dateRange))
                    filters.Add($"A.DocumentDate BETWEEN '{DateTime.ParseExact(dateRange.Split('-')[0].Trim(), "MM/dd/yyyy", null)}' AND '{DateTime.ParseExact(dateRange.Split('-')[1].Trim(), "MM/dd/yyyy", null)}' ");
                if (!string.IsNullOrEmpty(employee))
                    filters.Add($"A.AssignedUser = '{employee}' ");

                if (filters.Count > 0)
                    sqlQuery += "WHERE " + filters.FirstOrDefault();
                foreach (var item in filters)
                    sqlQuery += " AND " + item;
                list = _repository.ExecuteQuery<EquipmentDelivery>(sqlQuery).ToList();
            }
            catch (Exception ex)
            {
                xStatus = ex.Message;
            }

            return new JsonResult { Data = new { status = xStatus, registros = list } };
        }

        #endregion

        [OutputCache(Duration = 0)]
        public JsonResult LookupData(int tipo = 0, string consultaExtra = "", string consulta = "")
        {
            string sqlQuery;
            switch (tipo)
            {
                case 1:
                    var service = new ServiceContract();
                    if (HttpContext.Cache["Brand"] == null) HttpContext.Cache["Brand"] = consultaExtra;
                    if (HttpContext.Cache["Brand"].ToString() != consultaExtra) HttpContext.Cache["Models"] = service.GetMobileDevices(consultaExtra);
                    if (HttpContext.Cache["Models"] == null) HttpContext.Cache["Models"] = service.GetMobileDevices(consultaExtra);
                    var mobileDevices = (List<MobileDevice>)HttpContext.Cache["Models"];
                    if (!string.IsNullOrEmpty(consulta)) mobileDevices = mobileDevices.Where(x => x.DeviceName.ToUpper().Contains(consulta.ToUpper())).ToList();
                    return Json(mobileDevices, JsonRequestBehavior.AllowGet);
                case 2:
                    sqlQuery = $"SELECT SimCardCode [Id], PhoneNumber [Descripción], UPPER(Operator) DataExtended " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EIIV00201 WITH (NOLOCK, READUNCOMMITTED) " +
                        $"WHERE (SimCardCode LIKE '%{consulta}%' OR PhoneNumber LIKE '%{consulta}%') AND Status NOT IN (3, 4) AND LEN(AsignedEquipment) = 0 ORDER BY SimCardCode";
                    var simCards = _repository.ExecuteQuery<Lookup>(sqlQuery);
                    return Json(simCards, JsonRequestBehavior.AllowGet);
                case 3:
                    sqlQuery = $"SELECT DeviceCode [Id], Model [Descripción], SerialNumber DataExtended, CONVERT(NVARCHAR(20), Cost) DataPlus " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WITH (NOLOCK, READUNCOMMITTED) " +
                        $"WHERE (DeviceCode LIKE '%{consulta}%' OR Model LIKE '%{consulta}%' OR SerialNumber LIKE '%{consulta}%') AND Status NOT IN (3, 4) AND LEN(AsignedUser) = 0 ORDER BY DeviceCode";
                    var equipments = _repository.ExecuteQuery<Lookup>(sqlQuery);
                    return Json(equipments, JsonRequestBehavior.AllowGet);
                case 4:
                    sqlQuery = $"SELECT DeviceCode [Id], Model [Descripción], SerialNumber DataExtended, CONVERT(NVARCHAR(20), Cost) DataPlus " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WITH (NOLOCK, READUNCOMMITTED) " +
                        $"WHERE (DeviceCode LIKE '%{consulta}%' OR Model LIKE '%{consulta}%' OR SerialNumber LIKE '%{consulta}%') AND Status NOT IN (3, 4) AND AsignedUser = '{consultaExtra}' ORDER BY DeviceCode";
                    var equipmentsAssigned = _repository.ExecuteQuery<Lookup>(sqlQuery);
                    return Json(equipmentsAssigned, JsonRequestBehavior.AllowGet);
                case 5:
                    sqlQuery = $"SELECT A.SimCardCode [Id], A.PhoneNumber [Descripción], UPPER(A.Operator) DataExtended " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EIIV00201 A WITH (NOLOCK, READUNCOMMITTED) " +
                        $"INNER JOIN {Helpers.InterCompanyId}.dbo.EIIV00101 B WITH (NOLOCK, READUNCOMMITTED) ON A.SimCardCode = B.SimCard " +
                        $"WHERE (A.SimCardCode LIKE '%{consulta}%' OR A.PhoneNumber LIKE '%{consulta}%') AND A.Status NOT IN (3, 4) AND B.AsignedUser = '{consultaExtra}' ORDER BY A.SimCardCode";
                    var simCardsAssigned = _repository.ExecuteQuery<Lookup>(sqlQuery);
                    return Json(simCardsAssigned, JsonRequestBehavior.AllowGet);
            }
            return Json("");
        }

        [OutputCache(Duration = 0)]
        public JsonResult UnblockSecuence(string id, string formulario)
        {
            HelperLogic.DesbloqueoSecuencia(id, formulario, Account.GetAccount(User.Identity.GetUserName()).UserId);
            return Json("");
        }

        private void LogRequest(string requestId, string userId, string note)
        {
            var count = _repository.ExecuteScalarQuery<int>($"SELECT COUNT(*) FROM {Helpers.InterCompanyId}.dbo.EIPM10001 WHERE RequestId = '{requestId}' AND Note = '{note}'");
            if (count > 0)
                _repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EIPM10001 SET UserId = '{userId}', LogDate = GETDATE(), ModifiedDate = GETDATE(), " +
                    $"LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}' WHERE RequestId = '{requestId}' AND Note = '{note}'");
            else
                _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.EIPM10001 (RequestId, UserId, LogDate, Note, CreatedDate, ModifiedDate, LastUserId) " +
                    $"VALUES ('{requestId}', '{userId}', GETDATE(), '{note}', GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");
        }
    }
}