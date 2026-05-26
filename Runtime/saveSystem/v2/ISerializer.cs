using Newtonsoft.Json.Linq;

namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Abstraction over HOW objects become JSON. Default is <see cref="NewtonsoftSerializer"/>.
    /// Swap via <see cref="SaveSystem.Serializer"/> to use MessagePack, Odin, etc.
    ///
    /// The JObject methods are what the save pipeline actually uses: it keeps data as a JObject
    /// between deserialize and populate so migrations can mutate the tree before it touches the
    /// live module instance.
    /// </summary>
    public interface ISerializer
    {
        string Serialize(object obj);
        T Deserialize<T>(string text);
        object Deserialize(string text, System.Type type);
        void Populate(string text, object target);

        JObject ToJObject(object obj);
        object FromJObject(JObject json, System.Type type);

        /// <summary>Fill an existing instance from a JObject (preserves references already handed out).</summary>
        void PopulateFromJObject(JObject json, object target);
    }
}
