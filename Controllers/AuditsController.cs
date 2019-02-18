using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace kubernetes_audit_webhook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditsController : ControllerBase
    {
        // POST api/audits
        [HttpPost]
        public void Post(dynamic auditBody)
        {
            Console.WriteLine($"Received Audit Webhook Post at {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}");
            try
            {
                var parsedJson = JObject.Parse(auditBody.ToString());
                JArray itemsArray = (JArray)parsedJson["items"];

                foreach (var auditEvent in itemsArray.Children())
                {
                    var timestamp = auditEvent["stageTimestamp"];
                    var level = auditEvent["level"];
                    var stage = auditEvent["stage"];
                    var requestURI = auditEvent["requestURI"];
                    var verb = auditEvent["verb"];

                    var username = "UNKNOWN";
                    var userElement = auditEvent["user"];
                    if (userElement != null)
                    {
                        var userNameElement = userElement["username"];
                        if (userNameElement != null)
                        {
                            username = userElement["username"].ToString();
                        }
                    }

                    string sourceIPValue = "";
                    JArray sourceIPArray = (JArray)auditEvent["sourceIPs"];
                    if (sourceIPArray.Count == 0)
                    {
                        sourceIPValue = "UNKNOWN";
                    }
                    else if (sourceIPArray.Count == 1)
                    {
                        sourceIPValue = sourceIPArray[0].ToString();
                    }
                    else
                    {
                        var ipAddresses = new List<string>();
                        foreach (JToken ipAddress in sourceIPArray)
                        {
                            ipAddresses.Add(ipAddress.ToString());
                        }

                        sourceIPValue = String.Join(",", ipAddresses);
                    }

                    var resourceType = "UNKNOWN";
                    var resourceName = "UNKNOWN";

                    var objectRefElement = auditEvent["objectRef"];
                    if (objectRefElement != null)
                    {
                        var resourceTypeElement = objectRefElement["resource"];
                        if (resourceTypeElement != null)
                        {
                            resourceType = resourceTypeElement.ToString();
                        }

                        var resourceNameElement = objectRefElement["name"];
                        if (resourceNameElement != null)
                        {
                            resourceName = resourceNameElement.ToString();
                        }
                    }

                    var authorizationDecision = "UNKNOWN";
                    var authorizationReason = "UNKNOWN";

                    var annotationsElement = auditEvent["annotations"];
                    if (annotationsElement != null)
                    {
                        authorizationDecision = annotationsElement["authorization.k8s.io/decision"].ToString();
                        authorizationReason = annotationsElement["authorization.k8s.io/reason"].ToString();
                    }

                    // starting each actual record line with "###" so I can distinguish them in the log file from any errors
                    Console.WriteLine($"###{timestamp},{level},{stage},{requestURI},{verb},{username},{sourceIPValue},{resourceType},{resourceName},{authorizationDecision},{authorizationReason}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UNABLE TO PROCESS RECORD DUE TO ERROR: {ex.Message} - THE FULL POST BODY WILL BE SHOWN BELOW:");
                Console.WriteLine(auditBody.ToString());
            }
        }
    }
}
