using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Core;

namespace CmfChileScraping
{
    class Program
    {
        public static HtmlDocument LoadSite(string Url)
        {
            WebClient client = new WebClient();
            string resource = Encoding.UTF8.GetString(client.DownloadData(Url));
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(resource);
            return html;
        }

        public static void SaveListadoFiscalizados(HtmlDocument page, string baseUrl, CmfChileEntities db)
        {            
            List<Core.ListadoFiscalizados> listadoFiscalizados = new List<Core.ListadoFiscalizados>();
            var trs = page.DocumentNode.SelectNodes("//table/tr");
            for (int i = 3; i <= trs.Count; i++)
            {
                var url = page.DocumentNode.SelectNodes("//table/tr[" + i + "]/td[" + 1 + "]/a").First().Attributes[0].Value;
                url = baseUrl + url;

                var rut = page.DocumentNode.SelectNodes("//table/tr[" + i + "]/td[" + 1 + "]").First().InnerText.Replace("\r", "").Replace("\t", "").TrimStart().TrimEnd();
                var entidad = page.DocumentNode.SelectNodes("//table/tr[" + i + "]/td[" + 2 + "]").First().InnerText.Replace("\r", "").Replace("\t", "").TrimStart().TrimEnd();
                var estado = page.DocumentNode.SelectNodes("//table/tr[" + i + "]/td[" + 3 + "]").First().InnerText.Replace("\r", "").Replace("\t", "").TrimStart().TrimEnd();

                Core.ListadoFiscalizados item = new Core.ListadoFiscalizados();
                item.UrlListadoEmisor = url;
                item.Rut = rut;
                item.Estado = estado;
                item.Entidad = entidad.Replace("  ", " ").Trim();
                item.DateScraping = DateTime.Now;
                listadoFiscalizados.Add(item);
                Console.WriteLine("Process item " + i + " From " + trs.Count);
            }

            db.ListadoFiscalizados.AddRange(listadoFiscalizados);
            db.SaveChanges();
        }

        public static void SaveUrlDirectores(string baseUrl, CmfChileEntities db)
        {
            List<Core.ListadoFiscalizados> listadoFiscalizados = db.ListadoFiscalizados.ToList();
            foreach (var item in listadoFiscalizados)
            {
                HtmlDocument page = LoadSite(item.UrlListadoEmisor);
                var url = page.DocumentNode.SelectNodes("//a[@title='Registro de Directores, Admin. y Liq.']").First().Attributes[0].Value.Replace("&amp;","&");
                url = baseUrl + url;
                item.UrlRegistroDirector = url;
                Console.WriteLine("Process item " + item.IdListadoFiscalizados);
            }
            Console.WriteLine("Guardando items procesados");
            db.SaveChanges();
        }

        public static void SaveRegistroDirectores(CmfChileEntities db)
        {
            List<Core.ListadoFiscalizados> listadoFiscalizados = db.ListadoFiscalizados.ToList();
            foreach (var item in listadoFiscalizados)
            {
                HtmlDocument page = LoadSite(item.UrlRegistroDirector);
                List<Core.ListadoDirectores> listadoDirectores = new List<Core.ListadoDirectores>();

                var trs = page.DocumentNode.SelectNodes("//table/tr");
                for (int i = 2; i <= trs.Count; i++)
                {
                    var rut = page.DocumentNode.SelectNodes("//table/tr[" + i + "]/td[" + 1 + "]").First().InnerText.Replace("\r", "").Replace("\t", "").TrimStart().TrimEnd();
                    if (rut != "Sin Informaci&oacute;n")
                    {
                        var nombre = page.DocumentNode.SelectNodes("//table/tr[" + i + "]/td[" + 2 + "]").First().InnerText.Replace("\r", "").Replace("\t", "").TrimStart().TrimEnd();
                        var cargo = page.DocumentNode.SelectNodes("//table/tr[" + i + "]/td[" + 3 + "]").First().InnerText.Replace("\r", "").Replace("\t", "").TrimStart().TrimEnd();
                        var fechaNombramiento = page.DocumentNode.SelectNodes("//table/tr[" + i + "]/td[" + 4 + "]").First().InnerText.Replace("\r", "").Replace("\t", "").TrimStart().TrimEnd();

                        Core.ListadoDirectores itemListado = new Core.ListadoDirectores();
                        itemListado.Rut = rut.Replace(".","").Replace("  "," ").Trim();
                        itemListado.IdListadoFiscalizados = item.IdListadoFiscalizados;
                        itemListado.Nombre = nombre.Replace(".", "").Replace("  ", " ").Trim();
                        itemListado.Cargo = cargo;
                        itemListado.FechaNombramiento = fechaNombramiento;
                        itemListado.DateScraping = DateTime.Now;
                        listadoDirectores.Add(itemListado);
                    }
                    Console.WriteLine("Process item directors " + i + " From " + trs.Count);
                }
                db.ListadoDirectores.AddRange(listadoDirectores);
                db.SaveChanges();
            }            
        }

        static void Main(string[] args)
        {
            CmfChileEntities db = new CmfChileEntities();
            db.ClearTables();
            string baseUrl1 = @"http://www.cmfchile.cl/institucional/mercados/";
            string baseUrl2 = @"http://www.cmfchile.cl";

            string url = @"http://www.cmfchile.cl/institucional/mercados/consulta.php?mercado=V&Estado=TO&entidad=RVEMI&_=1518466470090";

            HtmlDocument page = LoadSite(url);
            SaveListadoFiscalizados(page, baseUrl1, db);
            SaveUrlDirectores(baseUrl2, db);
            SaveRegistroDirectores(db);
            
        }
    }
}