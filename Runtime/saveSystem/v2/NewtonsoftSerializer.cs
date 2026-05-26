using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Default <see cref="ISerializer"/> backed by Newtonsoft.Json. Chosen over Unity's JsonUtility
    /// because it handles dictionaries, polymorphism, nullables, and arbitrary nesting — all needed
    /// for schema migration to work cleanly.
    ///
    /// TypeNameHandling.Auto writes a "$type" tag for polymorphic fields so the concrete type is
    /// restored on load. ReferenceLoopHandling.Ignore prevents crashes on cyclic object graphs.
    /// </summary>
    public class NewtonsoftSerializer : ISerializer
    {
        private readonly JsonSerializerSettings m_Settings;
        private readonly JsonSerializer m_Serializer;

        public NewtonsoftSerializer(JsonSerializerSettings settings = null)
        {
            m_Settings = settings ?? new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
            };
            m_Serializer = JsonSerializer.Create(m_Settings);
        }

        public string Serialize(object obj) => JsonConvert.SerializeObject(obj, m_Settings);

        public T Deserialize<T>(string text) => JsonConvert.DeserializeObject<T>(text, m_Settings);

        public object Deserialize(string text, System.Type type) => JsonConvert.DeserializeObject(text, type, m_Settings);

        public void Populate(string text, object target) => JsonConvert.PopulateObject(text, target, m_Settings);

        public JObject ToJObject(object obj) => JObject.FromObject(obj, m_Serializer);

        public object FromJObject(JObject json, System.Type type) => json.ToObject(type, m_Serializer);

        // Populate fills an EXISTING instance (rather than constructing a new one), which preserves
        // any runtime references already handed out to the rest of the game.
        public void PopulateFromJObject(JObject json, object target)
        {
            using var reader = json.CreateReader();
            m_Serializer.Populate(reader, target);
        }
    }
}
