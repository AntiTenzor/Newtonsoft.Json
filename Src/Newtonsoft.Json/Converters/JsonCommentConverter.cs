using System;

namespace Newtonsoft.Json.Converters
{
    /// <summary>
    /// Special converter to add some user comment into generated JSON
    /// </summary>
    public class JsonCommentConverter : JsonConverter
    {
        private readonly string _comment;

        /// <summary>
        /// Initialize converter instance with user-defined comment
        /// </summary>
        public JsonCommentConverter(string comment)
        {
            _comment = comment;
        }

        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => true;

        /// <inheritdoc/>
        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteValue(value);

            if (_comment == null)
                return;

            int len = _comment.Length;
            bool hasNormalChar = false;
            for (int j = 0; j < len; j++)
            {
                if (Char.IsWhiteSpace(_comment[j]))
                    continue;

                hasNormalChar = true;
                break;
            }

            if (hasNormalChar)
            {
                writer.WriteComment(_comment); // append comment
            }
        }

        /// <inheritdoc/>
        public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue,
            Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
