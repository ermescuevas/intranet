﻿using Microsoft.SharePoint.Client;
using Seaboard.Intranet.Data;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.ViewModels;
using Seaboard.Intranet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Seaboard.Intranet.Data.Repository;
using System.Security;
using System.Data;
using System.Web;

namespace Seaboard.Intranet.BusinessLogic
{
    public class ProcessLogic
    {
        private static SeaboContext _db;
        private static GenericRepository _repository;
        private static List<Attachments> _attachments;

        public static void SendToSharepoint(string idSolicitud, int tipo, string email, ref string status)
        {
            AttachmentCreationInformation attachInfo;
            Attachment attach;
            string sqlQuery;
            _db = new SeaboContext();
            _repository = new GenericRepository(_db);
            ClientContext clientContext;
            var securePassword = new SecureString();
            List listCollection;
            ListItemCreationInformation listInformation;
            ListItem listItem;
            var retryCount = 0;
            switch (tipo)
            {
                case 1:
                    #region Solicitud de Servicio

                    do
                    {
                        try
                        {
                            sqlQuery = "SELECT A.[POPRequisitionNumber],  A.[RequisitionDescription], CONVERT(nvarchar(8), A.[DOCDATE], 112) FechaDocumento, "
                            + " CONVERT(nvarchar(8), A.[REQDATE], 112) FechaRequerida, A.[REQSTDBY], A.[USERDEF1] Prioridad, A.[USERDEF2] Departamento, "
                            + "ISNULL(B.TXTFIELD, '') APROBADOR, ISNULL(D.TXTFIELD, '') NOTA,ISNULL(F.USERID, '') APROBADOR2, ISNULL(G.TXTFIELD, '') AR "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.POP10200 A "
                            + "LEFT JOIN  " + Helpers.InterCompanyId +
                            ".dbo.LPRFQ10100 B ON A.POPRequisitionNumber = B.RFQNMBR AND B.TYPE = 1 "
                            + "INNER JOIN " + Helpers.InterCompanyId +
                            ".dbo.POP10210   C ON A.POPRequisitionNumber = C.POPRequisitionNumber "
                            + "LEFT JOIN  " + Helpers.InterCompanyId +
                            ".dbo.SY03900    D ON A.Requisition_Note_Index = D.NOTEINDX "
                            + "LEFT JOIN  " + Helpers.InterCompanyId +
                            ".dbo.LPPOP40100 E ON LTRIM(RTRIM(A.USERDEF2)) = LTRIM(RTRIM(E.DEPRTMDS)) "
                            + "LEFT JOIN  " + Helpers.InterCompanyId +
                            ".dbo.LPPOP40101 F ON LTRIM(RTRIM(E.DEPRTMID)) = LTRIM(RTRIM(F.DEPRTMID)) AND F.ISSECND = 1 "
                            + "LEFT JOIN  " + Helpers.InterCompanyId +
                            ".dbo.LPRFQ10100 G ON A.POPRequisitionNumber = G.RFQNMBR AND G.TYPE = 3 "
                            + "WHERE A.POPRequisitionNumber = '" + idSolicitud + "'";

                            var header = _repository.ExecuteScalarQuery<RequisitionHeaderViewModel>(sqlQuery);

                            sqlQuery = "SELECT B.fileName NombreArchivo, B.BinaryBlob DataArchivo "
                                       + "FROM " + Helpers.InterCompanyId + ".dbo.CO00102 A "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.coAttachmentItems B "
                                       + "ON A.Attachment_ID = B.Attachment_ID "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.CO00105 C "
                                       + "ON A.Attachment_ID = C.Attachment_ID "
                                       + "WHERE C.DOCNUMBR = '" + idSolicitud + "' "
                                       + "AND C.DELETE1 = 0 ";

                            _attachments = _repository.ExecuteQuery<Attachments>(sqlQuery).ToList();
                            sqlQuery = $"SELECT TOP 1 RTRIM(ISNULL(DOCNUMBR, '')) FROM {Helpers.InterCompanyId}.dbo.LLIF10100 WHERE WORKNUMB = '{idSolicitud}' AND DOCTYPE = 1";
                            var solicitud = _repository.ExecuteScalarQuery<string>(sqlQuery);

                            if (header != null)
                            {
                                clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                                foreach (char c in "Servicios2.4")
                                    securePassword.AppendChar(c);
                                clientContext.Credentials = new SharePointOnlineCredentials("no-reply@seaboardpower.com.do", securePassword);
                                listCollection = clientContext.Web.Lists.GetByTitle("Servicio");
                                listInformation = new ListItemCreationInformation();

                                using (clientContext)
                                {
                                    clientContext.Load(clientContext.Web);
                                    clientContext.Load(listCollection);
                                    var builder = new StringBuilder();
                                    builder.Append("<View><Query>");
                                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                                    builder.Append("<Value Type='Text'>" + header.PopRequisitionNumber.Trim() + "</Value></Eq></Where>");
                                    builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                    var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                    var collection = listCollection.GetItems(query);
                                    clientContext.Load(collection);
                                    clientContext.ExecuteQuery();

                                    if (collection.Count > 0)
                                    {
                                        foreach (var item in collection)
                                        {
                                            listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                            listItem.DeleteObject();
                                            clientContext.ExecuteQuery();
                                        }
                                    }
                                }

                                listItem = listCollection.AddItem(listInformation);
                                listItem["Solicitud"] = header.PopRequisitionNumber.Trim();
                                listItem["Fecha"] = DateTime.ParseExact(header.FechaDocumento, "yyyyMMdd", null).ToString(CultureInfo.InvariantCulture);
                                listItem["FechaRequerida"] = DateTime.ParseExact(header.FechaRequerida, "yyyyMMdd", null).ToString(CultureInfo.InvariantCulture);
                                listItem["Departamento"] = header.Departamento.Trim();
                                listItem["Prioridad"] = header.Prioridad.Trim();
                                listItem["AR"] = header.Ar.Trim();
                                listItem["Descripcion"] = header.RequisitionDescription.Trim();
                                listItem["Solicitante"] = email;
                                listItem["DB"] = Helpers.InterCompanyId;
                                listItem["Nota"] = header.Nota.Trim();
                                if (!string.IsNullOrEmpty(solicitud))
                                    listItem["Requisicion"] = solicitud.Trim();
                                listItem.Update();
                                if (!string.IsNullOrEmpty(solicitud))
                                {
                                    var isTwoAprovers = false;
                                    try
                                    {
                                        sqlQuery = "SELECT ISNULL(A.WFSTS, 0) FROM " + Helpers.InterCompanyId + ".dbo.LPWF00201 A "
                                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LPWF00101 B "
                                                       + "ON A.DOCNUM = B.DOCNUM "
                                                       + "WHERE A.DOCNUM = '" + solicitud + "' AND TYPE = 4";

                                        var solicitudes = _repository.ExecuteQuery<short>(sqlQuery).ToList();

                                        sqlQuery = "SELECT APPRVDBY APROBADOR "
                                                   + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 "
                                                   + "WHERE DOCNUMBR = '" + solicitud.Trim() + "'";

                                        var aprobador = _repository.ExecuteScalarQuery<string>(sqlQuery);

                                        #region Imagen

                                        if (solicitudes.Any(item => item == 4))
                                            isTwoAprovers = true;

                                        if (isTwoAprovers)
                                        {
                                            HelperLogic.InsertSignature(aprobador.Trim());
                                            HelperLogic.InsertSignaturePayment(HelperLogic.GetSecondAproverPayment());
                                        }
                                        else
                                        {
                                            if (solicitudes.Any(item => item == 2))
                                                isTwoAprovers = true;

                                            if (!isTwoAprovers)
                                            {
                                                HelperLogic.InsertSignature(aprobador.Trim());
                                                HelperLogic.InsertSignaturePayment("");
                                            }
                                            else
                                            {
                                                HelperLogic.InsertSignature("");
                                                HelperLogic.InsertSignaturePayment("");
                                            }
                                        }

                                        #endregion

                                        string xStatus = "";
                                        ReportHelper.Export(Helpers.ReportPath + "Requisicion", Helpers.ReportPath + @"Requisicion\" + solicitud.Trim() + ".pdf",
                                            $"LODYNDEV.dbo.LPPOP10200R2 '{Helpers.InterCompanyId}','{solicitud}'", 1, ref xStatus);

                                        var reader = new StreamReader(Helpers.ReportPath + @"Requisicion\" + solicitud.Trim() + ".pdf");
                                        attachInfo = new AttachmentCreationInformation
                                        {
                                            FileName = Helpers.ReportPath
                                                       + solicitud.Trim() + ".pdf",
                                            ContentStream = reader.BaseStream
                                        };

                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                    catch { }
                                }

                                if (_attachments != null)
                                {
                                    foreach (var item in _attachments)
                                    {
                                        var fileInfo = item.DataArchivo;
                                        attachInfo = new AttachmentCreationInformation
                                        {
                                            FileName = item.NombreArchivo.Trim().Replace("^", " ").Replace("@", " ").Replace("#", " ").Replace("$", " ").Replace("&", " ").Replace("*", " "),
                                            ContentStream = new MemoryStream(fileInfo)
                                        };

                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }

                                clientContext.ExecuteQuery();
                            }

                            status = "OK";
                            break;
                        }
                        catch
                        {
                            status = "En estos momentos no es posible conectar con el servidor, por favor intente de nuevo";
                            retryCount++;
                        }
                    } while (retryCount < 3);
                    break;

                #endregion
                case 2:
                    #region Solicitud de Caja Chica

                    do
                    {
                        try
                        {
                            sqlQuery = "SELECT REQUESID RequestId, DESCRIPT Description, DOCAMNT Amount, COMMENT1 Note, REQUESTBY Requester, DEPRTMID Department, CURRCYID Currency "
                                + "FROM " + Helpers.InterCompanyId + ".dbo.LPPOP30600 "
                                + "WHERE REQUESID = '" + idSolicitud + "' ";

                            var pettyCash = _repository.ExecuteScalarQuery<PettyCashRequestViewModel>(sqlQuery);

                            sqlQuery = "SELECT B.fileName NombreArchivo, B.BinaryBlob DataArchivo "
                                       + "FROM " + Helpers.InterCompanyId + ".dbo.CO00102 A "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.coAttachmentItems B "
                                       + "ON A.Attachment_ID = B.Attachment_ID "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.CO00105 C "
                                       + "ON A.Attachment_ID = C.Attachment_ID "
                                       + "WHERE C.DOCNUMBR = '" + idSolicitud + "' "
                                       + "AND C.DELETE1 = 0 ";

                            _attachments = _repository.ExecuteQuery<Attachments>(sqlQuery).ToList();

                            if (pettyCash != null)
                            {
                                clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                                foreach (char c in "Servicios2.4")
                                    securePassword.AppendChar(c);
                                clientContext.Credentials = new SharePointOnlineCredentials("no-reply@seaboardpower.com.do", securePassword);
                                listCollection = clientContext.Web.Lists.GetByTitle("Caja Chica");
                                listInformation = new ListItemCreationInformation();
                                using (clientContext)
                                {
                                    clientContext.Load(clientContext.Web);
                                    clientContext.Load(listCollection);
                                    var builder = new StringBuilder();
                                    builder.Append("<View><Query>");
                                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                                    builder.Append($"<Value Type='Text'>{idSolicitud}</Value></Eq></Where>");
                                    builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                    var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                    var collection = listCollection.GetItems(query);
                                    clientContext.Load(collection);
                                    clientContext.ExecuteQuery();

                                    if (collection.Count > 0)
                                    {
                                        foreach (var item in collection)
                                        {
                                            listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                            listItem.DeleteObject();
                                            clientContext.ExecuteQuery();
                                        }
                                    }
                                }
                                listItem = listCollection.AddItem(listInformation);
                                listItem["Solicitud"] = pettyCash.RequestId.Trim();
                                listItem["Monto"] = pettyCash.Amount.ToString(new CultureInfo("en-us")).Trim();
                                listItem["Fecha"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                                listItem["Departamento"] = pettyCash.Department;
                                listItem["Moneda"] = pettyCash.Currency;
                                listItem["Comentario"] = pettyCash.Note;
                                listItem["Descripcion"] = pettyCash.Description;
                                listItem["Solicitante"] = email;
                                listItem["DB"] = Helpers.InterCompanyId;
                                listItem.Update();

                                try
                                {
                                    string xStatus = "";
                                    HelperLogic.InsertSignature("");
                                    ReportHelper.Export(Helpers.ReportPath + "Caja", Helpers.ReportPath + @"Caja\" + idSolicitud.Trim() + ".pdf",
                                        $"LODYNDEV.dbo.LPPOP30600R1 '{Helpers.InterCompanyId}','{idSolicitud}'", 3, ref xStatus);

                                    var reader = new StreamReader(Helpers.ReportPath + @"Caja\" + idSolicitud.Trim() + ".pdf");
                                    attachInfo = new AttachmentCreationInformation
                                    {
                                        FileName = Helpers.ReportPath + idSolicitud.Trim() + ".pdf",
                                        ContentStream = reader.BaseStream
                                    };

                                    attach = listItem.AttachmentFiles.Add(attachInfo);
                                    clientContext.Load(attach);
                                }
                                catch { }

                                if (_attachments?.Count > 0)
                                {
                                    foreach (var item in _attachments)
                                    {
                                        var fileInfo = item.DataArchivo;
                                        attachInfo = new AttachmentCreationInformation
                                        {
                                            FileName = item.NombreArchivo.Trim().Replace("^", " ").Replace("@", " ").Replace("#", " ").Replace("$", " ").Replace("&", " ").Replace("*", " "),
                                            ContentStream = new MemoryStream(fileInfo)
                                        };
                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }

                                clientContext.ExecuteQuery();
                            }
                            status = "OK";
                            break;
                        }
                        catch
                        {
                            status = "En estos momentos no es posible conectar con el servidor, por favor intente de nuevo";
                            retryCount++;
                        }
                    } while (retryCount < 3);
                    break;

                    #endregion
                case 3:
                    #region Solicitud de Articulo

                    do
                    {
                        try
                        {
                            sqlQuery = "SELECT REQUESID RequestId, ITEMDESC ItemDescription, UOFM UnitId, CONVERT(nvarchar(20), ITEMTYPE) ItemType, "
                                + "CURRCOST CurrentCost, COMMENT1 Comment, CLASSID ClassId, ITEMAREA ItemArea "
                                + "FROM " + Helpers.InterCompanyId + ".dbo.LPIV00101 WHERE REQUESID = '" + idSolicitud + "'";

                            var items = _repository.ExecuteScalarQuery<ItemRequestViewModel>(sqlQuery);

                            sqlQuery = "SELECT B.fileName NombreArchivo, B.BinaryBlob DataArchivo "
                                       + "FROM " + Helpers.InterCompanyId + ".dbo.CO00102 A "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.coAttachmentItems B "
                                       + "ON A.Attachment_ID = B.Attachment_ID "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.CO00105 C "
                                       + "ON A.Attachment_ID = C.Attachment_ID "
                                       + "WHERE C.DOCNUMBR = '" + idSolicitud + "' "
                                       + "AND C.DELETE1 = 0 ";

                            _attachments = _repository.ExecuteQuery<Attachments>(sqlQuery).ToList();

                            if (items != null)
                            {
                                clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                                foreach (char c in "Servicios2.4")
                                    securePassword.AppendChar(c);
                                clientContext.Credentials = new SharePointOnlineCredentials("no-reply@seaboardpower.com.do", securePassword);
                                listCollection = clientContext.Web.Lists.GetByTitle("Articulo");
                                listInformation = new ListItemCreationInformation();

                                using (clientContext)
                                {
                                    clientContext.Load(clientContext.Web);
                                    clientContext.Load(listCollection);

                                    var builder = new StringBuilder();
                                    builder.Append("<View><Query>");
                                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                                    builder.Append("<Value Type='Text'>" + items.RequestId.Trim() + "</Value></Eq></Where>");
                                    builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                    var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                    var collection = listCollection.GetItems(query);
                                    clientContext.Load(collection);
                                    clientContext.ExecuteQuery();

                                    if (collection.Count > 0)
                                    {
                                        foreach (var item in collection)
                                        {
                                            listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                            listItem.DeleteObject();
                                            clientContext.ExecuteQuery();
                                        }
                                    }
                                }

                                listItem = listCollection.AddItem(listInformation);
                                listItem["Solicitud"] = items.RequestId.Trim();
                                listItem["Descripcion"] = items.ItemDescription.Trim();
                                listItem["Comentario"] = items.Comment.Trim();
                                listItem["Solicitante"] = email;
                                listItem["Fecha"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                                listItem["DB"] = Helpers.InterCompanyId;
                                listItem.Update();

                                if (_attachments != null)
                                {
                                    foreach (var item in _attachments)
                                    {
                                        var fileInfo = item.DataArchivo;
                                        attachInfo = new AttachmentCreationInformation
                                        {
                                            FileName = item.NombreArchivo.Trim().Replace("^", " ").Replace("@", " ").Replace("#", " ").Replace("$", " ").Replace("&", " ").Replace("*", " "),
                                            ContentStream = new MemoryStream(fileInfo)
                                        };

                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }

                                clientContext.ExecuteQuery();
                            }
                            status = "OK";
                            break;
                        }
                        catch
                        {
                            status = "En estos momentos no es posible conectar con el servidor, por favor intente de nuevo";
                            retryCount++;
                        }
                    } while (retryCount < 3);
                    break;

                    #endregion
                case 4:
                    #region Solicitud de Almacen

                    do
                    {
                        try
                        {
                            sqlQuery = "SELECT A.DOCNUMBR RequestId, A.DOCID DocumentId, CONVERT(NVARCHAR(8), A.DOCDATE, 112) DocumentDate, "
                            + "A.DEPTMTID DepartmentId, B.DEPTDESC DepartmentDesc, A.TRXLOCTN WareHouse, A.WORKNUMB WorkNumber, A.PTDUSRID Priority, "
                            + "A.STTSUSRD UserId, A.USERID UserName, A.DEX_ROW_ID RowId, ISNULL(C.USERID, '') Aprover1, ISNULL(D.USERID, '') Aprover2, "
                            + "CONVERT(NVARCHAR(8), A.POSTEDDT, 112) RequiredDate, A.TRNSTLOC AR, RTRIM(A.SRCDOCNUM) Description, RTRIM(ISNULL(E.TEXT1, '')) Note "
                            + "FROM " + Helpers.InterCompanyId + ".dbo.LLIF10100 A "
                            + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00100 B "
                            + "ON A.DEPTMTID = B.DEPTMTID "
                            + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 C "
                            + "ON A.DEPTMTID = C.DEPRTMID AND C.TYPE = 1 AND C.ISPRINC = 1 "
                            + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LPPOP40101 D "
                            + "ON A.DEPTMTID = D.DEPRTMID AND D.TYPE = 1 AND D.ISSECND = 1 "
                            + "LEFT JOIN " + Helpers.InterCompanyId + ".dbo.LLIF00140 E "
                            + "ON A.DOCNUMBR = E.DOCNUMBR "
                            + "WHERE A.DOCNUMBR = '" + idSolicitud + "'";

                            var headerLogistica = _repository.ExecuteScalarQuery<LogisticHeaderViewModel>(sqlQuery);

                            sqlQuery = "SELECT B.fileName NombreArchivo, B.BinaryBlob DataArchivo "
                                       + "FROM " + Helpers.InterCompanyId + ".dbo.CO00102 A "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.coAttachmentItems B "
                                       + "ON A.Attachment_ID = B.Attachment_ID "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.CO00105 C "
                                       + "ON A.Attachment_ID = C.Attachment_ID "
                                       + "WHERE C.DOCNUMBR = '" + idSolicitud + "' "
                                       + "AND C.DELETE1 = 0 ";

                            _attachments = _repository.ExecuteQuery<Attachments>(sqlQuery).ToList();

                            if (headerLogistica != null)
                            {
                                clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                                foreach (char c in "Servicios2.4")
                                    securePassword.AppendChar(c);
                                clientContext.Credentials = new SharePointOnlineCredentials("no-reply@seaboardpower.com.do", securePassword);
                                listCollection = clientContext.Web.Lists.GetByTitle("Almacen");
                                listInformation = new ListItemCreationInformation();

                                using (clientContext)
                                {
                                    clientContext.Load(clientContext.Web);
                                    clientContext.Load(listCollection);

                                    var builder = new StringBuilder();
                                    builder.Append("<View><Query>");
                                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                                    builder.Append("<Value Type='Text'>" + headerLogistica.RequestId.Trim() + "</Value></Eq></Where>");
                                    builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                    var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                    var collection = listCollection.GetItems(query);
                                    clientContext.Load(collection);
                                    clientContext.ExecuteQuery();

                                    if (collection.Count > 0)
                                    {
                                        foreach (var item in collection)
                                        {
                                            listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                            listItem.DeleteObject();
                                            clientContext.ExecuteQuery();
                                        }
                                    }
                                }

                                listItem = listCollection.AddItem(listInformation);
                                listItem["Solicitud"] = headerLogistica.RequestId.Trim();
                                listItem["Fecha"] = DateTime.ParseExact(headerLogistica.DocumentDate, "yyyyMMdd", null).ToString(CultureInfo.InvariantCulture);
                                listItem["FechaRequerida"] = DateTime.ParseExact(headerLogistica.RequiredDate, "yyyyMMdd", null).ToString(CultureInfo.InvariantCulture);
                                listItem["Departamento"] = headerLogistica.DepartmentDesc.Trim();
                                listItem["AR"] = headerLogistica.Ar.Trim();
                                listItem["Descripcion"] = headerLogistica.Description.Trim();
                                listItem["Nota"] = headerLogistica.Note.Trim();
                                listItem["Prioridad"] = headerLogistica.Priority.Trim();
                                listItem["Solicitante"] = email;
                                listItem["DB"] = Helpers.InterCompanyId;
                                listItem.Update();

                                if (_attachments != null)
                                {
                                    foreach (var item in _attachments)
                                    {
                                        var fileInfo = item.DataArchivo;
                                        attachInfo = new AttachmentCreationInformation
                                        {
                                            FileName = item.NombreArchivo.Trim().Replace("^", " ").Replace("@", " ").Replace("#", " ").Replace("$", " ").Replace("&", " ").Replace("*", " "),
                                            ContentStream = new MemoryStream(fileInfo)
                                        };

                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }

                                clientContext.ExecuteQuery();
                            }
                            status = "OK";
                            break;
                        }
                        catch
                        {
                            status = "En estos momentos no es posible conectar con el servidor, por favor intente de nuevo";
                            retryCount++;
                        }
                    } while (retryCount < 3);
                    break;

                    #endregion
                case 5:
                    #region Solicitud de ausencia

                    do
                    {
                        try
                        {
                            sqlQuery = "SELECT A.RequestId, RTRIM(B.EMPLOYID) EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartDate, A.EndDate, A.UnitDays, A.Note, " +
                            "CASE A.AbsenceType WHEN 1 THEN 'Vacaciones' WHEN 2 THEN 'Permiso' WHEN 3 THEN 'Duelo' WHEN 4 THEN 'Paternidad' " +
                            "WHEN 5 THEN 'Maternidad' WHEN 6 THEN 'Matrimonio' WHEN 7 THEN 'Cumpleaños' WHEN 8 THEN 'Licencia Medica' " +
                            "WHEN 9 THEN 'Cita Medica' ELSE 'Vacaciones' END AbsenceType, RTRIM(C.DSCRIPTN) DepartmentId, A.AvailableDays, A.RowId, " +
                            "CASE WHEN DATEADD(YEAR,DATEDIFF(YEAR, B.STRTDATE, GETDATE()), B.STRTDATE) > GETDATE() " +
                            "THEN DATEDIFF(YEAR, B.STRTDATE, GETDATE()) - 1 ELSE DATEDIFF(YEAR, B.STRTDATE, GETDATE()) END Seniority " +
                            $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30100 A " +
                            $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                            $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR40300 C ON B.DEPRTMNT = C.DEPRTMNT " +
                            $"WHERE A.RowId = '{idSolicitud}' ";

                            var ausencias = _repository.ExecuteScalarQuery<AbsenceRequest>(sqlQuery);

                            if (ausencias != null)
                            {
                                clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                                foreach (char c in "Servicios2.4")
                                    securePassword.AppendChar(c);
                                clientContext.Credentials = new SharePointOnlineCredentials("no-reply@seaboardpower.com.do", securePassword);
                                listCollection = clientContext.Web.Lists.GetByTitle("Ausencia");
                                listInformation = new ListItemCreationInformation();

                                using (clientContext)
                                {
                                    clientContext.Load(clientContext.Web);
                                    clientContext.Load(listCollection);
                                    var builder = new StringBuilder();
                                    builder.Append("<View><Query>");
                                    builder.Append("<Where><Eq><FieldRef Name='Title'/>");
                                    builder.Append("<Value Type='Text'>" + ausencias.RowId + "</Value></Eq></Where>");
                                    builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                    var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                    var collection = listCollection.GetItems(query);
                                    clientContext.Load(collection);
                                    clientContext.ExecuteQuery();

                                    if (collection.Count > 0)
                                    {
                                        foreach (var item in collection)
                                        {
                                            listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                            listItem.DeleteObject();
                                            clientContext.ExecuteQuery();
                                        }
                                    }
                                }

                                var departmentoCodigo = _repository.ExecuteScalarQuery<string>($"SELECT Department FROM INTRANET.dbo.USERS WHERE EmployeeId = '{ausencias.EmployeeId}'");

                                listItem = listCollection.AddItem(listInformation);
                                listItem["Title"] = ausencias.RowId.ToString();
                                listItem["Solicitud"] = ausencias.RequestId.ToString();
                                listItem["Codigo"] = ausencias.EmployeeId.ToString();
                                listItem["Empleado"] = ausencias.EmployeeName.ToString();
                                listItem["Departamento"] = ausencias.DepartmentId.ToString();
                                listItem["DepartamentoCodigo"] = departmentoCodigo;
                                listItem["FechaInicio"] = ausencias.StartDate.ToString(CultureInfo.InvariantCulture);
                                listItem["FechaFin"] = ausencias.EndDate.ToString(CultureInfo.InvariantCulture);
                                listItem["Dias"] = ausencias.UnitDays;
                                listItem["Fecha"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                                listItem["Nota"] = ausencias.Note.Trim();
                                listItem["Antiguedad"] = ausencias.Seniority;
                                listItem["DiasRestantes"] = ausencias.AvailableDays;
                                listItem["TipoAusencia"] = ausencias.AbsenceType;
                                listItem["Solicitante"] = email;
                                listItem["DB"] = Helpers.InterCompanyId;
                                listItem.Update();

                                var xStatus = "";
                                ReportHelper.Export(Helpers.ReportPath + "Reportes", HttpContext.Current.Server.MapPath("~/PDF/Reportes/") + "AbsenceRequestReport.pdf",
                                    string.Format("INTRANET.dbo.AbsenceRequestReport '{0}','{1}'", Helpers.InterCompanyId, idSolicitud), 35, ref xStatus);

                                var reader = new StreamReader(HttpContext.Current.Server.MapPath("~/PDF/Reportes/") + "AbsenceRequestReport.pdf");
                                attachInfo = new AttachmentCreationInformation();
                                attachInfo.FileName = HttpContext.Current.Server.MapPath("~/PDF/Reportes/") + "AbsenceRequestReport.pdf";
                                attachInfo.ContentStream = reader.BaseStream;

                                attach = listItem.AttachmentFiles.Add(attachInfo);
                                clientContext.Load(attach);
                                clientContext.ExecuteQuery();
                            }
                            status = "OK";
                            break;
                        }
                        catch
                        {
                            status = "En estos momentos no es posible conectar con el servidor, por favor intente de nuevo";
                            retryCount++;
                        }
                    } while (retryCount < 3);
                    break;

                #endregion
                case 6:
                    #region Solicitud de entrenamiento

                    do
                    {
                        try
                        {
                            sqlQuery = "SELECT A.RequestId, A.[Description], A.StartDate, A.EmployeeId, RTRIM(B.EMPLOYID) EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, " +
                                "A.Duration, A.Department, A.Cost, CASE A.CurrencyId WHEN '1' THEN 'USD' WHEN '2' THEN 'DOP' ELSE 'EUR' END CurrencyId, " +
                                $"CASE A.[Location] WHEN '1' THEN 'Onsite' WHEN '2' THEN 'Online' WHEN '3' THEN 'Local' ELSE 'Internacional' END [Location], A.Supplier, A.Objectives, A.Requirements, A.Participants, " +
                                $"A.IsCompleted, A.RowId FROM {Helpers.InterCompanyId}.dbo.EFUPR30400 A " +
                                $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                                $"WHERE A.RowId = '{idSolicitud}' ";

                            var trainingRequest = _repository.ExecuteScalarQuery<TrainingRequest>(sqlQuery);
                            var participantes = "";
                            string departamento = _repository.ExecuteScalarQuery<string>($"SELECT RTRIM(DEPRTMDS) FROM {Helpers.InterCompanyId}.dbo.LPPOP40100 WHERE DEPRTMID = '{trainingRequest.Department}'");
                            trainingRequest.Participants.Split(',').ToList().ForEach(p =>
                            {
                                var user = _repository.ExecuteScalarQuery<string>($"SELECT FirstName + ' ' + LastName FROM INTRANET.dbo.USERS WHERE EmployeeId = '{p}'") ?? "";
                                if (!string.IsNullOrEmpty(user))
                                    participantes += user + ",";
                            });
                            sqlQuery = "SELECT B.fileName NombreArchivo, B.BinaryBlob DataArchivo "
                                       + "FROM " + Helpers.InterCompanyId + ".dbo.CO00102 A "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.coAttachmentItems B "
                                       + "ON A.Attachment_ID = B.Attachment_ID "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.CO00105 C "
                                       + "ON A.Attachment_ID = C.Attachment_ID "
                                       + "WHERE C.DOCNUMBR = 'TrainingReq" + idSolicitud + "' "
                                       + "AND C.DELETE1 = 0 ";

                            _attachments = _repository.ExecuteQuery<Attachments>(sqlQuery).ToList();
                            if (trainingRequest != null)
                            {
                                clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                                foreach (char c in "Servicios2.4")
                                    securePassword.AppendChar(c);
                                clientContext.Credentials = new SharePointOnlineCredentials("no-reply@seaboardpower.com.do", securePassword);
                                listCollection = clientContext.Web.Lists.GetByTitle("Entrenamiento");
                                listInformation = new ListItemCreationInformation();

                                using (clientContext)
                                {
                                    clientContext.Load(clientContext.Web);
                                    clientContext.Load(listCollection);
                                    var builder = new StringBuilder();
                                    builder.Append("<View><Query>");
                                    builder.Append("<Where><Eq><FieldRef Name='Title'/>");
                                    builder.Append("<Value Type='Text'>" + trainingRequest.RowId + "</Value></Eq></Where>");
                                    builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                    var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                    var collection = listCollection.GetItems(query);
                                    clientContext.Load(collection);
                                    clientContext.ExecuteQuery();

                                    if (collection.Count > 0)
                                    {
                                        foreach (var item in collection)
                                        {
                                            listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                            listItem.DeleteObject();
                                            clientContext.ExecuteQuery();
                                        }
                                    }
                                }

                                listItem = listCollection.AddItem(listInformation);
                                listItem["Title"] = trainingRequest.RowId.ToString();
                                listItem["Solicitud"] = trainingRequest.RequestId.ToString();
                                listItem["Curso"] = trainingRequest.Description.ToString();
                                listItem["Duracion"] = trainingRequest.Duration.ToString();
                                listItem["Fecha"] = trainingRequest.StartDate.ToString(CultureInfo.InvariantCulture);
                                listItem["Costo"] = trainingRequest.Cost;
                                listItem["Moneda"] = trainingRequest.CurrencyId.ToString().Trim();
                                listItem["Ubicacion"] = trainingRequest.Location.ToString().Trim();
                                listItem["Suplidor"] = trainingRequest.Supplier.Trim();
                                listItem["Necesidad"] = trainingRequest.Requirements.Trim();
                                listItem["Objetivos"] = trainingRequest.Objectives.Trim();
                                listItem["Departamento"] = departamento;
                                listItem["EsCompleto"] = trainingRequest.IsCompleted ? "SI" : "NO";
                                listItem["Participantes"] = participantes;
                                listItem["Solicitante"] = email;
                                listItem["Empleado"] = trainingRequest.EmployeeId.Trim() + " - " + trainingRequest.EmployeeName.Trim();
                                listItem["DB"] = Helpers.InterCompanyId;
                                listItem.Update();

                                if (_attachments?.Count > 0)
                                {
                                    foreach (var item in _attachments)
                                    {
                                        var fileInfo = item.DataArchivo;
                                        attachInfo = new AttachmentCreationInformation
                                        {
                                            FileName = item.NombreArchivo.Trim().Replace("^", " ").Replace("@", " ").Replace("#", " ").Replace("$", " ").Replace("&", " ").Replace("*", " "),
                                            ContentStream = new MemoryStream(fileInfo)
                                        };
                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }

                                clientContext.ExecuteQuery();
                            }
                            status = "OK";
                            break;
                        }
                        catch
                        {
                            status = "En estos momentos no es posible conectar con el servidor, por favor intente de nuevo";
                            retryCount++;
                        }
                    } while (retryCount < 3);
                    break;

                #endregion
                case 7:
                    #region Solicitud de creacion de usuario

                    do
                    {
                        try
                        {
                            sqlQuery = "SELECT A.RequestId, A.EmployeeId, RTRIM(B.EMPLOYID) EmployeeId, RTRIM(B.FRSTNAME) + ' ' + RTRIM(B.LASTNAME) EmployeeName, A.StartTime, A.EndTime, A.DaysWork, " +
                            "A.Resources, A.Comments, A.IsPolicy, A.Status, A.RowId, A.Department, " +
                            "CASE A.EmailAccount WHEN 1 THEN 'Interno' ELSE 'Externo' END EmailAccount, CASE A.InternetAccess WHEN 1 THEN 'Ilimitado' ELSE 'Limitado' END InternetAccess " +
                            $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30500 A " +
                            $"INNER JOIN {Helpers.InterCompanyId}.dbo.UPR00100 B ON A.EmployeeId = B.EMPLOYID " +
                            $"WHERE A.RowId = '{idSolicitud}' ";

                            var userRequest = _repository.ExecuteScalarQuery<UserRequest>(sqlQuery);

                            sqlQuery = "SELECT B.fileName NombreArchivo, B.BinaryBlob DataArchivo "
                                       + "FROM " + Helpers.InterCompanyId + ".dbo.CO00102 A "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.coAttachmentItems B "
                                       + "ON A.Attachment_ID = B.Attachment_ID "
                                       + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.CO00105 C "
                                       + "ON A.Attachment_ID = C.Attachment_ID "
                                       + "WHERE C.DOCNUMBR = 'UserReq" + idSolicitud + "' "
                                       + "AND C.DELETE1 = 0 ";

                            _attachments = _repository.ExecuteQuery<Attachments>(sqlQuery).ToList();
                            var days = "";
                            var resources = "";
                            userRequest.DaysWork.Split(',').ToList().ForEach(p =>
                            {
                                switch (p)
                                {
                                    case "1":
                                        days += "Domingo";
                                        break;
                                    case "2":
                                        days += "Lunes";
                                        break;
                                    case "3":
                                        days += "Martes";
                                        break;
                                    case "4":
                                        days += "Miercoles";
                                        break;
                                    case "5":
                                        days += "Jueves";
                                        break;
                                    case "6":
                                        days += "Viernes";
                                        break;
                                    case "7":
                                        days += "Sabado";
                                        break;
                                }
                                days += ",";
                            });
                            userRequest.Resources.Split(',').ToList().ForEach(p =>
                            {
                                switch (p)
                                {
                                    case "1":
                                        resources += "Acceso VPN";
                                        break;
                                    case "2":
                                        resources += "MS Dynamics GP (Compras)";
                                        break;
                                    case "3":
                                        resources += "MS Dynamics GP (Ventas)";
                                        break;
                                    case "4":
                                        resources += "MS Dynamics GP (Nomina)";
                                        break;
                                    case "5":
                                        resources += "MS Dynamics GP (RRHH)";
                                        break;
                                    case "6":
                                        resources += "MS Dynamics GP (CxP & CxC)";
                                        break;
                                    case "7":
                                        resources += "MS Dynamics GP (Requisiciones)";
                                        break;
                                    case "8":
                                        resources += "MS Office";
                                        break;
                                    case "9":
                                        resources += "MS Windows";
                                        break;
                                    case "10":
                                        resources += "Oracle JDE";
                                        break;
                                    case "11":
                                        resources += "Perfil Intranet";
                                        break;
                                    case "12":
                                        resources += "Equipo Portatil";
                                        break;
                                    case "13":
                                        resources += "Equipo sobremesa";
                                        break;
                                    case "14":
                                        resources += "Telefono móvil";
                                        break;
                                    case "15":
                                        resources += "Telefono sobremesa";
                                        break;
                                }
                                resources += ",";
                            });
                            string departamento = _repository.ExecuteScalarQuery<string>($"SELECT RTRIM(DEPRTMDS) FROM {Helpers.InterCompanyId}.dbo.LPPOP40100 WHERE DEPRTMID = '{userRequest.Department}'");
                            if (userRequest != null)
                            {
                                clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                                foreach (char c in "Servicios2.4")
                                    securePassword.AppendChar(c);
                                clientContext.Credentials = new SharePointOnlineCredentials("no-reply@seaboardpower.com.do", securePassword);
                                listCollection = clientContext.Web.Lists.GetByTitle("Usuario");
                                listInformation = new ListItemCreationInformation();

                                using (clientContext)
                                {
                                    clientContext.Load(clientContext.Web);
                                    clientContext.Load(listCollection);
                                    var builder = new StringBuilder();
                                    builder.Append("<View><Query>");
                                    builder.Append("<Where><Eq><FieldRef Name='Title'/>");
                                    builder.Append("<Value Type='Text'>" + userRequest.RowId + "</Value></Eq></Where>");
                                    builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                    var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                    var collection = listCollection.GetItems(query);
                                    clientContext.Load(collection);
                                    clientContext.ExecuteQuery();

                                    if (collection.Count > 0)
                                    {
                                        foreach (var item in collection)
                                        {
                                            listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                            listItem.DeleteObject();
                                            clientContext.ExecuteQuery();
                                        }
                                    }
                                }

                                listItem = listCollection.AddItem(listInformation);
                                listItem["Title"] = userRequest.RowId.ToString();
                                listItem["Solicitud"] = userRequest.RequestId.Trim();
                                listItem["Departamento"] = departamento;
                                listItem["CuentaEmail"] = userRequest.EmailAccount.Trim();
                                listItem["Internet"] = userRequest.InternetAccess;
                                listItem["HoraInicio"] = userRequest.StartTime;
                                listItem["HoraSalida"] = userRequest.EndTime.Trim();
                                listItem["DiasTrabajo"] = days;
                                listItem["Fecha"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                                listItem["Recursos"] = resources;
                                listItem["Comentario"] = userRequest.Comments.Trim();
                                listItem["PoliticasIT"] = userRequest.IsPolicy ? "SI" : "NO";
                                listItem["Solicitante"] = email;
                                listItem["Empleado"] = userRequest.EmployeeId.Trim() + " - " + userRequest.EmployeeName.Trim();
                                listItem["DB"] = Helpers.InterCompanyId;
                                listItem.Update();

                                if (_attachments?.Count > 0)
                                {
                                    foreach (var item in _attachments)
                                    {
                                        var fileInfo = item.DataArchivo;
                                        attachInfo = new AttachmentCreationInformation
                                        {
                                            FileName = item.NombreArchivo.Trim().Replace("^", " ").Replace("@", " ").Replace("#", " ").Replace("$", " ").Replace("&", " ").Replace("*", " "),
                                            ContentStream = new MemoryStream(fileInfo)
                                        };
                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }

                                clientContext.ExecuteQuery();
                            }
                            status = "OK";
                            break;
                        }
                        catch
                        {
                            status = "En estos momentos no es posible conectar con el servidor, por favor intente de nuevo";
                            retryCount++;
                        }
                    } while (retryCount < 3);
                    break;

                #endregion
                case 8:
                    #region Solicitud de overtime

                    do
                    {
                        try
                        {
                            sqlQuery = "SELECT A.RowId, A.BatchNumber, A.Description, A.Note, A.NumberOfTransactions, A.Approver " +
                            $"FROM {Helpers.InterCompanyId}.dbo.EFUPR30300 A " +
                            $"WHERE A.RowId = '{idSolicitud}' ";
                            var overtimeApproval = _repository.ExecuteScalarQuery<OvertimeApproval>(sqlQuery);

                            sqlQuery = "SELECT B.fileName NombreArchivo, B.BinaryBlob DataArchivo "
                                      + "FROM " + Helpers.InterCompanyId + ".dbo.CO00102 A "
                                      + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.coAttachmentItems B "
                                      + "ON A.Attachment_ID = B.Attachment_ID "
                                      + "INNER JOIN " + Helpers.InterCompanyId + ".dbo.CO00105 C "
                                      + "ON A.Attachment_ID = C.Attachment_ID "
                                      + "WHERE C.DOCNUMBR = '" + "Overtime" + idSolicitud + "' "
                                      + "AND C.DELETE1 = 0 ";

                            _attachments = _repository.ExecuteQuery<Attachments>(sqlQuery).ToList();
                            if (overtimeApproval != null)
                            {
                                clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                                foreach (char c in "Servicios2.4")
                                    securePassword.AppendChar(c);
                                clientContext.Credentials = new SharePointOnlineCredentials("no-reply@seaboardpower.com.do", securePassword);
                                listCollection = clientContext.Web.Lists.GetByTitle("Overtime");
                                listInformation = new ListItemCreationInformation();

                                using (clientContext)
                                {
                                    clientContext.Load(clientContext.Web);
                                    clientContext.Load(listCollection);
                                    var builder = new StringBuilder();
                                    builder.Append("<View><Query>");
                                    builder.Append("<Where><Eq><FieldRef Name='Title'/>");
                                    builder.Append("<Value Type='Text'>" + overtimeApproval.RowId + "</Value></Eq></Where>");
                                    builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                    var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                    var collection = listCollection.GetItems(query);
                                    clientContext.Load(collection);
                                    clientContext.ExecuteQuery();

                                    if (collection.Count > 0)
                                    {
                                        foreach (var item in collection)
                                        {
                                            listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                            listItem.DeleteObject();
                                            clientContext.ExecuteQuery();
                                        }
                                    }
                                }

                                listItem = listCollection.AddItem(listInformation);
                                listItem["Title"] = overtimeApproval.RowId.ToString();
                                listItem["Lote"] = overtimeApproval.BatchNumber.Trim();
                                listItem["Fecha"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                                listItem["Departamento"] = overtimeApproval.Approver;
                                listItem["Descripcion"] = overtimeApproval.Description.Trim();
                                listItem["Nota"] = overtimeApproval.Note.Trim();
                                listItem["Solicitante"] = email;
                                listItem["DB"] = Helpers.InterCompanyId;
                                listItem.Update();

                                var xStatus = "";
                                ReportHelper.Export(Helpers.ReportPath + "Reportes", HttpContext.Current.Server.MapPath("~/PDF/Reportes/") + "ApprovalOvertimeReport.pdf",
                                    string.Format("INTRANET.dbo.ApprovalOvertimeReport '{0}','{1}'", Helpers.InterCompanyId, idSolicitud), 36, ref xStatus);

                                var reader = new StreamReader(HttpContext.Current.Server.MapPath("~/PDF/Reportes/") + "ApprovalOvertimeReport.pdf");
                                attachInfo = new AttachmentCreationInformation();
                                attachInfo.FileName = HttpContext.Current.Server.MapPath("~/PDF/Reportes/") + "ApprovalOvertimeReport.pdf";
                                attachInfo.ContentStream = reader.BaseStream;

                                attach = listItem.AttachmentFiles.Add(attachInfo);
                                clientContext.Load(attach);

                                ReportHelper.Export(Helpers.ReportPath + "Reportes", HttpContext.Current.Server.MapPath("~/PDF/Reportes/") + "OvertimeReportDetail.pdf",
                                    string.Format("INTRANET.dbo.PreOvertimeReportDetail '{0}','{1}'", Helpers.InterCompanyId, overtimeApproval.BatchNumber.Trim()), 38, ref xStatus);

                                reader = new StreamReader(HttpContext.Current.Server.MapPath("~/PDF/Reportes/") + "OvertimeReportDetail.pdf");
                                attachInfo = new AttachmentCreationInformation();
                                attachInfo.FileName = HttpContext.Current.Server.MapPath("~/PDF/Reportes/") + "OvertimeReportDetail.pdf";
                                attachInfo.ContentStream = reader.BaseStream;

                                attach = listItem.AttachmentFiles.Add(attachInfo);
                                clientContext.Load(attach);

                                if (_attachments?.Count > 0)
                                {
                                    foreach (var item in _attachments)
                                    {
                                        var fileInfo = item.DataArchivo;
                                        attachInfo = new AttachmentCreationInformation
                                        {
                                            FileName = item.NombreArchivo.Trim().Replace("^", " ").Replace("@", " ").Replace("#", " ").Replace("$", " ").Replace("&", " ").Replace("*", " "),
                                            ContentStream = new MemoryStream(fileInfo)
                                        };
                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }
                                clientContext.ExecuteQuery();
                            }
                            break;
                        }
                        catch(Exception ex)
                        {
                            status = ex.Message;
                            retryCount++;
                        }
                    } while (retryCount < 3);
                    break;

                #endregion
                case 9:
                    #region Orden de Compra

                    DataTable Header = ConnectionDb.GetDt(String.Format("LODYNDEV.dbo.LPPOP10100S2 '{0}','{1}'", Helpers.InterCompanyId, idSolicitud));
                    DataTable Attachments;

                    if (Header != null)
                    {
                        if (Header.Rows.Count > 0)
                        {
                            clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                            foreach (char c in "Servicios2.4")
                                securePassword.AppendChar(c);
                            clientContext.Credentials = new SharePointOnlineCredentials(email, securePassword);
                            listCollection = clientContext.Web.Lists.GetByTitle("Compra");
                            listInformation = new ListItemCreationInformation();

                            using (clientContext)
                            {
                                clientContext.Load(clientContext.Web);
                                clientContext.Load(listCollection);
                                var builder = new StringBuilder();
                                builder.Append("<View><Query>");
                                builder.Append("<Where><Eq><FieldRef Name='OrdenNum'/>");
                                builder.Append("<Value Type='Text'>" + Header.Rows[0]["PONUMBER"].ToString().Trim() + "</Value></Eq></Where>");
                                builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                var collection = listCollection.GetItems(query);
                                clientContext.Load(collection);
                                clientContext.ExecuteQuery();

                                if (collection.Count > 0)
                                {
                                    foreach (var item in collection)
                                    {
                                        listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                        listItem.DeleteObject();
                                        clientContext.ExecuteQuery();
                                    }
                                }
                            }

                            listItem = listCollection.AddItem(listInformation);
                            listItem["OrdenNum"] = Header.Rows[0]["PONUMBER"].ToString().Trim();
                            listItem["Fecha"] = DateTime.ParseExact(Header.Rows[0]["DOCDATE"].ToString(), "yyyyMMdd", null).ToString();
                            listItem["Moneda"] = Header.Rows[0]["CURNCYID"].ToString().Trim();
                            listItem["Requisicion"] = Header.Rows[0]["REQUISICION"].ToString().Trim();
                            listItem["Condicion"] = Header.Rows[0]["PYMTRMID"].ToString().Trim();
                            listItem["Analisis"] = Header.Rows[0]["ANALISIS"].ToString().Trim();
                            listItem["Suplidor"] = Header.Rows[0]["VENDNAME"].ToString().Trim();
                            listItem["Total"] = Header.Rows[0]["ONORDAMT"].ToString().Trim();
                            listItem["Flete"] = Header.Rows[0]["FRTAMNT"].ToString().Trim();
                            listItem["Descuento"] = Header.Rows[0]["TRDISAMT"].ToString().Trim();
                            listItem["Itbis"] = Header.Rows[0]["TAXAMNT"].ToString().Trim();
                            listItem["Miscelaneo"] = Header.Rows[0]["MSCCHAMT"].ToString().Trim();
                            listItem["Departamento"] = Header.Rows[0]["DEPARTAMENTO"].ToString().Trim();
                            listItem["Nota"] = Header.Rows[0]["NOTA"].ToString().Trim();
                            listItem["DB"] = Helpers.InterCompanyId;
                            listItem.Update();

                            #region Adjuntos Orden De Compras

                            Attachments = ConnectionDb.GetDt(String.Format("LODYNDEV.dbo.LPCO00100S1 '{0}','{1}'", Helpers.InterCompanyId, idSolicitud));

                            if (Attachments != null)
                            {
                                if (Attachments.Rows.Count > 0)
                                {
                                    foreach (DataRow item in Attachments.Rows)
                                    {
                                        Byte[] FileInfo;
                                        FileInfo = Encoding.UTF8.GetBytes(String.Empty);
                                        FileInfo = (Byte[])item[1];
                                        attachInfo = new AttachmentCreationInformation();
                                        attachInfo.FileName = item[0].ToString().Trim()
                                            .Replace("^", " ").Replace("@", " ").Replace("#", " ")
                                            .Replace("$", " ").Replace("&", " ").Replace("*", " ");
                                        attachInfo.ContentStream = new MemoryStream(FileInfo);

                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }
                            }

                            #endregion

                            #region Adjuntos Analisis de Compras

                            Attachments = ConnectionDb.GetDt(String.Format("LODYNDEV.dbo.LPCO00100S1 '{0}','{1}'", Helpers.InterCompanyId, Header.Rows[0]["ANALISIS"].ToString().Trim()));

                            if (Attachments != null)
                            {
                                if (Attachments.Rows.Count > 0)
                                {
                                    foreach (DataRow item in Attachments.Rows)
                                    {
                                        Byte[] FileInfo;
                                        FileInfo = Encoding.UTF8.GetBytes(String.Empty);
                                        FileInfo = (Byte[])item[1];
                                        attachInfo = new AttachmentCreationInformation();
                                        attachInfo.FileName = item[0].ToString().Trim()
                                            .Replace("^", " ").Replace("@", " ").Replace("#", " ")
                                            .Replace("$", " ").Replace("&", " ").Replace("*", " ");
                                        attachInfo.ContentStream = new MemoryStream(FileInfo);

                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }
                            }

                            #endregion

                            #region Adjuntos de Solicitud de Cotizacion

                            Attachments = ConnectionDb.GetDt(String.Format("LODYNDEV.dbo.LPCO00100S1 '{0}','{1}'", Helpers.InterCompanyId, Header.Rows[0]["QUOTREQS"].ToString().Trim()));

                            if (Attachments != null)
                            {
                                if (Attachments.Rows.Count > 0)
                                {
                                    foreach (DataRow item in Attachments.Rows)
                                    {
                                        Byte[] FileInfo;
                                        FileInfo = Encoding.UTF8.GetBytes(String.Empty);
                                        FileInfo = (Byte[])item[1];
                                        attachInfo = new AttachmentCreationInformation();
                                        attachInfo.FileName = item[0].ToString().Trim()
                                            .Replace("^", " ").Replace("@", " ").Replace("#", " ")
                                            .Replace("$", " ").Replace("&", " ").Replace("*", " ");
                                        attachInfo.ContentStream = new MemoryStream(FileInfo);

                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }
                            }

                            #endregion

                            #region Adjuntos Solicitud de Compras

                            Attachments = ConnectionDb.GetDt(String.Format("LODYNDEV.dbo.LPCO00100S1 '{0}','{1}'", Helpers.InterCompanyId, Header.Rows[0]["REQUISICION"].ToString().Trim()));

                            if (Attachments != null)
                            {
                                if (Attachments.Rows.Count > 0)
                                {
                                    foreach (DataRow item in Attachments.Rows)
                                    {
                                        Byte[] FileInfo;
                                        FileInfo = Encoding.UTF8.GetBytes(String.Empty);
                                        FileInfo = (Byte[])item[1];
                                        attachInfo = new AttachmentCreationInformation();
                                        attachInfo.FileName = item[0].ToString().Trim()
                                            .Replace("^", " ").Replace("@", " ").Replace("#", " ")
                                            .Replace("$", " ").Replace("&", " ").Replace("*", " ");
                                        attachInfo.ContentStream = new MemoryStream(FileInfo);

                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }
                            }

                            #endregion

                            clientContext.ExecuteQuery();
                        }
                    }

                    break;

                #endregion
                case 10:
                    #region Analisis de Compra

                    DataTable Analisis = ConnectionDb.GetDt("LODYNDEV.dbo.LPPOP30100S2 @INTERID = '" + Helpers.InterCompanyId + "', @ANLREQUS = '" + idSolicitud + "'");
                    Attachments = ConnectionDb.GetDt(String.Format("LODYNDEV.dbo.LPCO00100S2 '{0}','{1}','{2}'", Helpers.InterCompanyId, idSolicitud, Analisis.Rows[0]["QUOTREQS"].ToString().Trim()));
                    if (Analisis != null)
                    {
                        if (Analisis.Rows.Count > 0)
                        {
                            clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                            foreach (char c in "Servicios2.4")
                                securePassword.AppendChar(c);
                            clientContext.Credentials = new SharePointOnlineCredentials(email, securePassword);
                            listCollection = clientContext.Web.Lists.GetByTitle("Analisis");
                            listInformation = new ListItemCreationInformation();

                            using (clientContext)
                            {
                                clientContext.Load(clientContext.Web);
                                clientContext.Load(listCollection);
                                var builder = new StringBuilder();
                                builder.Append("<View><Query>");
                                builder.Append("<Where><Eq><FieldRef Name='Analisis'/>");
                                builder.Append("<Value Type='Text'>" + Analisis.Rows[0]["ANLREQUS"].ToString().Trim() + "</Value></Eq></Where>");
                                builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                var collection = listCollection.GetItems(query);
                                clientContext.Load(collection);
                                clientContext.ExecuteQuery();

                                if (collection.Count > 0)
                                {
                                    foreach (var item in collection)
                                    {
                                        listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                        listItem.DeleteObject();
                                        clientContext.ExecuteQuery();
                                    }
                                }
                            }

                            listItem = listCollection.AddItem(listInformation);
                            listItem["Analisis"] = Analisis.Rows[0]["ANLREQUS"].ToString().Trim();
                            listItem["Fecha"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                            listItem["Moneda"] = Analisis.Rows[0]["CURNCYID"].ToString().Trim();
                            listItem["Departamento"] = Analisis.Rows[0]["DEPARTAMENTO"].ToString().Trim();
                            listItem["Requisicion"] = Analisis.Rows[0]["PURCHREQ"].ToString().Trim();
                            listItem["DB"] = Helpers.InterCompanyId;
                            listItem.Update();

                            #region Adjuntos Orden De Compras

                            if (Attachments != null)
                            {
                                if (Attachments.Rows.Count > 0)
                                {
                                    foreach (DataRow item in Attachments.Rows)
                                    {
                                        Byte[] FileInfo;
                                        FileInfo = Encoding.UTF8.GetBytes(String.Empty);
                                        FileInfo = (Byte[])item[1];
                                        attachInfo = new AttachmentCreationInformation();
                                        attachInfo.FileName = item[0].ToString().Trim()
                                            .Replace("^", " ").Replace("@", " ").Replace("#", " ")
                                            .Replace("$", " ").Replace("&", " ").Replace("*", " ");
                                        attachInfo.ContentStream = new MemoryStream(FileInfo);

                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }
                            }

                            #endregion

                            clientContext.ExecuteQuery();
                        }
                    }

                    break;
                #endregion
                case 11:
                    #region Solicitud de Proveedor

                    DataTable Vendors = ConnectionDb.GetDt(String.Format("LODYNDEV.dbo.LPPM00200S2 '{0}','{1}'", Helpers.InterCompanyId, idSolicitud));
                    Attachments = ConnectionDb.GetDt(String.Format("LODYNDEV.dbo.LPCO00100S1 '{0}','{1}'", Helpers.InterCompanyId, idSolicitud));

                    if (Vendors != null)
                    {
                        if (Vendors.Rows.Count > 0)
                        {
                            clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                            foreach (char c in "Servicios2.4")
                                securePassword.AppendChar(c);
                            clientContext.Credentials = new SharePointOnlineCredentials(email, securePassword);
                            listCollection = clientContext.Web.Lists.GetByTitle("Proveedor");
                            listInformation = new ListItemCreationInformation();

                            using (clientContext)
                            {
                                clientContext.Load(clientContext.Web);
                                clientContext.Load(listCollection);
                                var builder = new StringBuilder();
                                builder.Append("<View><Query>");
                                builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                                builder.Append("<Value Type='Text'>" + Vendors.Rows[0]["REQUESID"].ToString().Trim() + "</Value></Eq></Where>");
                                builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                var collection = listCollection.GetItems(query);
                                clientContext.Load(collection);
                                clientContext.ExecuteQuery();

                                if (collection.Count > 0)
                                {
                                    foreach (var item in collection)
                                    {
                                        listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                        listItem.DeleteObject();
                                        clientContext.ExecuteQuery();
                                    }
                                }
                            }

                            listItem = listCollection.AddItem(listInformation);
                            listItem["Solicitud"] = Vendors.Rows[0]["REQUESID"].ToString().Trim();
                            listItem["RNC"] = Vendors.Rows[0]["RNC"].ToString().Trim();
                            listItem["Direccion"] = Vendors.Rows[0]["ADDRESS1"].ToString().Trim() + "," + Vendors.Rows[0]["ADDRESS2"].ToString().Trim() + "," + Vendors.Rows[0]["ADDRESS3"].ToString().Trim();
                            listItem["Pais"] = Vendors.Rows[0]["COUNTRY"].ToString().Trim();
                            listItem["Ciudad"] = Vendors.Rows[0]["CITY"].ToString().Trim();
                            listItem["Telefono1"] = Vendors.Rows[0]["PHONE1"].ToString().Trim();
                            listItem["Telefono2"] = Vendors.Rows[0]["PHONE2"].ToString().Trim();
                            listItem["Fax"] = Vendors.Rows[0]["FAX"].ToString().Trim();
                            listItem["Contacto"] = Vendors.Rows[0]["CONTACT"].ToString().Trim();
                            listItem["Clasificacion"] = Vendors.Rows[0]["CLASIF"].ToString().Trim();
                            listItem["Criterio"] = Vendors.Rows[0]["COMMENT"].ToString().Trim();
                            listItem["Proveedor"] = Vendors.Rows[0]["COMPANY"].ToString().Trim();
                            listItem["Fecha"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                            listItem["DB"] = Helpers.InterCompanyId;
                            listItem.Update();

                            if (Attachments != null)
                            {
                                if (Attachments.Rows.Count > 0)
                                {
                                    foreach (DataRow item in Attachments.Rows)
                                    {
                                        Byte[] FileInfo;
                                        FileInfo = Encoding.UTF8.GetBytes(String.Empty);
                                        FileInfo = (Byte[])item[1];
                                        attachInfo = new AttachmentCreationInformation();
                                        attachInfo.FileName = item[0].ToString().Trim()
                                            .Replace("^", " ").Replace("@", " ").Replace("#", " ")
                                            .Replace("$", " ").Replace("&", " ").Replace("*", " ");
                                        attachInfo.ContentStream = new MemoryStream(FileInfo);

                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }
                            }

                            clientContext.ExecuteQuery();
                        }
                    }
                    break;

                #endregion
                case 12:
                    #region Solicitud de Pago

                    DataTable Pagos = ConnectionDb.GetDt(String.Format("LODYNDEV.dbo.LPPOP10300R1 '{0}','{1}'", Helpers.InterCompanyId, idSolicitud));
                    Attachments = ConnectionDb.GetDt(String.Format("LODYNDEV.dbo.LPCO00100S1 '{0}','{1}'", Helpers.InterCompanyId, idSolicitud));

                    string PurchaseOrder = "";
                    if (Pagos != null)
                    {
                        if (Pagos.Rows.Count > 0)
                        {
                            clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
                            foreach (char c in "Servicios2.4")
                                securePassword.AppendChar(c);
                            clientContext.Credentials = new SharePointOnlineCredentials(email, securePassword);
                            listCollection = clientContext.Web.Lists.GetByTitle("Pago");
                            listInformation = new ListItemCreationInformation();

                            using (clientContext)
                            {
                                clientContext.Load(clientContext.Web);
                                clientContext.Load(listCollection);
                                var builder = new StringBuilder();
                                builder.Append("<View><Query>");
                                builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                                builder.Append("<Value Type='Text'>" + Pagos.Rows[0]["PMNTNMBR"].ToString().Trim() + "</Value></Eq></Where>");
                                builder.Append("</Query><RowLimit>1</RowLimit></View>");

                                var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                                var collection = listCollection.GetItems(query);
                                clientContext.Load(collection);
                                clientContext.ExecuteQuery();

                                if (collection.Count > 0)
                                {
                                    foreach (var item in collection)
                                    {
                                        listItem = listCollection.GetItemById(item["ID"].ToString().Trim());
                                        listItem.DeleteObject();
                                        clientContext.ExecuteQuery();
                                    }
                                }
                            }

                            foreach (DataRow item in Pagos.Rows)
                            {
                                if (item["PONUMBER"].ToString().Length > 0)
                                {
                                    if (PurchaseOrder.Length > 0)
                                    {
                                        if (PurchaseOrder != item["PONUMBER"].ToString().Trim())
                                        {
                                            PurchaseOrder += ", " + item["PONUMBER"].ToString().Trim();
                                        }
                                    }
                                    else
                                    {
                                        PurchaseOrder = item["PONUMBER"].ToString().Trim();
                                    }
                                }
                            }

                            listItem = listCollection.AddItem(listInformation);
                            listItem["Solicitud"] = Pagos.Rows[0]["PMNTNMBR"].ToString().Trim();
                            listItem["Departamento"] = Pagos.Rows[0]["DEPARTAMENTO"].ToString().Trim();
                            listItem["Fecha"] = Pagos.Rows[0]["DOCDATE"].ToString().Trim();
                            listItem["Monto"] = Pagos.Rows[0]["CHEKTOTL"].ToString().Trim();
                            listItem["Nota"] = Pagos.Rows[0]["NOTA"].ToString().Trim();
                            listItem["Moneda"] = Pagos.Rows[0]["CURNCYID"].ToString().Trim();
                            listItem["Suplidor"] = Pagos.Rows[0]["VENDNAME"].ToString().Trim();
                            listItem["OrdenOC"] = PurchaseOrder;
                            listItem["DB"] = Helpers.InterCompanyId;
                            listItem.Update();

                            if (Attachments != null)
                            {
                                if (Attachments.Rows.Count > 0)
                                {
                                    foreach (DataRow item in Attachments.Rows)
                                    {
                                        Byte[] FileInfo;
                                        FileInfo = Encoding.UTF8.GetBytes(String.Empty);
                                        FileInfo = (Byte[])item[1];
                                        attachInfo = new AttachmentCreationInformation();
                                        attachInfo.FileName = item[0].ToString().Trim()
                                            .Replace("^", " ").Replace("@", " ").Replace("#", " ")
                                            .Replace("$", " ").Replace("&", " ").Replace("*", " ");
                                        attachInfo.ContentStream = new MemoryStream(FileInfo);

                                        attach = listItem.AttachmentFiles.Add(attachInfo);
                                        clientContext.Load(attach);
                                    }
                                }
                            }

                            clientContext.ExecuteQuery();
                        }
                    }
                    break;

                    #endregion
            }
        }

        public static List<ApprovalHistory> GetListSharepoint(int modulo, string id, ref string status, ref string pendingApprover, ref string documentDate)
        {
            var clientContext = new ClientContext("https://seaboardpowercomdo.sharepoint.com");
            var securePassword = new SecureString();
            var builder = new StringBuilder();
            foreach (char c in "Servicios2.4")
                securePassword.AppendChar(c);
            clientContext.Credentials = new SharePointOnlineCredentials("no-reply@seaboardpower.com.do", securePassword);
            var listCollection = clientContext.Web.Lists.GetByTitle("Historial Aprobaciones");
            var approvalHistory = new List<ApprovalHistory>();
            using (clientContext)
            {
                clientContext.Load(clientContext.Web);
                clientContext.Load(listCollection);
                builder.Append("<View><Query>");
                builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                builder.Append("<Value Type='Text'>" + id + "</Value></Eq></Where>");
                builder.Append("</Query><RowLimit>100</RowLimit></View>");
                var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                var collection = listCollection.GetItems(query);
                clientContext.Load(collection);
                clientContext.ExecuteQuery();

                if (collection.Count > 0)
                {
                    foreach (var item in collection)
                    {
                        var approver = clientContext.Web.SiteUsers.GetById(((FieldUserValue)item["Aprobador"]).LookupId);
                        var requester = clientContext.Web.SiteUsers.GetById(((FieldUserValue)item["Solicitante"]).LookupId);
                        clientContext.Load(approver);
                        clientContext.Load(approver, u => u.LoginName);
                        clientContext.Load(requester);
                        clientContext.Load(requester, u => u.LoginName);
                        clientContext.ExecuteQuery();
                        approvalHistory.Add(new ApprovalHistory
                        {
                            Module = item["Modulo"].ToString().Trim(),
                            Request = item["Solicitud"].ToString().Trim(),
                            DateApproved = item["Fecha"].ToString().Trim(),
                            Status = item["Aprobacion"].ToString().Trim(),
                            Approver = approver.Title,
                            Requester = requester.Title
                        });
                    }
                }
            }

            builder = new StringBuilder();
            builder.Append("<View><Query>");
            switch (modulo)
            {
                case 1:
                    listCollection = clientContext.Web.Lists.GetByTitle("Almacen");
                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                    break;
                case 2:
                    listCollection = clientContext.Web.Lists.GetByTitle("Articulo");
                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                    break;
                case 3:
                    listCollection = clientContext.Web.Lists.GetByTitle("Servicio");
                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                    break;
                case 4:
                    listCollection = clientContext.Web.Lists.GetByTitle("Caja Chica");
                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                    break;
                case 5:
                    listCollection = clientContext.Web.Lists.GetByTitle("Compra");
                    builder.Append("<Where><Eq><FieldRef Name='OrdenNum'/>");
                    break;
                case 6:
                    listCollection = clientContext.Web.Lists.GetByTitle("Analisis");
                    builder.Append("<Where><Eq><FieldRef Name='Analisis'/>");
                    break;
                case 7:
                    listCollection = clientContext.Web.Lists.GetByTitle("Pago");
                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                    break;
                case 8:
                    listCollection = clientContext.Web.Lists.GetByTitle("Proveedor");
                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                    break;
                case 9:
                    listCollection = clientContext.Web.Lists.GetByTitle("Ausencia");
                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                    break;
                case 10:
                    listCollection = clientContext.Web.Lists.GetByTitle("Entrenamiento");
                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                    break;
                case 11:
                    listCollection = clientContext.Web.Lists.GetByTitle("Overtime");
                    builder.Append("<Where><Eq><FieldRef Name='Lote'/>");
                    break;
                case 12:
                    listCollection = clientContext.Web.Lists.GetByTitle("Usuario");
                    builder.Append("<Where><Eq><FieldRef Name='Solicitud'/>");
                    break;
            }
            
            using (clientContext)
            {
                clientContext.Load(clientContext.Web);
                clientContext.Load(listCollection);
                builder.Append("<Value Type='Text'>" + id + "</Value></Eq></Where>");
                builder.Append("</Query><RowLimit>1</RowLimit></View>");
                var query = new CamlQuery { ViewXml = builder.ToString().Trim() };
                var collection = listCollection.GetItems(query);
                clientContext.Load(collection);
                clientContext.ExecuteQuery();

                if (collection.Count > 0)
                {
                    foreach (var item in collection)
                    {
                        status = item["Estado"].ToString().Trim();
                        documentDate = item["Fecha"].ToString();
                        if (status == "Pendiente")
                            pendingApprover = item["AprobadorPendiente"].ToString();
                    }
                }
            }

            return approvalHistory;
        }
    }
}