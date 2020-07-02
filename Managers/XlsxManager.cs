using SerAPI.Models;
using SerAPI.Services;
using SerAPI.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SerAPI.Utils;

namespace SerAPI.Managers
{
    public class XlsxManager : GenericModelFactory<Xlsx>
    {
        private IConfiguration _config;
        private readonly XlsxHelpers _xlsxHelpers;
        private readonly Locales _locales;
        private readonly PostgresQLService _postgresQLService;

        public XlsxManager(ApplicationDbContext db,
            ILogger<XlsxManager> logger,
            IHttpContextAccessor contextAccessor,
            AuditManager cRepositoryLog,
            XlsxHelpers xlsxHelpers,
            Locales locales,
            PostgresQLService postgresQLService,
            IConfiguration config)
            : base(db, logger, contextAccessor, cRepositoryLog, config)
        {
            _config = config;
            _xlsxHelpers = xlsxHelpers;
            _locales = locales;
            _postgresQLService = postgresQLService;
        }

        /// <summary>
        /// Method for checking if JToken key exists and has value
        /// </summary>
        /// <param name="Value"></param>
        /// <returns>Result of the validation in a boolean value</returns>
        public static bool HasJValue(JToken Value)
        {
            return Value != null && !string.IsNullOrEmpty(Value.ToString());
        }

        public async Task<object> GetDataFromDB(string modelName, string columnStr, string orderBy,
            string parameters, bool download = false)
        {
            return await GetDataFromPsqlDB(_postgresQLService, modelName, columnStr: columnStr,
                orderBy: orderBy, download: download, parameters: parameters);
        }

        public async Task<JObject> GenerateXlsx(JArray Results, string ModelName)
        {
            MemoryStream stream = new MemoryStream();
            using (ExcelPackage package = new ExcelPackage(stream))
            {
                ExcelWorksheet Worksheet = package.Workbook.Worksheets.Add(_locales.__(ModelName));

                int Row = 1;
                int column = 1;

                //Descarga y redimensionado de logo
                /*WebClient wc = new WebClient();
                byte[] bytes_img = wc.DownloadData(_config["Assets:ImagesReport"]);
                MemoryStream StreamImage = new MemoryStream(bytes_img);
                Image Logo = Image.FromStream(StreamImage);
                ExcelPicture ExcelLogo = Worksheet.Drawings.AddPicture("Logo", Logo);
                ExcelLogo.SetSize(20);
                ExcelLogo.SetPosition(0, 1, 0, 0);

                Row++;*/

                if (Results.Count > 0)
                {
                    string[] keys = ((JObject)Results[0]).Properties().Select(p => p.Name).ToArray();

                    foreach (var key in keys)
                    {
                        using (ExcelRange Cells = Worksheet.Cells[Row, column])
                        {
                            Cells.Value = _locales.__(key).ToUpper();
                            _xlsxHelpers.MakeTitle(Cells);
                        }

                        column++;
                    }
                }

                var numberformat = "#,##0";
                Row++;

                foreach (JObject parsedObject in Results.Children<JObject>())
                {
                    column = 1;
                    foreach (JProperty pair in parsedObject.Properties())
                    {
                        string propertyName = pair.Name;

                        using (ExcelRange Cells = Worksheet.Cells[Row, column])
                        {

                            if (pair.Value.Type == JTokenType.None || pair.Value.Type == JTokenType.Null)
                            {
                                Cells.Value = string.Empty;
                            }
                            else if (pair.Value.Type == JTokenType.String)
                            {
                                Cells.Value = (string)pair.Value;
                            }
                            else if (pair.Value.Type == JTokenType.Integer)
                            {
                                numberformat = "#";
                                Cells.Style.Numberformat.Format = numberformat;
                                Cells.Value = (int)pair.Value;
                            }
                            else if (pair.Value.Type == JTokenType.Boolean)
                            {
                                Cells.Style.Fill.PatternType = ExcelFillStyle.Solid;

                                if ((bool)pair.Value)
                                {
                                    Cells.Value = _locales.__("active");
                                    Cells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(135, 236, 109));
                                    Cells.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(33, 119, 27));
                                    _xlsxHelpers.BorderStyle(Cells, System.Drawing.Color.FromArgb(87, 175, 81));
                                }
                                else
                                {
                                    Cells.Value = _locales.__("inactive");
                                    Cells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 165, 165));
                                    Cells.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(169, 34, 34));
                                    _xlsxHelpers.BorderStyle(Cells, System.Drawing.Color.FromArgb(253, 78, 78));
                                }
                            }
                            else if (pair.Value.Type == JTokenType.Float)
                            {
                                numberformat = "#,###0";
                                Cells.Style.Numberformat.Format = numberformat;
                                Cells.Value = (float)pair.Value;
                            }
                            else if (pair.Value.Type == JTokenType.Date)
                            {
                                Cells.Style.Numberformat.Format = "yyyy-mm-dd HH:MM:ss";
                                Cells.Value = (DateTime)pair.Value;
                            }
                            else if (pair.Value.Type == JTokenType.Array)
                            {
                                Cells.Value = ((JArray)pair.Value).ToString(Newtonsoft.Json.Formatting.None);
                            }
                            else
                            {
                                Cells.Value = pair.Value;
                            }
                        }

                        column++;
                    }

                    Row++;
                }

                /*
                foreach (JToken Item in Results)
                {
                    var RowInitial = Row;

                    #region OrderHeader
                    using (ExcelRange Cells = Worksheet.Cells[RowInitial, 1, Row - 1, 1])
                    {
                        _xlsxHelpers.SetValue(Cells, Item["CodeLocalOrder"].ToString());
                    }

                    using (ExcelRange Cells = Worksheet.Cells[RowInitial, 2, Row - 1, 2])
                    {
                        _xlsxHelpers.SetValue(Cells, Item["Name"].ToString());
                    }

                    using (ExcelRange Cells = Worksheet.Cells[RowInitial, 3, Row - 1, 3])
                    {
                        Cells.Merge = true;
                        Cells.Value = DateTime.Parse(Item["CreatedDate"].ToString());
                        Cells.Style.Numberformat.Format = "dd MMM yyyy";
                    }

                    using (ExcelRange Cells = Worksheet.Cells[RowInitial, 5, Row - 1, 5])
                    {
                        _xlsxHelpers.SetValue(Cells, Item[(Lang == "es" ? "DescOrderType" : "DescOrderType_" + Lang)].ToString());
                    }

                    using (ExcelRange Cells = Worksheet.Cells[RowInitial, 6, Row - 1, 6])
                    {
                        Cells.Merge = true;
                        Cells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        Cells.Style.Font.Bold = true;
                        Cells.Value = _locales.__("order_state_" + Item["OrderState"].ToString()).ToUpper();

                        switch (Item["OrderState"].ToString())
                        {
                            case "0":
                                Cells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(251, 188, 72));
                                Cells.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(195, 83, 0));
                                _xlsxHelpers.BorderStyle(Cells, System.Drawing.Color.FromArgb(255, 109, 0));
                                break;

                            case "1":
                            case "2":
                            case "3":
                                Cells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(132, 208, 253));
                                Cells.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(24, 86, 152));
                                _xlsxHelpers.BorderStyle(Cells, System.Drawing.Color.FromArgb(54, 148, 247));
                                break;

                            case "4":
                            case "5":
                                Cells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(135, 236, 109));
                                Cells.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(33, 119, 27));
                                _xlsxHelpers.BorderStyle(Cells, System.Drawing.Color.FromArgb(87, 175, 81));
                                break;

                            case "6":
                            case "10":
                                Cells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 165, 165));
                                Cells.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(169, 34, 34));
                                _xlsxHelpers.BorderStyle(Cells, System.Drawing.Color.FromArgb(253, 78, 78));
                                break;

                            default:
                                break;
                        }
                    }
                    #endregion

                    Worksheet.Cells[Row - 1, 1, Row - 1, 46].Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    Worksheet.Cells[Row - 1, 1, Row - 1, 46].Style.Border.Bottom.Color.SetColor(System.Drawing.Color.Black);
                    Worksheet.Cells[1, 46, Row - 1, 46].Style.Border.Right.Style = ExcelBorderStyle.Medium;
                    Worksheet.Cells[1, 46, Row - 1, 46].Style.Border.Right.Color.SetColor(System.Drawing.Color.Black);

                }
                */

                Worksheet.Cells[Worksheet.Dimension.Address].AutoFitColumns();
                Worksheet.Cells[Worksheet.Dimension.Address].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                Worksheet.Cells[Worksheet.Dimension.Address].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                Worksheet.Cells[Worksheet.Dimension.Address].Style.Font.Name = "Arial";
                Worksheet.Cells[Worksheet.Dimension.Address].Style.Font.Size = 10;

                Worksheet.Row(1).Height = 44;

                // Printer Settings
                Worksheet.PrinterSettings.RepeatRows = new ExcelAddress("1:2");
                Worksheet.PrinterSettings.BlackAndWhite = false;
                Worksheet.PrinterSettings.PaperSize = ePaperSize.A4;
                Worksheet.PrinterSettings.Orientation = eOrientation.Landscape;
                Worksheet.PrinterSettings.TopMargin = 0.333333M;
                Worksheet.PrinterSettings.RightMargin = 0.333333M;
                Worksheet.PrinterSettings.BottomMargin = 0.44M;
                Worksheet.PrinterSettings.LeftMargin = 0.333333M;
                Worksheet.PrinterSettings.FitToPage = true;
                Worksheet.PrinterSettings.FitToWidth = 1;
                Worksheet.PrinterSettings.FitToHeight = 0;
                Worksheet.PrinterSettings.PrintArea = Worksheet.Cells[1, 1, Row, 46];

                package.Save();

            }

            var res = await UploadS3File(stream, $"/downloads/{Guid.NewGuid()}/", name: $"{_locales.__(ModelName)}", ext: ".xlsx");

            return new JObject()
            {
                {"url", $"https://s3.amazonaws.com/{_config["AWS:S3:Bucket"]}/{res.Key}" }
            };
        }
    }
}