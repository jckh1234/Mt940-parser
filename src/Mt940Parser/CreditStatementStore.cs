using System;
using System.Globalization;
using System.Text;

namespace Mt940Parser
{
    /// <summary>
    /// Pseudo-code insert of one <see cref="DbsCreditEntry"/>.
    /// No connection is opened and no SQL is executed.
    /// </summary>
    public static class CreditStatementStore
    {
        /// <summary>
        /// Builds the INSERT that a real database call would run, and returns that text.
        /// <paramref name="connectionString"/> is checked and then discarded. It is not written into the script.
        /// </summary>
        public static string Insert(string connectionString, DbsCreditEntry entry)
        {
            // PSEUDO CODE — database insert (not executed)
            //
            // IF connectionString is blank THEN
            //     fail "connection string is required"
            // IF entry is null THEN
            //     fail "credit entry is required"
            // IF entry.Field20AccountNumber is blank THEN
            //     fail "Field 20 account is required"
            //
            // OPEN connection using connectionString
            // BEGIN TRANSACTION
            //     INSERT INTO CreditStatement
            //     (
            //         AccountNumber,            -- Field 20
            //         ValueDate,                -- Field 61 value date
            //         Amount,                   -- Field 61 amount
            //         SupplementaryTimestamp,   -- Field 61 supplementary HHMMSS
            //         TransactionDescription,   -- Field 86 TD
            //         FpsProxy,                 -- Field 86 PV
            //         PayerName,                -- Field 86 ORDP
            //         CustomerReference,        -- Field 86 CREF
            //         TransParticular,          -- Field 86 OTH1
            //         PaymentPurposeCode,       -- Field 86 PURP
            //         RemittanceBankBicCode     -- Field 86 BC
            //     )
            //     VALUES (the entry fields)
            //
            //     IF the insert fails THEN
            //         ROLLBACK
            //         fail with the database error
            //     ELSE
            //         COMMIT
            // CLOSE connection

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is required.", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(entry.Field20AccountNumber))
                throw new ArgumentException("Field 20 account is required.", nameof(entry));

            var script = new StringBuilder();
            script.AppendLine("-- Pseudo-code insert. No database connection is opened.");
            script.AppendLine("BEGIN TRANSACTION;");
            script.AppendLine("INSERT INTO CreditStatement");
            script.AppendLine("(");
            script.AppendLine("    AccountNumber,");
            script.AppendLine("    ValueDate,");
            script.AppendLine("    Amount,");
            script.AppendLine("    SupplementaryTimestamp,");
            script.AppendLine("    TransactionDescription,");
            script.AppendLine("    FpsProxy,");
            script.AppendLine("    PayerName,");
            script.AppendLine("    CustomerReference,");
            script.AppendLine("    TransParticular,");
            script.AppendLine("    PaymentPurposeCode,");
            script.AppendLine("    RemittanceBankBicCode");
            script.AppendLine(")");
            script.AppendLine("VALUES");
            script.AppendLine("(");
            script.AppendLine("    " + Literal(entry.Field20AccountNumber) + ",");
            script.AppendLine("    " + Literal(entry.ValueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) + ",");
            script.AppendLine("    " + entry.Amount.ToString("0.00", CultureInfo.InvariantCulture) + ",");
            script.AppendLine("    " + Literal(entry.SupplementaryTimestamp) + ",");
            script.AppendLine("    " + Literal(entry.TransactionDescription) + ",");
            script.AppendLine("    " + Literal(entry.FpsProxy) + ",");
            script.AppendLine("    " + Literal(entry.PayerName) + ",");
            script.AppendLine("    " + Literal(entry.CustomerReference) + ",");
            script.AppendLine("    " + Literal(entry.TransParticular) + ",");
            script.AppendLine("    " + Literal(entry.PaymentPurposeCode) + ",");
            script.AppendLine("    " + Literal(entry.RemittanceBankBicCode));
            script.AppendLine(");");
            script.AppendLine("COMMIT;");
            return script.ToString();
        }

        private static string Literal(string value)
        {
            if (value == null)
                return "NULL";
            return "N'" + value.Replace("'", "''") + "'";
        }
    }
}
