
using System.Text;
using System.Xml;
using Microsoft.Extensions.Configuration;


namespace JETSample
{
    public class HRXML
    {

        private string UsernamePassword { get; set; }
        private static string Gcc { get; set; }
        private static string Url { get; set; }
        private int Sleep = 15;
        private readonly HttpClient _client = new();
        private IConfiguration Configuration { get; set; }
        

        public HRXML()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddJsonFile("jet.json");
            Configuration = configurationBuilder.Build();
            Gcc = Configuration["gcc"];
            Url = Configuration["url"];
            Sleep = Int32.Parse(Configuration["sleep"]);
            UsernamePassword = Convert.ToBase64String(new ASCIIEncoding().GetBytes(Configuration["username"] + ":" + Configuration["password"]));
        }

        public async Task Runner(string hrxml)
        {

            string xml = ReadPayload(hrxml);

            Console.WriteLine($"Processing {hrxml}");
            string mybod = await PerformSOAP(xml);
            Console.WriteLine($"Got BOD ID {mybod} and sleep for {Sleep} seconds");

            System.Threading.Thread.Sleep(Sleep * 000);
            await CheckResults(mybod);
        }

        public async Task CheckResults(string mybod)
        {
            string results = await GetResults(mybod);
            Console.WriteLine($"Got response {results} for BOD ID {mybod}");
        }


        private static string ReadPayload(string filename)
        {
            string text = System.IO.File.ReadAllText(filename);
            var plainText = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(plainText);
        }

        private XmlDocument ProcessXMLResponse(string xmlcontent)
        {
            XmlDocument d = new();
            XmlNamespaceManager nsmanager2 = new(d.NameTable);
            nsmanager2.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsmanager2.AddNamespace("nga", "nga.pex.ExternalInbound.webservices.provider:submitBod");
            d.LoadXml(xmlcontent);
            return d;
        }

        public async Task<String> PerformSOAP(string xml)
        {
            string ep = $"{Url}/ws/nga.pex.ExternalInbound.webservices.provider.submitBod/nga_pex_ExternalInbound_webservices_provider_submitBod_Port";

            string payload = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:nga=""nga.pex.ExternalInbound.webservices.provider:submitBod"">

  <soap:Body> <nga:submitBod><xmlIn>" + xml + "</xmlIn> </nga:submitBod></soap:Body></soap:Envelope>";
            HttpContent content = new StringContent(payload, Encoding.UTF8, "text/xml");
            HttpRequestMessage request = new(HttpMethod.Post, ep);
            request.Headers.Add("SOAPAction", ep);
            request.Headers.Add("Authorization", "Basic " + UsernamePassword);
            request.Content = content;
            var response = await _client.SendAsync(request);
            _ = response.EnsureSuccessStatusCode();
            var respbody = await response.Content.ReadAsStringAsync();
            XmlDocument bodid = ProcessXMLResponse(respbody);
            return bodid.InnerText;
        }

        public async Task<string> GetResults(string bodid)
        {

            string uri = $"{Url}/api/bods/{bodid}/gccs/{Gcc}/payroll-status";

            HttpRequestMessage request = new(HttpMethod.Get, uri);

            request.Headers.Add("Authorization", "Basic " + UsernamePassword);

            var response = await _client.SendAsync(request);

            _ = response.EnsureSuccessStatusCode();
            string res = await response.Content.ReadAsStringAsync();
            return res;


        }
    }
}