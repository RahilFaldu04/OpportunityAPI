using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MyWeatherAgent.Plugins
{
    public class Dataverse
    {
        private readonly HttpClient _httpClient;
        private readonly AuthHelper _authHelper;

        public Dataverse(HttpClient httpClient, AuthHelper authHelper)
        {
            _httpClient = httpClient;
            _authHelper = authHelper;
        }

        public async Task<List<RecordResult>> QueryEntityAsync(string entityLogicalName, List<string> attributes)
        {
            string token = await _authHelper.GetAccessTokenAsync();
            string dataverseUrl = _authHelper.GetDataverseUrl();

            string attributeXml = string.Join("\n", attributes.Select(attr => $"<attribute name='{attr}' />"));
            string filterAttribute = attributes.First();
            string fetchXml = $@"
                            <fetch top='5'>
                              <entity name='{entityLogicalName}'>
                                {attributeXml}
                              </entity>
                            </fetch>";
            var pluralEntityName = String.Empty;
            if(String.Equals(entityLogicalName,"opportunity"))
            {
                entityLogicalName = "opportunities";
            }
            else
            {
                entityLogicalName = entityLogicalName + 's';
            }
                var request = new HttpRequestMessage(
                    HttpMethod.Get, $"{dataverseUrl}/api/data/v9.2/{entityLogicalName}?fetchXml={Uri.EscapeDataString(fetchXml)}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var jsonNode = JsonNode.Parse(content);
            var records = jsonNode?["value"]?.AsArray();
            var recordList = new List<RecordResult>();
            if (records == null || records.Count == 0)
                return recordList;

            foreach (var record in records)
            {
                var result = new RecordResult();
                foreach (var attr in attributes)
                {
                    var val = record?[attr]?.ToString() ?? "";
                    result.Attributes[attr] = val;
                }
                recordList.Add(result);
            }

            return recordList;
        }
        public string CreateRecordCard(List<RecordResult> records)
        {
            var card = new
            {
                type = "AdaptiveCard",
                version = "1.3",
                body = new List<object>(),
                schema = "http://adaptivecards.io/schemas/adaptive-card.json" // Note the "$schema" fix
            };

            foreach (var rec in records)
            {
                var items = new List<object>();
                foreach (var kvp in rec.Attributes)
                {
                    items.Add(new
                    {
                        type = "TextBlock",
                        text = $"**{kvp.Key}**: {kvp.Value}",
                        wrap = true,
                        color = "default"
                    });
                }

                card.body.Add(new
                {
                    type = "Container",
                    style = "default", // Forces light background
                    items = items,
                    separator = true,
                    spacing = "Medium"
                });
            }

            return JsonSerializer.Serialize(card);
        }


    }
    public class RecordResult
    {
        public Dictionary<string, string> Attributes { get; set; } = new();
    }

}
