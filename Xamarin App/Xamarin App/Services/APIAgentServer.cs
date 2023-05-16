using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;

namespace Xamarin_App.Services
{
    public class RequestWebForm
    {
        public string userName {  get; set; }
        public string eMail { get; set; }
        public string password { get; set; }

        public string apiKey 
        {
            get { 
                return "11303e234269a05903a805d2d22b95d59ce0a1ffa4b33f470b19e108efe5313b"; //API Key
            }
        }

        public string nonce
        {
            get
            {
                return "NJm4KUUt7LVCbkkL3eDWZ2k046I08wuHAXFFELe+ZSA="; //
            }
        }

        public string signature
        {
            get
            {
                return "czTZW82KOqjtR8/8d1xZdl4kM4qUZ2ZhTXexe8eP0MY=";
            }
        }

        public int seconds { get; set; }
    }

    public class WebForm
    {
        public int created { get; set; }
        public bool enabled { get; set; }
        public bool canRelay { get; set; }
        public string jwt { get; set; }
        public int expires { get; set; }
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

    public class APIAgentServer
    {
        private const string SERVERURL = "";
        private const string CreateWebFormJavaScript = "await AgentAPI.Account.Create(UserName,EMail,Password,ApiKey,Secret,Seconds)";

        public async void CreateWebForm(RequestWebForm requestDetailst)
        {

            try
            {
                string domainName = "id.tagroot.io";
                SRV endpoint = await DnsResolver.LookupServiceEndpoint(domainName, "xmpp-client", "tcp");
                if (endpoint != null && !string.IsNullOrWhiteSpace(endpoint.TargetHost) && endpoint.Port > 0)
                {
                    string hostname = endpoint.TargetHost;
                    int port = endpoint.Port;
                }

                var obj = new Jint.Engine()
                                .SetValue("UserName", "AnishVartak")
                                .SetValue("EMail", "anishvartak@gmail.com")
                                .SetValue("Password", "Anish@123")
                                .SetValue("ApiKey", "11303e234269a05903a805d2d22b95d59ce0a1ffa4b33f470b19e108efe5313b")
                                .SetValue("Secret", "9abou9l6jJjcAb7vEc5zGhrKyGxA7y+i5NP8r0D68ME=")
                                .SetValue("Seconds", 60)
                                .Execute(CreateWebFormJavaScript);
                                //.GetCompletionValue()
                                //.ToObject();

                Console.WriteLine(obj.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        
        }


    }
}
