using Microsoft.AspNet.Identity;
using Seaboard.Intranet.BusinessLogic;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Data.Repository;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Seaboard.Intranet.Web
{
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
            return View();
        }

        #region Equipos

        public ActionResult EquipmentIndex()
        {
            string sqlQuery = $"SELECT AcquireMode, AsignedUser, Brand, CanUseData, Cost, DeliveryDate, Description, MobileTechnology, Model, Operator, PhoneNumber, " +
                    $"SerialNumber, SimCard, Status, DeviceCode, AcquiredDate " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIIV00101";
            var equipments = _repository.ExecuteQuery<Equipment>(sqlQuery).ToList();
            return View(equipments);
        }

        public ActionResult Equipment(string id = "")
        {
            Equipment equipment;
            if (string.IsNullOrEmpty(id))
                equipment = new Equipment()
                {
                    Status = 1,
                    DeviceCode = HelperLogic.AsignaciónSecuencia("Equipment", Account.GetAccount(User.Identity.GetUserName()).UserId),
                    AcquiredDate = new DateTime(1900, 1, 1),
                    DeliveryDate = new DateTime(1900, 1, 1)
                };
            else
            {
                string sqlQuery = $"SELECT AcquireMode, AsignedUser, Brand, CanUseData, Cost, DeliveryDate, Description, MobileTechnology, Model, Operator, PhoneNumber, " +
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
                new SelectListItem { Value = "xiaomi", Text = "Xiaomi" }
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
                new SelectListItem { Value = "1", Text = "Nuevo" },
                new SelectListItem { Value = "2", Text = "Usado" },
                new SelectListItem { Value = "3", Text = "Dañado" },
                new SelectListItem { Value = "4", Text = "Reparado" },
                new SelectListItem { Value = "5", Text = "En Reparacion" },
                new SelectListItem { Value = "6", Text = "Prestamo" }
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
                        $"Operator = '{equipment.Operator}', PhoneNumber = '{equipment.PhoneNumber}', SerialNumber = '{equipment.SerialNumber}', SimCard = '{equipment.SimCard}', Status = '{equipment.Status}', " +
                        $"AcquiredDate = '{equipment.AcquiredDate.ToString("yyyyMMdd")}', ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}', " +
                        $"DeliveryDate = '{equipment.DeliveryDate.ToString("yyyyMMdd")}' " +
                        $"WHERE DeviceCode = '{equipment.DeviceCode}'");
                else
                    _repository.ExecuteCommand($"INSERT INTO {Helpers.InterCompanyId}.dbo.EIIV00101 (AcquireMode, AsignedUser, Brand, CanUseData, Cost, DeliveryDate, Description, MobileTechnology, Model, " +
                        $"Operator, PhoneNumber, SerialNumber, SimCard, Status, DeviceCode, AcquiredDate, CreatedDate, ModifiedDate, LastUserId) " +
                        $"VALUES ('{Convert.ToInt32(equipment.AcquireMode)}', '{equipment.AsignedUser}', '{equipment.Brand}', '{equipment.CanUseData}', '{equipment.Cost}', '{equipment.DeliveryDate.ToString("yyyyMMdd")}', " +
                        $"'{equipment.Description}', '{equipment.MobileTechnology}', '{equipment.Model}', '{equipment.Operator}', '{equipment.PhoneNumber}', '{equipment.SerialNumber}', '{equipment.SimCard}', " +
                        $"'{equipment.Status}', '{equipment.DeviceCode}', '{equipment.AcquiredDate.ToString("yyyyMMdd")}', GETDATE(), GETDATE(), '{Account.GetAccount(User.Identity.GetUserName()).UserId}')");

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
            string sqlQuery = $"SELECT SimCardCode, SerialNumber, Operator, PhoneNumber, AcquiredDate, HasData, DataQuantity, MinuteOpen, QuantityMinutes, AsignedEquipment, " +
                    $"DeliveryDate, ChangePoints, Status " +
                    $"FROM {Helpers.InterCompanyId}.dbo.EIIV00201";
            var simCards = _repository.ExecuteQuery<SimCard>(sqlQuery).ToList();
            return View(simCards);
        }

        public ActionResult SimCard(string id = "")
        {
            SimCard simCard;
            if (string.IsNullOrEmpty(id))
                simCard = new SimCard()
                {
                    Status = 1,
                    SimCardCode = HelperLogic.AsignaciónSecuencia("SimCard", Account.GetAccount(User.Identity.GetUserName()).UserId),
                    AcquiredDate = new DateTime(1900, 1, 1),
                    DeliveryDate = new DateTime(1900, 1, 1)
                };
            else
            {
                string sqlQuery = $"SELECT SimCardCode, SerialNumber, Operator, PhoneNumber, AcquiredDate, HasData, DataQuantity, MinuteOpen, QuantityMinutes, AsignedEquipment, " +
                   $"DeliveryDate, ChangePoints, Status " +
                   $"FROM {Helpers.InterCompanyId}.dbo.EIIV00201 WHERE SimCardCode = '{id}'";
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
                new SelectListItem { Value = "1", Text = "Nuevo" },
                new SelectListItem { Value = "2", Text = "Usado" },
                new SelectListItem { Value = "3", Text = "Dañado" },
                new SelectListItem { Value = "4", Text = "Reparado" },
                new SelectListItem { Value = "5", Text = "En Reparacion" },
                new SelectListItem { Value = "6", Text = "Prestamo" }
            };
            ViewBag.Operators = operators;
            ViewBag.Status = status;
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
                        $"AcquiredDate = '{simCard.AcquiredDate.ToString("yyyyMMdd")}', ModifiedDate = GETDATE(), LastUserId = '{Account.GetAccount(User.Identity.GetUserName()).UserId}', " +
                        $"DeliveryDate = '{simCard.DeliveryDate.ToString("yyyyMMdd")}' " +
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
                    sqlQuery = $"SELECT DeviceCode [Id], Model [Descripción], SerialNumber DataExtended " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EIIV00101 WITH (NOLOCK, READUNCOMMITTED) " +
                        $"WHERE (DeviceCode LIKE '%{consulta}%' OR Model LIKE '%{consulta}%' OR SerialNumber LIKE '%{consulta}%') AND Status NOT IN (3, 4) AND LEN(AsignedUser) = 0 ORDER BY DeviceCode";
                    var equipments = _repository.ExecuteQuery<Lookup>(sqlQuery);
                    return Json(equipments, JsonRequestBehavior.AllowGet);
            }
            return Json("");
        }

        [OutputCache(Duration = 0)]
        public JsonResult UnblockSecuence(string id, string formulario)
        {
            HelperLogic.DesbloqueoSecuencia(id, formulario, Account.GetAccount(User.Identity.GetUserName()).UserId);
            return Json("");
        }
    }
}