
using System.Text;
using System.Xml;
using Microsoft.Extensions.Configuration;


namespace JETSample
{
    public class HRXML
    {
        private static string Password { get; set; }
        private static string Username { get; set; }
        private string UsernamePassword { get; set; }
        private static string Gcc { get; set; }
        private static string Url { get; set; }
        private readonly HttpClient _client = new HttpClient();
        private IConfiguration Configuration { get; set; }
        private int Sleep = 15;

        public HRXML()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddJsonFile("jet.json");
            Configuration = configurationBuilder.Build();
            Password = Configuration["password"];
            Username = Configuration["username"];
            UsernamePassword = Convert.ToBase64String(new ASCIIEncoding().GetBytes(Username + ":" + Password));
            Gcc = Configuration["gcc"];
            Url = Configuration["url"];
            Sleep = Int32.Parse(Configuration["sleep"]); 
        }

        public async Task Runner(string hrxml) {

            var xml = Readpayload(hrxml);

            Console.WriteLine($"Processing {hrxml}");
            var mybod = await PerformSOAP(xml);
            Console.WriteLine($"Got BOD ID {mybod} and sleep for {Sleep} seconds");
           
            System.Threading.Thread.Sleep(Sleep * 000);
            await CheckResults(mybod);
        }

        public async Task CheckResults(string mybod)
        {
            string results = await getResults(mybod);
            Console.WriteLine($"Got response {results} for BOD ID {mybod}");
        }


        private static string Readpayload(string filename)
        {
            string text = System.IO.File.ReadAllText(filename);
            var plainText = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(plainText);
        }

        private XmlDocument ProcessXML(string xmlcontent)
        {
            XmlDocument d = new XmlDocument();
            XmlNamespaceManager nsmanager2 = new XmlNamespaceManager(d.NameTable);
            nsmanager2.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsmanager2.AddNamespace("nga", "nga.pex.ExternalInbound.webservices.provider:submitBod");
            d.LoadXml(xmlcontent);
            return d;
        }

        public async Task<String> PerformSOAP(string xml)
        {
   


            var ep = $"{Url}/ws/nga.pex.ExternalInbound.webservices.provider.submitBod/nga_pex_ExternalInbound_webservices_provider_submitBod_Port";

            var payload = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:nga=""nga.pex.ExternalInbound.webservices.provider:submitBod"">

  <soap:Body> <nga:submitBod><xmlIn>" + xml + "</xmlIn> </nga:submitBod></soap:Body></soap:Envelope>";



            HttpContent content = new StringContent(payload, Encoding.UTF8, "text/xml");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ep);
            request.Headers.Add("SOAPAction", ep);
            request.Headers.Add("Authorization", "Basic " + UsernamePassword);
            request.Content = content;
            var response =  await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var respbody = await response.Content.ReadAsStringAsync();
            XmlDocument bodid = ProcessXML(respbody);
            return bodid.InnerText;


        }







        public async Task<string> getResults(string bodid)
        {

            var uri = $"{Url}/api/bods/{bodid}/gccs/{Gcc}/payroll-status";
     
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            request.Headers.Add("Authorization", "Basic " + UsernamePassword);

            var response = await _client.SendAsync(request);

            _ = response.EnsureSuccessStatusCode();
            var res = await response.Content.ReadAsStringAsync();
            return res;


        }
    }
}