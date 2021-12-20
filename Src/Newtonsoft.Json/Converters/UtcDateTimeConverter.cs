using System;
using System.Globalization;

using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Converters
{
    /// <summary>
    /// Converts a UTC <see cref="DateTime"/> to and from the ISO 8601 date format (e.g. <c>"2008-04-12T12:53Z"</c>).
    /// All values must be in UTC.
    /// </summary>
    public class UtcDateTimeConverter : DateTimeConverterBase
    {
        private const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK";

        private readonly string[] formats = new[] { DefaultDateTimeFormat, "o" };
        
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            string text;

            if (value is DateTime dateTime)
            {
                if (dateTime.Kind == DateTimeKind.Unspecified)
                    dateTime = new DateTime(dateTime.Ticks, DateTimeKind.Utc);
                
                //Contract.Assert(dateTime.Kind == DateTimeKind.Utc, $"Expected Utc, got {dateTime.Kind}?");
                if (dateTime.Kind != DateTimeKind.Utc)
                {
                    //Debug.Assert(dateTime.Kind == DateTimeKind.Utc, $"Expected Utc, got {dateTime.Kind}?");
                    throw new JsonSerializationException("Unexpected time zone when converting date. Expected Utc, got {0}.".FormatWith(CultureInfo.InvariantCulture, dateTime.Kind));
                }
                
                text = dateTime.ToString(DefaultDateTimeFormat, CultureInfo.InvariantCulture);
            }
            else if (value is DateTimeOffset dateTimeOffset)
            {
                DateTime dtUtc = dateTimeOffset.UtcDateTime;
                if (dtUtc.Kind != DateTimeKind.Utc)
                {
                    //Debug.Assert(dtUtc.Kind == DateTimeKind.Utc, $"Expected Utc, got {dtUtc.Kind}?");
                    throw new JsonSerializationException("Unexpected time zone when converting date. Expected Utc, got {0}.".FormatWith(CultureInfo.InvariantCulture, dtUtc.Kind));
                }
                
                // DateTimeOffset prints zone as TimeSpan. Not as 'Z'
                // text = dateTimeOffset.ToString(_dateTimeFormat ?? DefaultDateTimeFormat, Culture);
                text = dtUtc.ToString(DefaultDateTimeFormat, CultureInfo.InvariantCulture);
            }
            else
            {
                throw new JsonSerializationException("Unexpected value when converting date. Expected DateTime, got {0}.".FormatWith(CultureInfo.InvariantCulture, ReflectionUtils.GetObjectType(value)!));
            }

            writer.WriteValue(text);
        }
        
        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            bool nullable = ReflectionUtils.IsNullableType(objectType);
            if (reader.TokenType == JsonToken.Null)
            {
                if (!nullable)
                {
                    throw JsonSerializationException.Create(reader, "Cannot convert null value to {0}.".FormatWith(CultureInfo.InvariantCulture, objectType));
                }

                return null;
            }

            if (reader.TokenType == JsonToken.Date)
            {
                return reader.Value;
            }

            if (reader.TokenType != JsonToken.String)
            {
                throw JsonSerializationException.Create(reader, "Unexpected token parsing date. Expected String, got {0}.".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
            }

            string? dateText = reader.Value?.ToString();
            if (nullable)
            {
                if (StringUtils.IsNullOrEmpty(dateText))
                {
                    return null;
                }
            }
            else
            {
                if (StringUtils.IsNullOrEmpty(dateText))
                {
                    throw JsonSerializationException.Create(reader, "Cannot convert null value to {0}.".FormatWith(CultureInfo.InvariantCulture, objectType));
                }
            }

            char last = dateText[dateText.Length - 1];
            if ((last != 'Z') && (last != 'z'))
            {
                throw JsonSerializationException.Create(reader, "Unexpected time-zone symbol. I expect 'Z', got '{0}'.".FormatWith(CultureInfo.InvariantCulture, last));
            }
            
            if (DateTime.TryParseExact(dateText, formats, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime res))
            {
                if (res.Kind != DateTimeKind.Utc)
                {
                    throw JsonSerializationException.Create(reader, "Time-zone has changed after deserialization? Expected Utc, got {0}.".FormatWith(CultureInfo.InvariantCulture, res.Kind));
                }

                return res;
            }
            else
            {
                throw JsonSerializationException.Create(reader, "Unexpected DateTime format. I expect '{0}'.".FormatWith(CultureInfo.InvariantCulture, DefaultDateTimeFormat));
            }
        }
    }
}