using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using apdb_3.Classes.GearTypes;
using System;

namespace apdb_3.Classes
{
    public class GearConverter : JsonConverter<Gear>
    {
        public override Gear ReadJson(JsonReader reader, Type objectType, Gear existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            Gear gear;

            if (jo["Mpx"] != null)
                gear = new Camera();
            else if (jo["Brand"] != null)
                gear = new GamingConsole();
            else if (jo["Processor"] != null)
                gear = new Laptop();
            else
                gear = new Gear();

            serializer.Populate(jo.CreateReader(), gear);
            return gear;
        }

        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, Gear value, JsonSerializer serializer)
            => throw new NotImplementedException();
    }
}