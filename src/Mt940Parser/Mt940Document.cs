using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mt940Parser.Parsing;

namespace Mt940Parser
{
    /// <summary>
    /// One or more MT940 customer statements from a single message or file.
    /// Statements are returned in the order they appear.
    /// </summary>
    public sealed class Mt940Document
    {
        /// <summary>
        /// Creates a document from statements that were already parsed. The sequence is copied.
        /// </summary>
        public Mt940Document(IEnumerable<Mt940Statement> statements)
        {
            if (statements == null)
                throw new ArgumentNullException(nameof(statements));

            var copy = new List<Mt940Statement>();
            foreach (var statement in statements)
            {
                if (statement != null)
                    copy.Add(statement);
            }

            Statements = copy.AsReadOnly();
        }

        /// <summary>Statements in file order.</summary>
        public IReadOnlyList<Mt940Statement> Statements { get; }

        /// <summary>
        /// Parses MT940 text. Throws <see cref="Mt940ParseException"/> at the first problem.
        /// </summary>
        public static Mt940Document Parse(string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            IList<Mt940ParseException> errors;
            var document = ParseCore(content, out errors);
            if (errors.Count > 0)
                throw errors[0];
            return document;
        }

        /// <summary>
        /// Parses an MT940 stream as UTF-8. A byte-order mark selects the encoding when present.
        /// The stream is left open.
        /// </summary>
        public static Mt940Document Parse(Stream stream)
        {
            return Parse(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Parses an MT940 stream with the given fallback encoding. The stream is left open.
        /// </summary>
        public static Mt940Document Parse(Stream stream, Encoding encoding)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            return Parse(ReadAll(stream, encoding));
        }

        /// <summary>
        /// Parses MT940 text and collects every problem instead of throwing.
        /// Returns false when <paramref name="errors"/> is not empty. <paramref name="document"/>
        /// still contains every statement that could be read.
        /// </summary>
        public static bool TryParse(string content, out Mt940Document document, out IList<Mt940ParseException> errors)
        {
            if (content == null)
            {
                document = new Mt940Document(new Mt940Statement[0]);
                errors = new List<Mt940ParseException>
                {
                    new Mt940ParseException("Content is null.", 0, null)
                };
                return false;
            }

            document = ParseCore(content, out errors);
            return errors.Count == 0;
        }

        /// <summary>
        /// Parses a UTF-8 stream and collects every problem. The stream is left open.
        /// </summary>
        public static bool TryParse(Stream stream, out Mt940Document document, out IList<Mt940ParseException> errors)
        {
            return TryParse(stream, Encoding.UTF8, out document, out errors);
        }

        /// <summary>
        /// Parses a stream and collects every problem. The stream is left open.
        /// A null encoding throws <see cref="ArgumentNullException"/>.
        /// </summary>
        public static bool TryParse(
            Stream stream,
            Encoding encoding,
            out Mt940Document document,
            out IList<Mt940ParseException> errors)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (stream == null)
            {
                document = new Mt940Document(new Mt940Statement[0]);
                errors = new List<Mt940ParseException>
                {
                    new Mt940ParseException("Stream is null.", 0, null)
                };
                return false;
            }

            return TryParse(ReadAll(stream, encoding), out document, out errors);
        }

        private static Mt940Document ParseCore(string content, out IList<Mt940ParseException> errors)
        {
            errors = new List<Mt940ParseException>();
            return Mt940Reader.Read(content, errors);
        }

        private static string ReadAll(Stream stream, Encoding encoding)
        {
            using (var reader = new StreamReader(stream, encoding, true, 4096, true))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
