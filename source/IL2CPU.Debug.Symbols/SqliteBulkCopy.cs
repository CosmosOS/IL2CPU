using Microsoft.Data.Sqlite;
using System.Data;
using System.Text;
using System;

namespace IL2CPU.Debug.Symbols
{
    public class SqliteBulkCopy : IDisposable
    {
        #region Constructors

        public SqliteBulkCopy(SqliteConnection connection)
        {
            mConnection = connection;
            mTransaction = mConnection.BeginTransaction();
            mCommand = mConnection.CreateCommand();
            mCommand.Transaction = mTransaction;
            mFieldNames = new StringBuilder();
            mParamNames = new StringBuilder();
        }

        #endregion

        #region Properties

        public string DestinationTableName { get; set; }

        #endregion

        #region Methods

        public void WriteToServer(IDataReader reader)
        {
            if (reader.Read())
            {
                // initialize bulk copy

                mFieldNames.Clear();
                mParamNames.Clear();

                SqliteParameter[] parms = new SqliteParameter[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string xFieldName = reader.GetName(i);
                    mFieldNames.Append($"{xFieldName},");
                    mParamNames.Append($"@_{xFieldName},");
                    parms[i] = new SqliteParameter($"@_{xFieldName}", SqliteType.Text);
                    mCommand.Parameters.Add(parms[i]);
                }

                mCommand.CommandText = $"insert into [{DestinationTableName}] ({mFieldNames.ToString().TrimEnd(',')}) values ({mParamNames.ToString().TrimEnd(',')})";
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

        public void Dispose()
        {
            if (mDisposed)
            {
                return;
            }
            mDisposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Fields

        private readonly SqliteTransaction mTransaction;
        private readonly SqliteConnection mConnection;
        private readonly StringBuilder mFieldNames;
        private readonly StringBuilder mParamNames;
        private readonly SqliteCommand mCommand;
        private bool mDisposed = false;

        #endregion
    }
}