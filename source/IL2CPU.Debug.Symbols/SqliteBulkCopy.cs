using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace IL2CPU.Debug.Symbols
{
    public class SqliteBulkCopy : IDisposable
    {
        private bool mDisposed = false;

        public void Dispose()
        {
            if (mDisposed)
            {
                return;
            }
            mDisposed = true;
            GC.SuppressFinalize(this);
        }

        private readonly SqliteTransaction mTransaction;
        private readonly SqliteConnection mConnection;
        private readonly SqliteCommand mCommand;

        public SqliteBulkCopy(SqliteConnection connection)
        {
            mConnection = connection;
            mTransaction = mConnection.BeginTransaction();
            mCommand = mConnection.CreateCommand();
            mCommand.Transaction = mTransaction;
        }

        public string DestinationTableName { get; set; }

        public void WriteToServer(IDataReader reader)
        {
            if (reader.Read())
            {
                // initialize bulk copy

                var fieldNames = "";
                var paramNames = "";
                SqliteParameter[] parms = new SqliteParameter[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string xFieldName = reader.GetName(i);
                    fieldNames += $"{xFieldName},";
                    paramNames += $"@_{xFieldName},";
                    parms[i] = new SqliteParameter($"@_{xFieldName}", SqliteType.Text);
                    mCommand.Parameters.Add(parms[i]);
                }
                fieldNames = fieldNames.TrimEnd(',');
                paramNames = paramNames.TrimEnd(',');

                mCommand.CommandText = $"insert into [{DestinationTableName}] ({fieldNames}) values ({paramNames})";
                mCommand.Prepare();
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (parms[i] != null)
                        {
                            parms[i].Value = reader.GetValue(i);
                        }
                    }
                    mCommand.ExecuteNonQuery();
                }
                mTransaction.Commit();
            }
        }
    }
}
