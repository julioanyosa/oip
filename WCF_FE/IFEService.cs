using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using OpenInvoicePeru.Comun.Dto.Modelos;
using OpenInvoicePeru.Comun.Dto.Intercambio;
using System.Threading.Tasks;
using System.Data;

namespace WCF_FE
{


    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IFEService
    {

        [OperationContract]
        Task<DocumentoResponse> GenerarXMLFactura(DocumentoElectronico _documento);

        [OperationContract]
        Task<DocumentoResponse> GenerarXMLBaja(ComunicacionBaja _documento);

        [OperationContract]
        Task<DocumentoResponse> GenerarXMLResumenDiario(ResumenDiarioNuevo _documento);

        // TODO: Add your service operations here
        [OperationContract]
        Task<RespuestaComunConArchivo2> EnviarSunat(string TipoDocumento, string RutaXml, string RutaCdr,
          string RutaCertificado, string ClaveCertificado, string RucEmisor, string UsuarioSol, string ClaveSol, string Correlativo,
          string RutaWSSunat, bool EsResumen);


        //[OperationContract]
        //string GenerarPdf(DataSet DS, string RutaGuardar, string NombreArchivo, String RespuestaSunat);

        [OperationContract]
        Task<RespuestaComunConArchivo2> EnviarXML(string RutaWS, string usuario, string clave, string RucEmisor,
      string TipoDocumento, string IdentificadorArchivo, bool EsResumen, string CarpetaXML, string CarpetaCdr);

        [OperationContract]
        Task<EnviarDocumentoResponse> ObtenerTicket(string RutaWS, string usuario, string clave, string RucEmisor,
            string NroTicket, string IdentificadorArchivo, string CarpetaCdr);

    }

    [DataContract]
    public class RespuestaComunConArchivo2
    {
        [DataMember]
        public string NombreArchivo { get; set; }
        [DataMember]
        public bool Exito { get; set; }
        [DataMember]
        public string MensajeError { get; set; }
        [DataMember]
        public string Pila { get; set; }
        [DataMember]
        public string CodigoRespuesta { get; set; }
        [DataMember]
        public string MensajeRespuesta { get; set; }
        [DataMember]
        public string TramaZipCdr { get; set; }
        [DataMember]
        public string NroTicket { get; set; }

    }

}
