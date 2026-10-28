using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Mt940Parser
{
    /// <summary>
    /// A statement could not be parsed. <see cref="LineNumber"/> is 1-based and is 0 when
    /// the failure is not tied to a line. <see cref="Tag"/> is the SWIFT tag such as ":61:",
    /// or null when the failure is not tied to a tag.
    /// </summary>
    [Serializable]
    public class Mt940ParseException : Exception
    {
        /// <summary>
        /// Creates a parse exception for a message that is not tied to a line or tag.
        /// </summary>
        public Mt940ParseException(string message)
            : this(message, 0, null)
        {
        }

        /// <summary>
        /// Creates a parse exception for the tag that starts on <paramref name="lineNumber"/>.
        /// </summary>
        public Mt940ParseException(string message, int lineNumber, string tag)
            : base(FormatMessage(message, lineNumber, tag))
        {
            LineNumber = lineNumber;
            Tag = tag;
        }

        /// <summary>
        /// Creates a parse exception with an inner exception.
        /// </summary>
        public Mt940ParseException(string message, int lineNumber, string tag, Exception innerException)
            : base(FormatMessage(message, lineNumber, tag), innerException)
        {
            LineNumber = lineNumber;
            Tag = tag;
        }

        /// <summary>
        /// Serialization constructor.
        /// </summary>
        protected Mt940ParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            LineNumber = info.GetInt32("LineNumber");
            Tag = info.GetString("Tag");
        }

        /// <summary>1-based line of the tag that failed, or 0 if unknown.</summary>
        public int LineNumber { get; }

        /// <summary>SWIFT tag including colons, such as ":61:", or null.</summary>
        public string Tag { get; }

        /// <summary>
        /// Stores <see cref="LineNumber"/> and <see cref="Tag"/> with the base exception data.
        /// </summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            base.GetObjectData(info, context);
            info.AddValue("LineNumber", LineNumber);
            info.AddValue("Tag", Tag);
        }

        private static string FormatMessage(string message, int lineNumber, string tag)
        {
            if (string.IsNullOrEmpty(message))
                message = "The MT940 content could not be parsed.";

            if (lineNumber > 0 && !string.IsNullOrEmpty(tag))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Line {0} {1} {2}",
                    lineNumber,
                    tag,
                    message);
            }

            if (lineNumber > 0)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Line {0}: {1}",
                    lineNumber,
                    message);
            }

            if (!string.IsNullOrEmpty(tag))
                return "Tag " + tag + ": " + message;

            return message;
        }
    }
}
