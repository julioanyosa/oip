using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using OpenInvoicePeru.Comun.Dto.Modelos;
using OpenInvoicePeru.Comun.Dto.Intercambio;
using System.Net.Http;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Data;
using CrystalDecisions.CrystalReports.Engine;
using Microsoft.Practices.Unity;
using System.Xml;

namespace WCF_FE
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    [ServiceBehavior(AddressFilterMode = AddressFilterMode.Any)]
    public class FEService : IFEService
    {

        public FEService()
        {

        }

        public async Task<DocumentoResponse> GenerarXMLFactura(DocumentoElectronico _documento)
        {


            var proxy = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["UrlOpenInvoicePeruApi"]) };

            string metodoApi;
            switch (_documento.TipoDocumento)
            {
                case "07":
                    metodoApi = "api/GenerarNotaCredito";
                    break;
                case "08":
                    metodoApi = "api/GenerarNotaDebito";
                    break;
                default:
                    metodoApi = "api/GenerarFactura";
                    break;
            }

            var response = await proxy.PostAsJsonAsync(metodoApi, _documento);
            var respuesta = await response.Content.ReadAsAsync<DocumentoResponse>();
            if (!respuesta.Exito)
                throw new ApplicationException(respuesta.MensajeError);

            if (!Directory.Exists(_documento.RutaXML))
                Directory.CreateDirectory(_documento.RutaXML);

            string RutaArchivo = Path.Combine(_documento.RutaXML,
                $"{_documento.IdDocumento}.xml");

            File.WriteAllBytes(RutaArchivo, Convert.FromBase64String(respuesta.TramaXmlSinFirma));

            //IdDocumento = _documento.IdDocumento;
            respuesta.Ruta = RutaArchivo;
            return respuesta;
        }

        public async Task<DocumentoResponse> GenerarXMLBaja(ComunicacionBaja _documento)
        {


            var proxy = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["UrlOpenInvoicePeruApi"]) };

            string metodoApi = "api/GenerarComunicacionBaja";


            var response = await proxy.PostAsJsonAsync(metodoApi, _documento);
            var respuesta = await response.Content.ReadAsAsync<DocumentoResponse>();
            if (!respuesta.Exito)
                throw new ApplicationException(respuesta.MensajeError);

            if (!Directory.Exists(_documento.RutaXML))
                Directory.CreateDirectory(_documento.RutaXML);

            string RutaArchivo = Path.Combine(_documento.RutaXML,
                $"{_documento.IdDocumento}.xml");

            File.WriteAllBytes(RutaArchivo, Convert.FromBase64String(respuesta.TramaXmlSinFirma));

            //IdDocumento = _documento.IdDocumento;
            respuesta.Ruta = RutaArchivo;
            return respuesta;
        }

        public async Task<DocumentoResponse> GenerarXMLResumenDiario(ResumenDiarioNuevo _documento)
        {


            var proxy = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["UrlOpenInvoicePeruApi"]) };

            string metodoApi = "api/GenerarResumenDiario/v2";


            var response = await proxy.PostAsJsonAsync(metodoApi, _documento);
            var respuesta = await response.Content.ReadAsAsync<DocumentoResponse>();
            if (!respuesta.Exito)
                throw new ApplicationException(respuesta.MensajeError);

            if (!Directory.Exists(_documento.RutaXML))
                Directory.CreateDirectory(_documento.RutaXML);

            string RutaArchivo = Path.Combine(_documento.RutaXML,
                $"{_documento.IdDocumento}.xml");

            File.WriteAllBytes(RutaArchivo, Convert.FromBase64String(respuesta.TramaXmlSinFirma));

            //IdDocumento = _documento.IdDocumento;
            respuesta.Ruta = RutaArchivo;
            return respuesta;
        }

        public async Task<[FromUri]RespuestaComunConArchivo2> EnviarSunat(string TipoDocumento, string CarpetaXml, string CarpetaCdr,
            string RutaCertificado, string ClaveCertificado, string RucEmisor, string UsuarioSol, string ClaveSol, string Correlativo,
            string RutaWSSunat, bool EsResumen)
        {
            RespuestaComunConArchivo2 respuestaEnvio = new RespuestaComunConArchivo2();
            RespuestaComunConArchivo respuestaEnviotemp;
            try
            {

                HttpClient _client = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["UrlOpenInvoicePeruApi"]) };


                if (string.IsNullOrEmpty(Correlativo))
                    throw new InvalidOperationException("La Serie y el Correlativo no pueden estar vacíos");

                string RutaArchivo = Path.Combine(CarpetaXml, $"{Correlativo}.xml");

                var tramaXmlSinFirma = Convert.ToBase64String(File.ReadAllBytes(RutaArchivo));

                var firmadoRequest = new FirmadoRequest
                {
                    TramaXmlSinFirma = tramaXmlSinFirma,
                    CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes(RutaCertificado)),
                    PasswordCertificado = ClaveCertificado,
                    UnSoloNodoExtension = EsResumen //rbRetenciones.Checked || rbResumen.Checked
                };

                var jsonFirmado = await _client.PostAsJsonAsync("api/Firmar", firmadoRequest);


                var respuestaFirmado = await jsonFirmado.Content.ReadAsAsync<FirmadoResponse>();
                if (!respuestaFirmado.Exito)
                    throw new ApplicationException(respuestaFirmado.MensajeError);

                var enviarDocumentoRequest = new EnviarDocumentoRequest
                {
                    Ruc = RucEmisor,
                    UsuarioSol = UsuarioSol,
                    ClaveSol = ClaveSol,
                    EndPointUrl = RutaWSSunat,
                    IdDocumento = Correlativo,
                    TipoDocumento = TipoDocumento,
                    TramaXmlFirmado = respuestaFirmado.TramaXmlFirmado
                };

                //var apiMetodo = rbResumen.Checked && codigoTipoDoc != "09" ? "api/EnviarResumen" : "api/EnviarDocumento";
                var apiMetodo = EsResumen && TipoDocumento != "09" ? "api/EnviarResumen" : "api/EnviarDocumento";

                var jsonEnvioDocumento = await _client.PostAsJsonAsync(apiMetodo, enviarDocumentoRequest);


                //string resultado = "";
                if (!EsResumen)
                {
                    var respuestaEnvioTemp = await jsonEnvioDocumento.Content.ReadAsAsync<EnviarDocumentoResponse>();
                    respuestaEnviotemp = respuestaEnvioTemp;

                    var rpta = (EnviarDocumentoResponse)respuestaEnviotemp;
                    //resultado = rpta.MensajeRespuesta + " " + "siendo las " + DateTime.Now.ToString();
                    try
                    {
                        if (rpta.Exito && !string.IsNullOrEmpty(rpta.TramaZipCdr))
                        {


                            if (!Directory.Exists(CarpetaXml))
                                Directory.CreateDirectory(CarpetaXml);
                            if (!Directory.Exists(CarpetaCdr))
                                Directory.CreateDirectory(CarpetaCdr);

                            File.WriteAllBytes($"{CarpetaXml}\\{respuestaEnviotemp.NombreArchivo}",
                                Convert.FromBase64String(respuestaFirmado.TramaXmlFirmado));

                            File.WriteAllBytes($"{CarpetaCdr}\\R-{respuestaEnviotemp.NombreArchivo}",
                                Convert.FromBase64String(rpta.TramaZipCdr));

                            respuestaEnvio.Exito = rpta.Exito;
                            respuestaEnvio.MensajeError = rpta.MensajeError;
                            respuestaEnvio.NombreArchivo = rpta.NombreArchivo;
                            respuestaEnvio.Pila = rpta.Pila;
                            respuestaEnvio.CodigoRespuesta = rpta.CodigoRespuesta;
                            respuestaEnvio.MensajeRespuesta = rpta.MensajeRespuesta;
                            respuestaEnvio.TramaZipCdr = rpta.TramaZipCdr;

                        }
                        else
                        {
                            respuestaEnvio.Exito = rpta.Exito;
                            respuestaEnvio.MensajeError = rpta.MensajeError;
                            respuestaEnvio.NombreArchivo = rpta.NombreArchivo;
                            respuestaEnvio.Pila = rpta.Pila;
                            respuestaEnvio.CodigoRespuesta = rpta.CodigoRespuesta;
                            respuestaEnvio.MensajeRespuesta = rpta.MensajeRespuesta;
                            respuestaEnvio.TramaZipCdr = rpta.TramaZipCdr;
                        }
                    }
                    catch (Exception ex)
                    {
                        respuestaEnviotemp.Exito = false;
                        respuestaEnviotemp.MensajeError = ex.Message;

                    }
                }
                else
                {
                    respuestaEnviotemp = await jsonEnvioDocumento.Content.ReadAsAsync<EnviarResumenResponse>();
                    var rpta = (EnviarResumenResponse)respuestaEnviotemp;
                    respuestaEnvio.NroTicket = rpta.NroTicket;
                    respuestaEnvio.NombreArchivo = rpta.NombreArchivo;
                    respuestaEnvio.MensajeError = rpta.MensajeError;
                    respuestaEnvio.Pila = rpta.Pila;
                    respuestaEnvio.Exito = rpta.Exito;
                    //txtResult.Text = $@"{Resources.procesoCorrecto}{Environment.NewLine}{rpta.NroTicket}";
                }

                //if (!respuestaEnvio.Exito)
                //    throw new ApplicationException(respuestaEnvio.MensajeError);
                //respuestaEnvio.Pila = "";
                //respuestaEnvio.MensajeError = "";

                return respuestaEnvio;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        //public string GenerarPdf(DataSet DS, string RutaGuardar, string NombreArchivo, String RespuestaSunat)
        //{
        //    try
        //    {
        //        CR.CrFE rpt = new CR.CrFE();
        //        DS.Tables[0].TableName = "Cabecera";
        //        DS.Tables[1].TableName = "Detalle";
        //        rpt.SetDataSource(DS);


        //        string TotalPagarLetras = new Utilitario().enletras((DS.Tables[0].Rows[0]["ImporteTotal"]).ToString());


        //        TextObject TxtMontoLetras;
        //        TxtMontoLetras = (TextObject)rpt.ReportDefinition.ReportObjects["TxtMontoLetras"];
        //        TxtMontoLetras.Text = TotalPagarLetras + " Soles.";

        //        TextObject TxtRespuestaSunat;
        //        TxtRespuestaSunat = (TextObject)rpt.ReportDefinition.ReportObjects["TxtRespuestaSunat"];
        //        TxtRespuestaSunat.Text = RespuestaSunat;

        //        //ExportOptions CrExportOptions;E:\erp\ERP_HALLEY-GRANJA_FACTURAE\openinvoiceperu-develop\OpenInvoicePeru\WCF_FE\FEService.cs
        //        //DiskFileDestinationOptions CrDiskFileDestinationOptions = new DiskFileDestinationOptions();
        //        //PdfRtfWordFormatOptions CrFormatTypeOptions = new PdfRtfWordFormatOptions();
        //        //CrDiskFileDestinationOptions.DiskFileName = RutaGuardar;
        //        //CrExportOptions = rpt.ExportOptions;
        //        //{
        //        //    CrExportOptions.ExportDestinationType = ExportDestinationType.DiskFile;
        //        //    CrExportOptions.ExportFormatType = ExportFormatType.PortableDocFormat;
        //        //    CrExportOptions.DestinationOptions = CrDiskFileDestinationOptions;
        //        //    CrExportOptions.FormatOptions = CrFormatTypeOptions;

        //        //}
        //        //rpt.Export();

        //        if (!Directory.Exists(RutaGuardar))
        //            Directory.CreateDirectory(RutaGuardar);

        //        rpt.ExportToDisk(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat, RutaGuardar + NombreArchivo + ".pdf");
        //        return "OK";
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.Message;
        //    }

        //}

        public async Task<[FromUri]RespuestaComunConArchivo2> EnviarXML(string RutaWS, string usuario, string clave, string RucEmisor,
      string TipoDocumento, string IdentificadorArchivo, bool EsResumen, string CarpetaXML, string CarpetaCdr)
        {
            RespuestaComunConArchivo2 respuestaEnvio = new RespuestaComunConArchivo2();
            RespuestaComunConArchivo respuestaEnviotemp;
            try
            {


                HttpClient _client = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["UrlOpenInvoicePeruApi"]) };

                //var xmlDoc = new XmlDocument();
                //xmlDoc.Load(@"E:\SFS_v1.2\sunat_archivos\sfs\FIRMA\20394050599-01-F001-00000037.xml");

                if (File.Exists(CarpetaXML + @"\" + RucEmisor + "-" + TipoDocumento + "-" + IdentificadorArchivo + ".xml") == false)
                {
                    respuestaEnvio.Exito = false;
                    respuestaEnvio.MensajeError = "No existe el archivo XML";
                    return respuestaEnvio;
                }
                //else {
                //    string text = File.ReadAllText(CarpetaXML + @"\" + RucEmisor + "-" + TipoDocumento + "-" + IdentificadorArchivo + ".xml");
                //    text = text.Replace("?>-", "?>");
                //    File.WriteAllText(CarpetaXML + @"\" + RucEmisor + "-" + TipoDocumento + "-" + IdentificadorArchivo + ".xml", text);


                //}
                byte[] arrayDeBytes = System.IO.File.ReadAllBytes(CarpetaXML + @"\" + RucEmisor + "-" + TipoDocumento + "-" + IdentificadorArchivo + ".xml");

                //Para codificar los bytes en base 64:
                string codificado = Convert.ToBase64String(arrayDeBytes);


                var enviarDocumentoRequest = new EnviarDocumentoRequest
                {
                    Ruc = RucEmisor,
                    UsuarioSol = usuario,
                    ClaveSol = clave,
                    EndPointUrl = RutaWS,
                    IdDocumento = IdentificadorArchivo,
                    TipoDocumento = TipoDocumento,
                    TramaXmlFirmado = codificado
                };

                //var apiMetodo = rbResumen.Checked && codigoTipoDoc != "09" ? "api/EnviarResumen" : "api/EnviarDocumento";
                var apiMetodo = EsResumen && TipoDocumento != "09" ? "api/EnviarResumen" : "api/EnviarDocumento";
                //var apiMetodo =  "api/EnviarDocumento";
                var jsonEnvioDocumento = await _client.PostAsJsonAsync(apiMetodo, enviarDocumentoRequest);


                //string resultado = "";
                if (!EsResumen)
                {
                    var respuestaEnvioTemp = await jsonEnvioDocumento.Content.ReadAsAsync<EnviarDocumentoResponse>();
                    respuestaEnviotemp = respuestaEnvioTemp;

                    var rpta = (EnviarDocumentoResponse)respuestaEnviotemp;
                    //resultado = rpta.MensajeRespuesta + " " + "siendo las " + DateTime.Now.ToString();
                    try
                    {
                        if (rpta.Exito && !string.IsNullOrEmpty(rpta.TramaZipCdr))
                        {


                            //if (!Directory.Exists(CarpetaXml))
                            //    Directory.CreateDirectory(CarpetaXml);
                            if (!Directory.Exists(CarpetaCdr))
                                Directory.CreateDirectory(CarpetaCdr);

                            //File.WriteAllBytes($"{CarpetaXml}\\{respuestaEnviotemp.NombreArchivo}",
                            //    Convert.FromBase64String(respuestaFirmado.TramaXmlFirmado));

                            File.WriteAllBytes($"{CarpetaCdr}\\R-{respuestaEnviotemp.NombreArchivo}.zip",
                                Convert.FromBase64String(rpta.TramaZipCdr));

                            respuestaEnvio.Exito = rpta.Exito;
                            respuestaEnvio.MensajeError = rpta.MensajeError;
                            respuestaEnvio.NombreArchivo = rpta.NombreArchivo;
                            respuestaEnvio.Pila = rpta.Pila;
                            respuestaEnvio.CodigoRespuesta = rpta.CodigoRespuesta;
                            respuestaEnvio.MensajeRespuesta = rpta.MensajeRespuesta;
                            respuestaEnvio.TramaZipCdr = rpta.TramaZipCdr;

                        }
                        else
                        {
                            respuestaEnvio.Exito = rpta.Exito;
                            respuestaEnvio.MensajeError = rpta.MensajeError;
                            respuestaEnvio.NombreArchivo = rpta.NombreArchivo;
                            respuestaEnvio.Pila = rpta.Pila;
                            respuestaEnvio.CodigoRespuesta = rpta.CodigoRespuesta;
                            respuestaEnvio.MensajeRespuesta = rpta.MensajeRespuesta;
                            respuestaEnvio.TramaZipCdr = rpta.TramaZipCdr;
                        }
                    }
                    catch (Exception ex)
                    {
                        respuestaEnviotemp.Exito = false;
                        respuestaEnviotemp.MensajeError = ex.Message;

                    }
                }
                else
                {
                    respuestaEnviotemp = await jsonEnvioDocumento.Content.ReadAsAsync<EnviarResumenResponse>();
                    var rpta = (EnviarResumenResponse)respuestaEnviotemp;
                    respuestaEnvio.NroTicket = rpta.NroTicket;
                    respuestaEnvio.NombreArchivo = rpta.NombreArchivo;
                    respuestaEnvio.MensajeError = rpta.MensajeError;
                    respuestaEnvio.Pila = rpta.Pila;
                    respuestaEnvio.Exito = rpta.Exito;
                }



                return respuestaEnvio;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        public async Task<EnviarDocumentoResponse> ObtenerTicket(string RutaWS, string usuario, string clave, string RucEmisor,
            string NroTicket, string IdentificadorArchivo, string CarpetaCdr)
        {

            HttpClient _client = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["UrlOpenInvoicePeruApi"]) };


            var consultaTicketRequest = new ConsultaTicketRequest
            {
                Ruc = RucEmisor,
                UsuarioSol = usuario,
                ClaveSol = clave,
                EndPointUrl = RutaWS,
                IdDocumento = IdentificadorArchivo,
                NroTicket = NroTicket
            };

            var jsonConsultaTicket = await _client.PostAsJsonAsync("api/ConsultarTicket", consultaTicketRequest);

            var respuestaEnvio = await jsonConsultaTicket.Content.ReadAsAsync<EnviarDocumentoResponse>();

            if (!respuestaEnvio.Exito || !string.IsNullOrEmpty(respuestaEnvio.MensajeError))
                throw new InvalidOperationException(respuestaEnvio.MensajeError);

            File.WriteAllBytes($"{CarpetaCdr}\\R-{respuestaEnvio.NombreArchivo}.zip",
                Convert.FromBase64String(respuestaEnvio.TramaZipCdr));

            return respuestaEnvio;

        }

    }
}
