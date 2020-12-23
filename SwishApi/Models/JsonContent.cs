using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SwishApi.Models
{
    public class JsonContent : StringContent
    {
        public JsonContent(object objectToSerialize) : base(JsonSerializer.Serialize(objectToSerialize), Encoding.UTF8, "application/json")
        {

        }
    }
}
