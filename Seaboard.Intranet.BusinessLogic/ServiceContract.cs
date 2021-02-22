using System;
using System.Collections.Generic;
using System.Net;
using RestSharp;
using Newtonsoft.Json;
using Seaboard.Intranet.BusinessLogic.GPServiceClient;
using Seaboard.Intranet.BusinessLogic.DGIIServiceClient;
using Seaboard.Intranet.Domain;
using Seaboard.Intranet.Domain.Models;
using CashReceiptType = Seaboard.Intranet.BusinessLogic.GPServiceClient.CashReceiptType;
using System.Linq;
using Seaboard.Intranet.Data.Repository;
using System.Net.Http;

namespace Seaboard.Intranet.BusinessLogic
{
    public class ServiceContract
    {
        #region Contract Methods

        public bool CreatePayablesInvoice(GpPayablesDocument invoice)
        {
            try
            {
                var payableTax = new List<PayablesTax>();
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
                var invoiceCurrency = new CurrencyKey();
                var payablesInvoice = new PayablesInvoice
                {
                    Key = new PayablesDocumentKey { Id = invoice.VoucherNumber },
                    VendorKey = new VendorKey { Id = invoice.VendorId },
                    PurchasesAmount = new MoneyAmount { Value = invoice.PurchaseAmount },
                    TradeDiscountAmount = new MoneyAmount { Value = invoice.TradeDiscountAmount },
                    MiscellaneousAmount = new MoneyAmount { Value = invoice.MiscellaneousAmount },
                    FreightAmount = new MoneyAmount { Value = invoice.FreightAmount },
                    Description = invoice.Description,
                    BatchKey = new BatchKey { Id = invoice.DocumentNumber },
                    VendorDocumentNumber = invoice.DocumentNumber
                };
                if (invoice.TaxAmount > 0)
                {
                    var payablesTax = new PayablesTax
                    {
                        Key = new PayablesTaxKey
                        {
                            PayablesDocumentKey = new PayablesDocumentKey { Id = invoice.VoucherNumber },
                            TaxDetailKey = new TaxDetailKey { Id = invoice.TaxDetail }
                        },
                        TaxAmount = new MoneyAmount { Value = invoice.TaxAmount }
                    };
                    payableTax.Add(payablesTax);
                    payablesInvoice.Taxes = payableTax.ToArray();
                }

                if (invoice.Currency != null)
                {
                    if (invoice.Currency != "")
                    {
                        if (invoice.Currency.Trim() == "RDPESO")
                            invoiceCurrency.ISOCode = "DOP";
                        else
                        {
                            invoiceCurrency.ISOCode = "USD";
                            payablesInvoice.ExchangeRate = 1;
                            payablesInvoice.ExchangeDate = new DateTime(invoice.DocumentDate.Year, invoice.DocumentDate.Month, invoice.DocumentDate.Day, 0, 0, 0);
                        }

                        payablesInvoice.CurrencyKey = invoiceCurrency;
                    }
                }

                payablesInvoice.Date = new DateTime(invoice.DocumentDate.Year, invoice.DocumentDate.Month, invoice.DocumentDate.Day, 0, 0, 0, 0);
                var payablesInvoiceCreatePolicy = wsDynamicsGp.GetPolicyByOperation("CreatePayablesInvoice", context);
                wsDynamicsGp.CreatePayablesInvoice(payablesInvoice, context, payablesInvoiceCreatePolicy);

                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool CreatePayablesCreditNote(GpPayablesDocument creditNote)
        {
            try
            {
                var payableTax = new List<PayablesTax>();
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
                var creditNoteCurrency = new CurrencyKey();
                var payablesCreditNote = new PayablesCreditMemo
                {
                    Key = new PayablesDocumentKey { Id = creditNote.VoucherNumber },
                    VendorKey = new VendorKey { Id = creditNote.VendorId },
                    PurchasesAmount = new MoneyAmount { Value = creditNote.PurchaseAmount },
                    TradeDiscountAmount = new MoneyAmount { Value = creditNote.TradeDiscountAmount },
                    MiscellaneousAmount = new MoneyAmount { Value = creditNote.MiscellaneousAmount },
                    FreightAmount = new MoneyAmount { Value = creditNote.FreightAmount },
                    Description = creditNote.Description,
                    BatchKey = new BatchKey { Id = creditNote.DocumentNumber },
                    VendorDocumentNumber = creditNote.DocumentNumber
                };

                if (creditNote.TaxAmount > 0)
                {
                    var payablesTax = new PayablesTax
                    {
                        Key = new PayablesTaxKey
                        {
                            PayablesDocumentKey = new PayablesDocumentKey { Id = creditNote.VoucherNumber },
                            TaxDetailKey = new TaxDetailKey { Id = creditNote.TaxDetail }
                        },
                        TaxAmount = new MoneyAmount { Value = creditNote.TaxAmount }
                    };
                    payableTax.Add(payablesTax);
                    payablesCreditNote.Taxes = payableTax.ToArray();
                }

                if (creditNote.Currency != null)
                {
                    if (creditNote.Currency != "")
                    {
                        if (creditNote.Currency.Trim() == "RDPESO")
                            creditNoteCurrency.ISOCode = "DOP";
                        else
                        {
                            creditNoteCurrency.ISOCode = "USD";
                            payablesCreditNote.ExchangeRate = 1;
                            payablesCreditNote.ExchangeDate = new DateTime(creditNote.DocumentDate.Year, creditNote.DocumentDate.Month, creditNote.DocumentDate.Day, 0, 0, 0, 0);
                        }

                        payablesCreditNote.CurrencyKey = creditNoteCurrency;
                    }
                }

                payablesCreditNote.Date = new DateTime(creditNote.DocumentDate.Year, creditNote.DocumentDate.Month, creditNote.DocumentDate.Day, 0, 0, 0, 0);
                var payablesCreditNoteCreatePolicy = wsDynamicsGp.GetPolicyByOperation("CreatePayablesCreditMemo", context);
                wsDynamicsGp.CreatePayablesCreditMemo(payablesCreditNote, context, payablesCreditNoteCreatePolicy);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void CreateInvoice(List<GPInvoice> invoices, GenericRepository repository, string userId, ref List<string[]> invoiceNumbers, ref string message)
        {
            if (invoices != null && invoices.Count() > 0)
            {
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context() { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
                var salesInvoiceCreatePolicy = wsDynamicsGp.GetPolicyByOperation("CreateSalesInvoice", context);
                var salesTax = new List<SalesDocumentTax>();
                SalesDocumentTax saleTax;
                foreach (var invoice in invoices)
                {
                    try
                    {
                        var ncf = repository.ExecuteScalarQuery<NcfEntity>($"INTRANET.dbo.GetNcfNumber '{Helpers.InterCompanyId}','{invoice.NcfType}'");
                        var accountReceivablesAccount = repository.ExecuteScalarQuery<int?>($"SELECT AccountIndex FROM {Helpers.InterCompanyId}.dbo.EFRM40601 " +
                        $"WHERE CustomerId = '{invoice.CustomerId}' AND AccountType = 1") ?? 0;
                        if (accountReceivablesAccount != 0)
                            repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.RM00101 SET RMARACC = '{accountReceivablesAccount}' WHERE CUSTNMBR = '{invoice.CustomerId}'");
                        var taxes = new List<GPTax>();
                        var salesInvoice = new SalesInvoice
                        {
                            Key = new SalesDocumentKey { Id = string.IsNullOrEmpty(invoice.SopNumber) ? "" : invoice.SopNumber },
                            DocumentTypeKey = new SalesDocumentTypeKey { Type = SalesDocumentType.Invoice, Id = invoice.NcfType == "B03" ? "ND" : "FACTURA" },
                            CustomerKey = new CustomerKey { Id = invoice.CustomerId },
                            Date = new DateTime(invoice.DocumentDate.Year, invoice.DocumentDate.Month, invoice.DocumentDate.Day, 0, 0, 0),
                            Terms = new SalesTerms { DueDate = new DateTime(invoice.DueDate.Year, invoice.DueDate.Month, invoice.DueDate.Day, 0, 0, 0) },
                            CurrencyKey = new CurrencyKey { ISOCode = invoice.CurrencyId == "RDPESO" ? "DOP" : "USD" },
                            BatchKey = new BatchKey { Id = invoice.BatchNumber },
                            ExchangeRate = invoice.CurrencyId != "RDPESO" ? invoice.ExchangeRate : 0,
                            ExchangeDate = invoice.CurrencyId != "RDPESO" ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0) : new DateTime(1900, 1, 1, 0, 0, 0),
                            UserDefined = new SalesUserDefined { Text02 = invoice.NcfType == "B03" ? invoice.ReferenceInvoice : "", Text03 = invoice.NcfType == "B03" ? invoice.ReferenceNcf : "", Text04 = ncf.DueDate.ToString("dd/MM/yyyy"), Text05 = ncf.Ncf },
                            Note = invoice.Note,
                            Lines = ProcessInvoiceLines(invoice, wsDynamicsGp, context, repository, ref taxes).ToArray(),
                        };

                        if (invoice.Lines.Sum(p => p.TaxAmount * p.Quantity) > 0)
                        {
                            foreach (var item in taxes.Select(x => x.TaxDetail).Distinct())
                            {
                                saleTax = new SalesDocumentTax
                                {
                                    Key = new SalesDocumentTaxKey
                                    {
                                        SalesDocumentKey = new SalesDocumentKey { Id = string.IsNullOrEmpty(invoice.SopNumber) ? "" : invoice.SopNumber },
                                        TaxDetailKey = new TaxDetailKey { Id = item },
                                        SequenceNumber = 0
                                    },
                                    TaxAmount = new MoneyAmount { Value = Math.Round(taxes.Where(i => i.TaxDetail == item).Sum(e => e.Amount), 2) },
                                    IsTaxableTax = true
                                };
                                salesTax.Add(saleTax);
                            }

                            if (taxes.Count > 0)
                            {
                                salesInvoice.TaxScheduleKey = new TaxScheduleKey { Id = taxes.FirstOrDefault()?.TaxDetail ?? "" };
                                salesInvoice.TaxAmount = new MoneyAmount { Value = Math.Round(taxes.Sum(e => e.Amount), 2) };
                                salesInvoice.Taxes = salesTax.ToArray();
                            }
                        }

                        salesInvoice.LineTotalAmount = new MoneyAmount { Value = Math.Round(salesInvoice.Lines.Sum(p => p.Quantity.Value * p.UnitPrice.Value), 2) };
                        salesInvoice.TotalAmount = new MoneyAmount { Value = Math.Round(salesInvoice.Lines.Sum(p => p.TotalAmount.Value), 2) };

                        wsDynamicsGp.CreateSalesInvoice(salesInvoice, context, salesInvoiceCreatePolicy);
                        repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20100 SET Posted = 1 " +
                            $"WHERE BatchNumber = '{invoice.BatchNumber}' AND SopNumber = '{invoice.DocumentNumber}'");
                        repository.ExecuteCommand($"INTRANET.dbo.UpdateNcfNumber '{Helpers.InterCompanyId}','{ncf.HeaderDocumentNumber}', '{ncf.DetailDocumentNumber}'");

                        var invoiceNumber = repository.ExecuteScalarQuery<string>($"SELECT TOP 1 RTRIM(SOPNUMBE) FROM {Helpers.InterCompanyId}.dbo.SOP10100 WHERE SOPTYPE = 3 ORDER BY DEX_ROW_ID DESC");
                        repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20100 SET SopNumber = '{invoiceNumber}' " +
                            $"WHERE BatchNumber = '{invoice.BatchNumber}' AND SopNumber = '{invoice.DocumentNumber}'");
                        repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.SOP10100 SET CNTCPRSN = '{invoice.ContactPerson}' WHERE SOPNUMBE = '{invoiceNumber}' AND SOPTYPE = 3");
                        var sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFRM30100 (NCFTypeId, NcfDetailId, NcfTypeDesc, DueDate, Ncf, SequenceNumber, " +
                            $"DocumentNumber, DocumentType, DocumentDate, Status, CustomerNumber, DocumentAmount, TaxAmount, CurrencyId, LastUserId, ApplyInvoice, ApplyNcf) " +
                            $"VALUES ('{ncf.HeaderDocumentNumber}','{ncf.DetailDocumentNumber}', '{ncf.NcfDescription}','{ncf.DueDate.ToString("yyyyMMdd")}', '{ncf.Ncf}', '{ncf.SecuenceNumber}'," +
                            $"'{invoiceNumber}', 3, '{invoice.DocumentDate.ToString("yyyyMMdd")}', 10, '{invoice.CustomerId}','{invoice.Lines.Sum(p => p.Price * p.Quantity)}'," +
                            $"{invoice.Lines.Sum(p => p.TaxAmount * p.Quantity)}, '{invoice.CurrencyId}', '{userId}', '{(string.IsNullOrEmpty(invoice.ReferenceInvoice) ? "" : invoice.ReferenceInvoice)}'," +
                            $"'{(string.IsNullOrEmpty(invoice.ReferenceNcf) ? "" : invoice.ReferenceNcf)}')";
                        repository.ExecuteCommand(sqlQuery);
                        if (!string.IsNullOrEmpty(invoice.ReferenceInvoice))
                        {
                            sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFRM30200 (SopNumber, SopDate, DocumentApply) " +
                            $"VALUES ('{invoice.ReferenceInvoice}','{invoice.DocumentDate.ToString("yyyyMMdd")}','{invoice.Lines.Sum(p => p.Total)}')";
                            repository.ExecuteCommand(sqlQuery);
                        }

                        if (!string.IsNullOrEmpty(invoiceNumber))
                            invoiceNumbers.Add(new string[] { invoiceNumber.Trim(), invoice.CustomerId, invoice.Lines.Sum(p => p.Price * p.Quantity).ToString("N2") });
                    }
                    catch (Exception ex)
                    {
                        message += "Cliente: " + invoice.CustomerId + Environment.NewLine + ex.Message;
                    }
                }
            }
        }
        public void CreateReturn(List<GPInvoice> invoices, GenericRepository repository, string userId, ref List<string[]> invoiceNumbers, ref string message)
        {
            if (invoices != null && invoices.Count() > 0)
            {
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context() { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
                var salesReturnCreatePolicy = wsDynamicsGp.GetPolicyByOperation("CreateSalesReturn", context);
                foreach (var invoice in invoices)
                {
                    try
                    {
                        var ncf = repository.ExecuteScalarQuery<NcfEntity>($"INTRANET.dbo.GetNcfNumber '{Helpers.InterCompanyId}','{invoice.NcfType}'");
                        var salesReturn = new SalesReturn
                        {
                            Key = new SalesDocumentKey { Id = string.IsNullOrEmpty(invoice.SopNumber) ? "" : invoice.SopNumber },
                            DocumentTypeKey = new SalesDocumentTypeKey { Type = SalesDocumentType.Return },
                            CustomerKey = new CustomerKey { Id = invoice.CustomerId },
                            Date = new DateTime(invoice.DocumentDate.Year, invoice.DocumentDate.Month, invoice.DocumentDate.Day, 0, 0, 0),
                            CurrencyKey = new CurrencyKey { ISOCode = invoice.CurrencyId == "RDPESO" ? "DOP" : "USD" },
                            BatchKey = new BatchKey { Id = invoice.BatchNumber },
                            ExchangeRate = invoice.CurrencyId != "RDPESO" ? invoice.ExchangeRate : 0,
                            ExchangeDate = invoice.CurrencyId != "RDPESO" ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0) : new DateTime(1900, 1, 1, 0, 0, 0),
                            UserDefined = new SalesUserDefined { Text02 = invoice.ReferenceInvoice, Text03 = invoice.ReferenceNcf, Text04 = ncf.DueDate.ToString("dd/MM/yyyy"), Text05 = ncf.Ncf },
                            Note = invoice.Note,
                            Lines = ProcessReturnLines(invoice, wsDynamicsGp, context).ToArray()
                        };

                        salesReturn.LineTotalAmount = new MoneyAmount { Value = Math.Round(salesReturn.Lines.Sum(p => p.Quantity.Value * p.UnitPrice.Value), 2) };
                        salesReturn.TaxAmount = new MoneyAmount { Value = Math.Round(salesReturn.Lines.Sum(p => p.TaxAmount.Value * p.Quantity.Value), 2) };
                        salesReturn.TotalAmount = new MoneyAmount { Value = Math.Round(salesReturn.Lines.Sum(p => p.TotalAmount.Value), 2) };
                        wsDynamicsGp.CreateSalesReturn(salesReturn, context, salesReturnCreatePolicy);
                        repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20100 SET Posted = 1 " +
                            $"WHERE BatchNumber = '{invoice.BatchNumber}' AND SopNumber = '{invoice.DocumentNumber}'");
                        repository.ExecuteCommand($"INTRANET.dbo.UpdateNcfNumber '{Helpers.InterCompanyId}','{ncf.HeaderDocumentNumber}', '{ncf.DetailDocumentNumber}'");

                        var invoiceNumber = repository.ExecuteScalarQuery<string>($"SELECT TOP 1 RTRIM(SOPNUMBE) FROM {Helpers.InterCompanyId}.dbo.SOP10100 WHERE SOPTYPE = 4 ORDER BY DEX_ROW_ID DESC");
                        repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.EFSOP20100 SET SopNumber = '{invoiceNumber}' " +
                            $"WHERE BatchNumber = '{invoice.BatchNumber}' AND SopNumber = '{invoice.DocumentNumber}'");
                        repository.ExecuteCommand($"UPDATE {Helpers.InterCompanyId}.dbo.SOP10100 SET CNTCPRSN = '{invoice.ContactPerson}' WHERE SOPNUMBE = '{invoiceNumber}' AND SOPTYPE = 4");
                        var sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFRM30100 (NCFTypeId, NcfDetailId, NcfTypeDesc, DueDate, Ncf, SequenceNumber, " +
                            $"DocumentNumber, DocumentType, DocumentDate, Status, CustomerNumber, DocumentAmount, TaxAmount, CurrencyId, LastUserId, ApplyInvoice, ApplyNcf) " +
                            $"VALUES ('{ncf.HeaderDocumentNumber}','{ncf.DetailDocumentNumber}', '{ncf.NcfDescription}','{ncf.DueDate.ToString("yyyyMMdd")}', '{ncf.Ncf}', '{ncf.SecuenceNumber}'," +
                            $"'{invoiceNumber}', 4, '{invoice.DocumentDate.ToString("yyyyMMdd")}', 10, '{invoice.CustomerId}','{invoice.Lines.Sum(p => p.Price * p.Quantity)}'," +
                            $"{invoice.Lines.Sum(p => p.TaxAmount * p.Quantity)}, '{invoice.CurrencyId}', '{userId}', '{(string.IsNullOrEmpty(invoice.ReferenceInvoice) ? "" : invoice.ReferenceInvoice)}'," +
                            $"'{(string.IsNullOrEmpty(invoice.ReferenceNcf) ? "" : invoice.ReferenceNcf)}')";
                        repository.ExecuteCommand(sqlQuery);
                        if (!string.IsNullOrEmpty(invoice.ReferenceInvoice))
                        {
                            sqlQuery = $"INSERT INTO {Helpers.InterCompanyId}.dbo.EFRM30200 (SopNumber, SopDate, DocumentApply) " +
                            $"VALUES ('{invoice.ReferenceInvoice}','{invoice.DocumentDate.ToString("yyyyMMdd")}','{invoice.Lines.Sum(p => p.Total)}')";
                            repository.ExecuteCommand(sqlQuery);
                        }

                        if (!string.IsNullOrEmpty(invoiceNumber))
                            invoiceNumbers.Add(new string[] { invoiceNumber.Trim(), invoice.CustomerId, invoice.Lines.Sum(p => p.Price * p.Quantity).ToString("N2") });
                    }
                    catch (Exception ex)
                    {
                        message += "Cliente: " + invoice.CustomerId + Environment.NewLine + ex.Message;
                    }
                }
            }
        }
        public void CreateReceivablesCreditNote(GpCreditNote creditNote)
        {
            var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
            //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
            var context = new Context() { OrganizationKey = new CompanyKey { Id = creditNote.CompanyId } };
            var creditNoteCurrency = new CurrencyKey();

            var receivablesCreditMemo = new ReceivablesCreditMemo
            {
                Key = new ReceivablesDocumentKey { Id = creditNote.Codigo },
                CustomerKey = new CustomerKey { Id = creditNote.Cliente },
                SalesAmount = new MoneyAmount { Value = creditNote.Monto },
                TradeDiscountAmount = new MoneyAmount { Value = creditNote.Descuento },
                BatchKey = new BatchKey { Id = creditNote.Lote },
                CustomerPONumber = creditNote.Ncf
            };

            if (creditNote.Moneda != null)
            {
                if (creditNote.Moneda != "")
                {
                    if (creditNote.Moneda.Trim() == "RD$" || creditNote.Moneda.Trim() == "RDPESO")
                        creditNoteCurrency.ISOCode = "DOP";
                    else
                    {
                        creditNoteCurrency.ISOCode = "USD";
                        receivablesCreditMemo.ExchangeRate = 1;
                        receivablesCreditMemo.ExchangeDate = new DateTime(creditNote.Fecha.Year, creditNote.Fecha.Month, creditNote.Fecha.Day, 0, 0, 0, 0);
                    }

                    receivablesCreditMemo.CurrencyKey = creditNoteCurrency;
                }
            }
            receivablesCreditMemo.Date = new DateTime(creditNote.Fecha.Year, creditNote.Fecha.Month, creditNote.Fecha.Day, 0, 0, 0, 0);

            var receivablesCreditMemoCreatePolicy = wsDynamicsGp.GetPolicyByOperation("CreateReceivablesCreditMemo", context);
            wsDynamicsGp.CreateReceivablesCreditMemo(receivablesCreditMemo, context, receivablesCreditMemoCreatePolicy);
        }
        public void CreateReceivablesDebitNote(GpCreditNote debitNote)
        {
            var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
            //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
            var context = new Context() { OrganizationKey = new CompanyKey { Id = debitNote.CompanyId } };
            var creditNoteCurrency = new CurrencyKey();

            var receivablesDebitMemo = new ReceivablesDebitMemo
            {
                Key = new ReceivablesDocumentKey { Id = debitNote.Codigo },
                CustomerKey = new CustomerKey { Id = debitNote.Cliente },
                SalesAmount = new MoneyAmount { Value = debitNote.Monto },
                TradeDiscountAmount = new MoneyAmount { Value = debitNote.Descuento },
                BatchKey = new BatchKey { Id = debitNote.Lote }
            };

            if (debitNote.Moneda != null)
            {
                if (debitNote.Moneda != "")
                {
                    if (debitNote.Moneda.Trim() == "RD$" || debitNote.Moneda.Trim() == "RDPESO")
                        creditNoteCurrency.ISOCode = "DOP";
                    else
                    {
                        creditNoteCurrency.ISOCode = "USD";
                        receivablesDebitMemo.ExchangeRate = 1;
                        receivablesDebitMemo.ExchangeDate = new DateTime(debitNote.Fecha.Year, debitNote.Fecha.Month, debitNote.Fecha.Day, 0, 0, 0, 0);
                    }

                    receivablesDebitMemo.CurrencyKey = creditNoteCurrency;
                }
            }
            receivablesDebitMemo.Date = new DateTime(debitNote.Fecha.Year, debitNote.Fecha.Month, debitNote.Fecha.Day, 0, 0, 0, 0);

            var receivablesDebitMemoCreatePolicy = wsDynamicsGp.GetPolicyByOperation("CreateReceivablesDebitMemo", context);
            wsDynamicsGp.CreateReceivablesDebitMemo(receivablesDebitMemo, context, receivablesDebitMemoCreatePolicy);
        }
        public void CreatePayablesCreditNote(GpCreditNote creditNote)
        {
            var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
            //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
            var context = new Context() { OrganizationKey = new CompanyKey { Id = creditNote.CompanyId } };
            var creditNoteCurrency = new CurrencyKey();
            var payablesCreditMemo = new PayablesCreditMemo
            {
                Key = new PayablesDocumentKey { Id = creditNote.Codigo },
                VendorKey = new VendorKey { Id = creditNote.Cliente },
                PurchasesAmount = new MoneyAmount { Value = creditNote.Monto },
                TradeDiscountAmount = new MoneyAmount { Value = creditNote.Descuento },
                BatchKey = new BatchKey { Id = creditNote.Lote },
                VendorDocumentNumber = creditNote.Ncf
            };
            if (creditNote.Moneda != null)
            {
                if (creditNote.Moneda != "")
                {
                    if (creditNote.Moneda.Trim() == "RD$" || creditNote.Moneda.Trim() == "RDPESO")
                    {
                        creditNoteCurrency.ISOCode = "DOP";
                    }
                    else
                    {
                        creditNoteCurrency.ISOCode = "USD";
                        payablesCreditMemo.ExchangeRate = 1;
                        payablesCreditMemo.ExchangeDate = new DateTime(creditNote.Fecha.Year, creditNote.Fecha.Month, creditNote.Fecha.Day, 0, 0, 0, 0);
                    }

                    payablesCreditMemo.CurrencyKey = creditNoteCurrency;
                }
            }

            payablesCreditMemo.Date = new DateTime(creditNote.Fecha.Year, creditNote.Fecha.Month, creditNote.Fecha.Day, 0, 0, 0, 0);
            var receivablesCreditMemoCreatePolicy = wsDynamicsGp.GetPolicyByOperation("CreatePayablesCreditMemo", context);
            wsDynamicsGp.CreatePayablesCreditMemo(payablesCreditMemo, context, receivablesCreditMemoCreatePolicy);
        }
        public void CreatePurchaseReceipt(List<GpPurchaseReceipt> receipts)
        {
            var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
            //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
            var context = new Context() { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
            var purchaseReceiptCreatePolicy = wsDynamicsGp.GetPolicyByOperation("CreatePurchaseReceipt", context);
            var createProductVendorPolicy = wsDynamicsGp.GetPolicyByOperation("CreateItemVendor", context);
            var createProductCurrencyPolicy = wsDynamicsGp.GetPolicyByOperation("CreateItemCurrency", context);

            foreach (var receipt in receipts)
            {
                var purchaseReceipt = new PurchaseReceipt
                {
                    Date = new DateTime(receipt.DocumentDate.Year, receipt.DocumentDate.Month, receipt.DocumentDate.Day, 0, 0, 0),
                    Key = new PurchaseTransactionKey { Id = receipt.DocumentNumber },
                    BatchKey = new BatchKey { Id = receipt.BatchNumber },
                    TransactionState = PurchaseTransactionState.Work,
                    VendorDocumentNumber = receipt.InvoiceId,
                    VendorKey = new VendorKey { Id = receipt.VendorId },
                    Lines = ProcessLinesReceipt(receipt, wsDynamicsGp, context, createProductVendorPolicy, createProductCurrencyPolicy).ToArray()
                };
                wsDynamicsGp.CreatePurchaseReceipt(purchaseReceipt, context, purchaseReceiptCreatePolicy);
            }
        }
        public bool CreateCashReceipt(GpCashReceipt receipt)
        {
            try
            {
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context() { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };

                var cashReceipt = new GPServiceClient.CashReceipt
                {
                    Date = new DateTime(receipt.DocumentDate.Year, receipt.DocumentDate.Month, receipt.DocumentDate.Day, 0, 0, 0),
                    Key = new ReceivablesDocumentKey { Id = receipt.DocumentNumber },
                    BatchKey = new BatchKey { Id = receipt.BatchNumber },
                    Amount = new MoneyAmount { Value = receipt.Amount },
                    CurrencyKey = new CurrencyKey(),
                    CustomerKey = new CustomerKey { Id = receipt.CustomerId },
                    Description = receipt.Description,
                    PostedDate = new DateTime(receipt.DocumentDate.Year, receipt.DocumentDate.Month, receipt.DocumentDate.Day, 0, 0, 0),
                    GeneralLedgerPostingDate = new DateTime(receipt.DocumentDate.Year, receipt.DocumentDate.Month, receipt.DocumentDate.Day, 0, 0, 0)
                };
                if (receipt.CurrencyId != null)
                {
                    if (receipt.CurrencyId != "")
                    {
                        if (receipt.CurrencyId.Trim() == "RDPESO")
                            cashReceipt.CurrencyKey.ISOCode = "DOP";
                        else
                        {
                            cashReceipt.CurrencyKey.ISOCode = "USD";
                            cashReceipt.ExchangeRate = 1;
                            cashReceipt.ExchangeDate = new DateTime(receipt.DocumentDate.Year, receipt.DocumentDate.Month, receipt.DocumentDate.Day, 0, 0, 0);
                        }
                    }
                }

                switch (receipt.Type)
                {
                    case Domain.Models.CashReceiptType.Transferencia:
                    case Domain.Models.CashReceiptType.Efectivo:
                        cashReceipt.Type = CashReceiptType.Cash;
                        break;
                    case Domain.Models.CashReceiptType.Cheque:
                        cashReceipt.Type = CashReceiptType.Check;
                        break;
                    case Domain.Models.CashReceiptType.Tarjeta:
                        cashReceipt.Type = CashReceiptType.PaymentCard;
                        break;
                    default:
                        cashReceipt.Type = CashReceiptType.PaymentCard;
                        break;
                }

                var cashReceiptCreatePolicy = wsDynamicsGp.GetPolicyByOperation("CreateCashReceipt", context);
                wsDynamicsGp.CreateCashReceipt(cashReceipt, context, cashReceiptCreatePolicy);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool VoidCashReceipt(string receiptId, ref string message)
        {
            try
            {
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context() { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
                var receiptKey = new ReceivablesDocumentKey { Id = receiptId };
                var policy = wsDynamicsGp.GetPolicyByOperation("VoidCashReceipt", context);
                wsDynamicsGp.VoidCashReceipt(receiptKey, context, policy);
                message = "OK";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
        public bool CreateVendor(GpVendor gpVendor)
        {
            try
            {
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context() { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
                var vendor = new Vendor
                {
                    Key = new VendorKey { Id = gpVendor.Rnc },
                    Name = gpVendor.Name,
                    CheckName = gpVendor.CheckName,
                    TaxRegistrationNumber = gpVendor.Rnc,
                    ClassKey = new VendorClassKey { Id = Helpers.VendorClass },
                    DefaultAddressKey = new VendorAddressKey { Id = "PRINCIPAL" },
                    HistoryOptions = new HistoryOptions
                    {
                        KeepCalendarHistory = true,
                        KeepDistributionHistory = true,
                        KeepFiscalHistory = true,
                        KeepTransactionHistory = true
                    }
                };
                var policy = wsDynamicsGp.GetPolicyByOperation("CreateVendor", context);
                wsDynamicsGp.CreateVendor(vendor, context, policy);

                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool CreateVendor(Domain.Models.Customer gpCustomer, ref string message)
        {
            try
            {
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context() { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
                var vendor = new Vendor
                {
                    Key = new VendorKey { Id = gpCustomer.CustomerId },
                    Name = gpCustomer.CustomerName,
                    CheckName = gpCustomer.CustomerName,
                    ShortName = gpCustomer.ShortName,
                    TaxRegistrationNumber = gpCustomer.RNC,
                    ClassKey = new VendorClassKey { Id = gpCustomer.ClassId },
                    DefaultAddressKey = new VendorAddressKey { Id = "PRINCIPAL" },
                    HistoryOptions = new HistoryOptions
                    {
                        KeepCalendarHistory = true,
                        KeepDistributionHistory = true,
                        KeepFiscalHistory = true,
                        KeepTransactionHistory = true
                    },
                    Addresses = new VendorAddress[] {new VendorAddress
                    {
                        Key = new VendorAddressKey{ Id = "PRINCIPAL", VendorKey = new VendorKey{ Id = gpCustomer.CustomerId} },
                        City = gpCustomer.City,
                        ContactPerson = gpCustomer.Contact,
                        Fax = new PhoneNumber { Value = gpCustomer.Fax },
                        Line1 = gpCustomer.Address1,
                        Line2 = gpCustomer.Address2,
                        Line3 = gpCustomer.Address3,
                        Phone1 = new PhoneNumber { Value = gpCustomer.Phone1 },
                        Phone2 = new PhoneNumber { Value = gpCustomer.Phone2 },
                        Phone3 = new PhoneNumber { Value = gpCustomer.Phone3 },
                        State = gpCustomer.State,
                        CountryRegion = gpCustomer.Country
                    }},
                    Comment1 = gpCustomer.NCFType
                };
                var policy = wsDynamicsGp.GetPolicyByOperation("CreateVendor", context);
                wsDynamicsGp.CreateVendor(vendor, context, policy);
                message = "OK";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
        public bool CreateCustomer(Domain.Models.Customer gpCustomer, ref string message)
        {
            try
            {
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context() { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
                var customer = new GPServiceClient.Customer
                {
                    Key = new CustomerKey { Id = gpCustomer.CustomerId },
                    Name = gpCustomer.CustomerName,
                    StatementName = gpCustomer.CustomerName,
                    Shortname = gpCustomer.ShortName,
                    TaxRegistrationNumber = gpCustomer.RNC,
                    ClassKey = new CustomerClassKey { Id = gpCustomer.ClassId },
                    DefaultAddressKey = new CustomerAddressKey { Id = "PRINCIPAL" },
                    HistoryOptions = new HistoryOptions
                    {
                        KeepCalendarHistory = true,
                        KeepDistributionHistory = true,
                        KeepFiscalHistory = true,
                        KeepTransactionHistory = true
                    },
                    Addresses = new CustomerAddress[] {new CustomerAddress
                    {
                        Key = new CustomerAddressKey{ Id = "PRINCIPAL", CustomerKey = new CustomerKey{ Id = gpCustomer.CustomerId } },
                        City = gpCustomer.City,
                        ContactPerson = gpCustomer.Contact,
                        Fax = new PhoneNumber { Value = gpCustomer.Fax },
                        Line1 = gpCustomer.Address1,
                        Line2 = gpCustomer.Address2,
                        Line3 = gpCustomer.Address3,
                        Phone1 = new PhoneNumber { Value = gpCustomer.Phone1 },
                        Phone2 = new PhoneNumber { Value = gpCustomer.Phone2 },
                        Phone3 = new PhoneNumber { Value = gpCustomer.Phone3 },
                        State = gpCustomer.State,
                        TaxScheduleKey = new TaxScheduleKey{ Id = "ITBISV18%" },
                        ShippingMethodKey = new ShippingMethodKey{ Id = "ENTREGA" },
                        CountryRegion = gpCustomer.Country
                    } },
                    Comment1 = gpCustomer.NCFType
                };
                var policy = wsDynamicsGp.GetPolicyByOperation("CreateCustomer", context);
                wsDynamicsGp.CreateCustomer(customer, context, policy);
                message = "OK";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
        public bool UpdateVendor(Domain.Models.Customer gpCustomer, ref string message)
        {
            try
            {
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context() { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
                var vendor = new Vendor
                {
                    Key = new VendorKey { Id = gpCustomer.CustomerId },
                    Name = gpCustomer.CustomerName,
                    CheckName = gpCustomer.CustomerName,
                    ShortName = gpCustomer.ShortName,
                    TaxRegistrationNumber = gpCustomer.RNC,
                    ClassKey = new VendorClassKey { Id = gpCustomer.ClassId },
                    DefaultAddressKey = new VendorAddressKey { Id = "PRINCIPAL" },
                    HistoryOptions = new HistoryOptions
                    {
                        KeepCalendarHistory = true,
                        KeepDistributionHistory = true,
                        KeepFiscalHistory = true,
                        KeepTransactionHistory = true
                    },
                    Addresses = new VendorAddress[] {new VendorAddress
                    {
                        Key = new VendorAddressKey{ Id = "PRINCIPAL", VendorKey = new VendorKey{ Id = gpCustomer.CustomerId} },
                        City = gpCustomer.City,
                        ContactPerson = gpCustomer.Contact,
                        Fax = new PhoneNumber { Value = gpCustomer.Fax },
                        Line1 = gpCustomer.Address1,
                        Line2 = gpCustomer.Address2,
                        Line3 = gpCustomer.Address3,
                        Phone1 = new PhoneNumber { Value = gpCustomer.Phone1 },
                        Phone2 = new PhoneNumber { Value = gpCustomer.Phone2 },
                        Phone3 = new PhoneNumber { Value = gpCustomer.Phone3 },
                        State = gpCustomer.State,
                        CountryRegion = gpCustomer.Country
                    }},
                    Comment1 = gpCustomer.NCFType
                };
                var policy = wsDynamicsGp.GetPolicyByOperation("UpdateVendor", context);
                wsDynamicsGp.UpdateVendor(vendor, context, policy);
                message = "OK";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
        public bool UpdateCustomer(Domain.Models.Customer gpCustomer, ref string message)
        {
            try
            {
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context() { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
                var customer = new GPServiceClient.Customer
                {
                    Key = new CustomerKey { Id = gpCustomer.CustomerId },
                    Name = gpCustomer.CustomerName,
                    StatementName = gpCustomer.CustomerName,
                    Shortname = gpCustomer.ShortName,
                    TaxRegistrationNumber = gpCustomer.RNC,
                    ClassKey = new CustomerClassKey { Id = gpCustomer.ClassId },
                    DefaultAddressKey = new CustomerAddressKey { Id = "PRINCIPAL" },
                    HistoryOptions = new HistoryOptions
                    {
                        KeepCalendarHistory = true,
                        KeepDistributionHistory = true,
                        KeepFiscalHistory = true,
                        KeepTransactionHistory = true
                    },
                    Addresses = new CustomerAddress[] {new CustomerAddress
                    {
                        Key = new CustomerAddressKey{ Id = "PRINCIPAL", CustomerKey = new CustomerKey{ Id = gpCustomer.CustomerId } },
                        City = gpCustomer.City,
                        ContactPerson = gpCustomer.Contact,
                        Fax = new PhoneNumber { Value = gpCustomer.Fax },
                        Line1 = gpCustomer.Address1,
                        Line2 = gpCustomer.Address2,
                        Line3 = gpCustomer.Address3,
                        Phone1 = new PhoneNumber { Value = gpCustomer.Phone1 },
                        Phone2 = new PhoneNumber { Value = gpCustomer.Phone2 },
                        Phone3 = new PhoneNumber { Value = gpCustomer.Phone3 },
                        State = gpCustomer.State,
                        CountryRegion = gpCustomer.Country,
                        TaxScheduleKey = new TaxScheduleKey{ Id = "ITBISV18%" },
                        ShippingMethodKey = new ShippingMethodKey{ Id = "ENTREGA" },
                    }},
                    Comment1 = gpCustomer.NCFType
                };
                var policy = wsDynamicsGp.GetPolicyByOperation("UpdateCustomer", context);
                wsDynamicsGp.UpdateCustomer(customer, context, policy);
                message = "OK";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
        public bool DeleteCustomer(string customerId, ref string message)
        {
            try
            {
                var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("wservices1", "@rioOzama0101") } } };
                //var wsDynamicsGp = new DynamicsGPClient { ClientCredentials = { Windows = { ClientCredential = new NetworkCredential("ermes", "netico24") } } };
                var context = new Context() { OrganizationKey = new CompanyKey { Id = Helpers.CompanyIdWebServices } };
                var customerKey = new CustomerKey { Id = customerId };
                var policy = wsDynamicsGp.GetPolicyByOperation("DeleteCustomer", context);
                wsDynamicsGp.DeleteCustomer(customerKey, context, policy);
                message = "OK";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
        public bool PostBatch(string batchSource, string batchNumber, ref string message)
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            var client = Helpers.InterCompanyId == "TEST1"
            ? new RestClient(
            $"https://tccsvr02.seaboardpower.com.do/gpservice/Tenants%28DefaultTenant%29/Companies%28COMPA%C3%91IA%20DE%20PRUEBA%29/PostingServices/System/Batches%28{batchSource};{batchNumber}%29")
            : new RestClient(
            $"https://tccsvr02.seaboardpower.com.do/gpservice/Tenants%28DefaultTenant%29/Companies(Transcontinental%20Capital%20Corportation%20(Bermuda)%20LTD)/PostingServices/System/Batches%28{batchSource};{batchNumber}%29");

            var request = new RestRequest(Method.POST)
            {
                //Credentials = new NetworkCredential("tcc-admin", EncryptionUtility.Decrypt("swcIP8J44vBMq++z++br13yBHSGeFeg1WUxsR5NhOsD9ihJ7C4GmKteD28yj9qaZZXaDZ/jrygRyXS9nSL0Nis6b0wQyCt2iVesFsAqokKcEAYqqW5uWvaGplK96W34R"), "seaboardpower.com.do")
            };
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Authorization", "Basic c2VhYm9hcmRwb3dlclx0Y2MtYWRtaW46QnV0dGVyLVNlYWItVENDKzQzMzA=");
            request.AddHeader("GP-Custom-Action", "POST");
            var response = client.Execute(request);
            message = response.ErrorMessage ?? "Error al conectar con Web Service";
            return response.IsSuccessful;
        }
        public Contribuyente GetContribuyente(PatronBusqueda patronBusqueda, string valorBusqueda, ref string status)
        {
            Contribuyente contribuyente = null;
            try
            {
                string value;
                WSMovilDGIISoapClient ws = new WSMovilDGIISoapClient();
                value = ws.GetContribuyentes(valorBusqueda, Convert.ToInt32(patronBusqueda), 1, 1, "");
                if (!string.IsNullOrEmpty(value))
                    contribuyente = JsonConvert.DeserializeObject<Contribuyente>(value);
            }
            catch (Exception ex) { status = ex.Message; }

            return contribuyente;
        }
        public List<MobileDevice> GetMobileDevices(string valorBusqueda)
        {
            List<MobileDevice> devices = null;
            try
            {
                HttpClient client = new HttpClient();
                var response = client.GetAsync($"https://fonoapi.freshpixl.com/v1/getlatest?token=c39ed8784c490fd8e0146f20082d0bc99fc9cf1ab30256a9&brand={valorBusqueda}", HttpCompletionOption.ResponseContentRead).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                    devices = JsonConvert.DeserializeObject<List<MobileDevice>>(response.Content.ReadAsStringAsync().Result);
            }
            catch { }

            return devices;
        }

        #endregion

        #region Private Methods

        private static List<PurchaseReceiptLine> ProcessLinesReceipt(GpPurchaseReceipt receipt, DynamicsGPClient wsDynamicsGp, Context context, Policy productVendorPolicy, Policy productCurrencyPolicy)
        {
            var receiptLines = new List<PurchaseReceiptLine>();

            try
            {
                foreach (var item in receipt.Lines)
                {
                    var purchaseReceiptLine = new PurchaseReceiptLine()
                    {
                        ItemKey = new ItemKey { Id = item.ItemId.Trim() },
                        UnitCost = new MoneyAmount { DecimalDigits = 2, Value = Math.Round(item.UnitPrice, 2) },
                        QuantityShipped = new Quantity { DecimalDigits = 2, Value = Math.Round(item.Quantity, 2) },
                        WarehouseKey = new WarehouseKey { Id = item.Warehouse },
                        UofM = item.UnitId
                    };
                    if (item.ItemDescription.Length > 0)
                        purchaseReceiptLine.ItemDescription = item.ItemDescription.Trim();
                    var serviceItem = (Service)wsDynamicsGp.GetItemByKey(new ItemKey { Id = item.ItemId.Trim() }, context);

                    CreateProductVendor(wsDynamicsGp, context, productVendorPolicy, serviceItem.Key.Id, serviceItem.Description, receipt.VendorId, serviceItem.PurchaseUofM.Trim());
                    CreateProductCurrency(wsDynamicsGp, context, productCurrencyPolicy, serviceItem.Key.Id, receipt.CurrencyId);

                    receiptLines.Add(purchaseReceiptLine);
                }
            }
            catch
            {
                return receiptLines;
            }

            return receiptLines;
        }
        private static List<SalesInvoiceLine> ProcessInvoiceLines(GPInvoice invoice, DynamicsGPClient wsDynamicsGp, Context context, GenericRepository repository, ref List<GPTax> taxes)
        {
            var invoiceLines = new List<SalesInvoiceLine>();

            try
            {
                foreach (var item in invoice.Lines)
                {
                    var salesItem = (Service)wsDynamicsGp.GetItemByKey(new ItemKey { Id = item.ItemNumber.Trim() }, context);
                    var invoiceLine = new SalesInvoiceLine()
                    {
                        ItemKey = new ItemKey { Id = item.ItemNumber.Trim() },
                        UnitPrice = new MoneyAmount { Value = Math.Round(item.Price, 2) },
                        Quantity = new Quantity { Value = Math.Round(item.Quantity, 5)},
                        Discount = new MoneyPercentChoice { Item = new Percent { Value = Math.Round(item.DiscountAmount, 2) } },
                        ItemDescription = item.ItemDescription.Length > 0 ? item.ItemDescription : null,
                        TotalAmount = new MoneyAmount { Value = Math.Round(item.Total, 2)}
                    };
                    if (item.TaxAmount > 0)
                    {
                        var detalle = new GPTax();
                        var taxDetailId = repository.ExecuteScalarQuery<string>($"SELECT B.TAXDTLID " +
                            $"FROM {Helpers.InterCompanyId}.dbo.TX00102 A INNER JOIN {Helpers.InterCompanyId}.dbo.TX00201 B ON A.TAXDTLID = B.TAXDTLID " +
                            $"WHERE TAXSCHID = '{salesItem.SalesTaxScheduleKey.Id}'");

                        detalle.TaxDetail = taxDetailId;
                        invoiceLine.TaxScheduleKey = salesItem.SalesTaxScheduleKey;
                        invoiceLine.ItemTaxScheduleKey = salesItem.SalesTaxScheduleKey;
                        invoiceLine.TaxBasis = salesItem.SalesTaxBasis;
                        detalle.Amount = Math.Round(item.TaxAmount * item.Quantity, 2);
                        taxes.Add(detalle);
                        invoiceLine.TaxAmount = new MoneyAmount { Value = Math.Round(item.TaxAmount * item.Quantity, 2) };
                    }
                    var costOfSalesAccount = repository.ExecuteScalarQuery<string>($"SELECT RTRIM(B.ACTNUMST) " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EFRM40601 A INNER JOIN {Helpers.InterCompanyId}.dbo.GL00105 B " +
                        $"ON A.AccountIndex = B.ACTINDX " +
                        $"WHERE CustomerId = '{invoice.CustomerId}' AND ItemCode= '{item.ItemNumber}' AND AccountType = 3");
                    var discountAccount = repository.ExecuteScalarQuery<string>($"SELECT RTRIM(B.ACTNUMST) " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EFRM40601 A INNER JOIN {Helpers.InterCompanyId}.dbo.GL00105 B " +
                        $"ON A.AccountIndex = B.ACTINDX " +
                        $"WHERE CustomerId = '{invoice.CustomerId}' AND ItemCode= '{item.ItemNumber}' AND AccountType = 4");
                    var salesAccount = repository.ExecuteScalarQuery<string>($"SELECT RTRIM(B.ACTNUMST) " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EFRM40601 A INNER JOIN {Helpers.InterCompanyId}.dbo.GL00105 B " +
                        $"ON A.AccountIndex = B.ACTINDX " +
                        $"WHERE CustomerId = '{invoice.CustomerId}' AND ItemCode= '{item.ItemNumber}' AND AccountType = 2");
                    var returnsAccount = repository.ExecuteScalarQuery<string>($"SELECT RTRIM(B.ACTNUMST) " +
                        $"FROM {Helpers.InterCompanyId}.dbo.EFRM40601 A INNER JOIN {Helpers.InterCompanyId}.dbo.GL00105 B " +
                        $"ON A.AccountIndex = B.ACTINDX " +
                        $"WHERE CustomerId = '{invoice.CustomerId}' AND ItemCode= '{item.ItemNumber}' AND AccountType = 5");

                    if (!string.IsNullOrEmpty(costOfSalesAccount))
                        invoiceLine.CostOfSalesGLAccountKey = new GLAccountNumberKey { Id = costOfSalesAccount.ToString() };
                    if (!string.IsNullOrEmpty(discountAccount))
                        invoiceLine.DiscountGLAccountKey = new GLAccountNumberKey { Id = discountAccount.ToString() };
                    if (!string.IsNullOrEmpty(salesAccount))
                        invoiceLine.SalesGLAccountKey = new GLAccountNumberKey { Id = salesAccount.ToString() };
                    if (!string.IsNullOrEmpty(returnsAccount))
                        invoiceLine.ReturnsGLAccountKey = new GLAccountNumberKey { Id = returnsAccount.ToString() };
                    invoiceLines.Add(invoiceLine);
                }
            }
            catch
            {
                return invoiceLines;
            }

            return invoiceLines;
        }
        private static List<SalesReturnLine> ProcessReturnLines(GPInvoice invoice, DynamicsGPClient wsDynamicsGp, Context context)
        {
            var invoiceLines = new List<SalesReturnLine>();

            try
            {
                foreach (var item in invoice.Lines)
                {
                    var salesItem = (Service)wsDynamicsGp.GetItemByKey(new ItemKey { Id = item.ItemNumber.Trim() }, context);
                    var returnLine = new SalesReturnLine()
                    {
                        ItemKey = new ItemKey { Id = item.ItemNumber.Trim() },
                        UnitPrice = new MoneyAmount { Value = Math.Round(item.Price, 2) },
                        Quantity = new Quantity { Value = Math.Round(item.Quantity, 5) },
                        Discount = new MoneyPercentChoice { Item = new Percent { Value = Math.Round(item.DiscountAmount, 2) } },
                        ItemDescription = item.ItemDescription.Length > 0 ? item.ItemDescription : null,
                        TotalAmount = new MoneyAmount { Value = Math.Round(item.Total, 2) },
                        TaxAmount = new MoneyAmount { Value = Math.Round(item.TaxAmount, 2) },
                    };
                    invoiceLines.Add(returnLine);
                }
            }
            catch
            {
                return invoiceLines;
            }

            return invoiceLines;
        }
        private static void CreateProductVendor(DynamicsGP wsDynamicsGp, Context context, Policy productVendorPolicy, string itemId, string description, string vendorId, string uofM)
        {
            ItemVendor itemVendor;
            ItemVendorKey itemVendorKey = new ItemVendorKey()
            {
                ItemKey = new ItemKey { Id = itemId },
                VendorKey = new VendorKey { Id = vendorId }
            };

            try
            {
                itemVendor = wsDynamicsGp.GetItemVendorByKey(itemVendorKey, context);
            }
            catch
            {
                itemVendor = null;
            }

            if (itemVendor != null) return;
            itemVendor = new ItemVendor
            {
                Key = itemVendorKey,
                VendorItemDescription = description,
                VendorItemNumber = itemId,
                PurchasingUofM = uofM.Trim().PadRight(9)
            };

            wsDynamicsGp.CreateItemVendor(itemVendor, context, productVendorPolicy);
        }
        private static void CreateProductCurrency(DynamicsGP wsDynamicsGp, Context context, Policy productCurrencyPolicy, string itemId, string currencyId)
        {
            ItemCurrencyKey itemCurrencyKey = new ItemCurrencyKey() { ItemKey = new ItemKey { Id = itemId } };
            ItemCurrency itemCurrency;

            var currencyKey = new CurrencyKey();
            switch (currencyId)
            {
                case "Z-US$":
                case "USDOLAR":
                    currencyKey.ISOCode = "USD";
                    break;
                case "EURO":
                case "Z-EURO":
                    currencyKey.ISOCode = "EUR";
                    break;
                default:
                    currencyKey.ISOCode = "DOP";
                    break;
            }
            itemCurrencyKey.CurrencyKey = currencyKey;

            try { itemCurrency = wsDynamicsGp.GetItemCurrencyByKey(itemCurrencyKey, context); }
            catch { itemCurrency = null; }

            if (itemCurrency != null) return;
            itemCurrency = new ItemCurrency { Key = itemCurrencyKey };
            wsDynamicsGp.CreateItemCurrency(itemCurrency, context, productCurrencyPolicy);
        }

        #endregion
    }

    internal class GPTax
    {
        public string TaxDetail { get; set; }
        public decimal Amount { get; set; }
    }
}