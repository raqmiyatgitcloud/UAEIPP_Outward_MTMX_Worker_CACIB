using System.Xml.Serialization;

namespace Raqmiyat.Framework.Model.ServiceResponse
{
    [XmlRoot(ElementName = "respStat")]
    public class RespStat
    {
        [XmlElement(ElementName = "wsiRefNo")]
        public string? WsiRefNo { get; set; }
        [XmlElement(ElementName = "responseCode")]
        public string? ResponseCode { get; set; }
        [XmlElement(ElementName = "responseMesaage")]
        public string? ResponseMessage { get; set; }
    }

    [XmlRoot(ElementName = "wsData")]
    public class WsData
    {
        [XmlElement(ElementName = "iban")]
        public string? Iban { get; set; }
    }

    [XmlRoot(ElementName = "wsResponse")]
    public class WsResponse
    {
        [XmlElement(ElementName = "title")]
        public string? Title { get; set; }
        [XmlElement(ElementName = "accountType")]
        public string? AccountType { get; set; }
        [XmlElement(ElementName = "idType")]
        public string? IdType { get; set; }
        [XmlElement(ElementName = "id")]
        public string? Id { get; set; }
    }

    [XmlRoot(ElementName = "Response")]
    public class Response
    {
        [XmlElement(ElementName = "respStat")]
        public RespStat? RespStat { get; set; }
        [XmlElement(ElementName = "wsData")]
        public WsData? WsData { get; set; }
        [XmlElement(ElementName = "wsResponse")]
        public WsResponse? WsResponse { get; set; }
    }

    [XmlRoot(ElementName = "responseList")]
    public class ResponseList
    {
        [XmlElement(ElementName = "Response")]
        public Response? Response { get; set; }
    }

    [XmlRoot(ElementName = "responseDetail")]
    public class ResponseDetail
    {
        [XmlElement(ElementName = "respStat")]
        public RespStat? RespStat { get; set; }
        [XmlElement(ElementName = "responseList")]
        public ResponseList? ResponseList { get; set; }
    }


}
