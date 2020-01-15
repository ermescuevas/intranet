using Seaboard.Intranet.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain
{
    public class Helpers
    {
        public static string InterCompanyId { get; set; }
        public static string ReportPath { get; set; }
        public static int CompanyId { get; set; }
        public static int CompanyIdWebServices { get; set; }
        public static string ConnectionStrings { get; set; }
        public static string PublicDocumentsPath { get; set; }
        public static string MailPort { get; set; }
        public static string MailServer { get; set; }
        public static string HelpdeskMail { get; set; }
        public static string VendorClass { get; set; }
        public static string DateOfWeek(DateTime date)
        {
            string day = "";
            string month = "";

            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    day = "Lunes";
                    break;
                case DayOfWeek.Tuesday:
                    day = "Martes";
                    break;
                case DayOfWeek.Wednesday:
                    day = "Miercoles";
                    break;
                case DayOfWeek.Thursday:
                    day = "Jueves";
                    break;
                case DayOfWeek.Friday:
                    day = "Viernes";
                    break;
                case DayOfWeek.Saturday:
                    day = "Sabado";
                    break;
                case DayOfWeek.Sunday:
                    day = "Domingo";
                    break;
            }

            switch (date.Month)
            {
                case 1:
                    month = "Enero";
                    break;
                case 2:
                    month = "Febrero";
                    break;
                case 3:
                    month = "Marzo";
                    break;
                case 4:
                    month = "Abril";
                    break;
                case 5:
                    month = "Mayo";
                    break;
                case 6:
                    month = "Junio";
                    break;
                case 7:
                    month = "Julio";
                    break;
                case 8:
                    month = "Agosto";
                    break;
                case 9:
                    month = "Septiembre";
                    break;
                case 10:
                    month = "Octubre";
                    break;
                case 11:
                    month = "Noviembre";
                    break;
                case 12:
                    month = "Diciembre";
                    break;
            }


            return day + " " + date.Day + " de " + month + " del " + date.Year;
        }

        public static string DateParse(DateTime value)
        {
            try
            {
                switch (value.Month)
                {
                    case 1:
                        return "ENE";
                    case 2:
                        return "FEB";
                    case 3:
                        return "MAR";
                    case 4:
                        return "ABRIL";
                    case 5:
                        return "MAY";
                    case 6:
                        return "JUN";
                    case 7:
                        return "JUL";
                    case 8:
                        return "AGO";
                    case 9:
                        return "SEP";
                    case 10:
                        return "OCT";
                    case 11:
                        return "NOV";
                    case 12:
                        return "DIC";
                    default:
                        return "ENE";
                }
            }
            catch { return ""; }
        }

        public static string DateParseDescription(DateTime value)
        {
            try
            {
                switch (value.Month)
                {
                    case 1:
                        return "enero";
                    case 2:
                        return "febrero";
                    case 3:
                        return "marzo";
                    case 4:
                        return "abril";
                    case 5:
                        return "mayo";
                    case 6:
                        return "junio";
                    case 7:
                        return "julio";
                    case 8:
                        return "agosto";
                    case 9:
                        return "septiembre";
                    case 10:
                        return "octubre";
                    case 11:
                        return "noviembre";
                    case 12:
                        return "diciembre";
                    default:
                        return "enero";
                }
            }
            catch { return ""; }
        }
    }
}
