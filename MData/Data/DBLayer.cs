﻿

using MData.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;


namespace MData
{
    public class DBLayer : IDisposable
    {
        /******************************************************************************************************/

        #region Fields
        private const string CONNECTION_STRING = @"Server=PATRICK_MSI\SQL2014;Database=mdata;Trusted_Connection=True;";

        private readonly bool keepOpen = false;
        private string transName = "";
        private bool hasTransaction = false;
        private SqlConnection connection; // changing from readonly to enable connection reset to new timeout
        private SqlCommand command;
        private SqlDataAdapter dataAdapter;
        private int sqlTimeOutInSeconds = 30;
        
        public int SqlTimeOutInSeconds
        {
            get { return sqlTimeOutInSeconds; }
            set
            {

                sqlTimeOutInSeconds = value;
                connection = InitializeSQLConnection();
            }
        }


        SqlTransaction transaction;
        private object returnValue;



        public object ReturnValue
        {

            set { returnValue = value; }
        }

        #endregion

        /******************************************************************************************************/

        #region Constructors



        public SqlConnection InitializeSQLConnection()
        {
            // forced override of connString
            string connectionString = string.Empty;

            return new SqlConnection(CONNECTION_STRING);
        }

        
        public DBLayer()
        {

            connection = InitializeSQLConnection();
        }


        private DBLayer(bool keepConnectionOpen, string transactionName)
        {
            keepOpen = keepConnectionOpen;
            transName = transactionName;

            if (!string.IsNullOrEmpty(transName))
            {
                hasTransaction = true;
            }

            //Removed - see OpenConnectionAsAppropriate for creation of transaction
            //transaction = new SqlTransaction();

            connection = InitializeSQLConnection();
        }

        public static object TopRow(DataSet ds, string columnName = "")
        {
            if (!ValidateHasRows(ds))
                return null;
            if (columnName.Length == 0)
                return ds.Tables[0].Rows[0];
            else
                return ds.Tables[0].Rows[0][columnName];
        }


        #endregion


        /******************************************************************************************************/

        #region Methods

        /// <summary>
        /// Opens the connection as appropriate per biz rules and status.  Also establishes and sets transaction if specified
        /// </summary>
        private void OpenConnectionAsAppropriate()
        {
            if (connection.State == ConnectionState.Broken || connection.State == ConnectionState.Closed)
            {

                connection.Open();

                command = new SqlCommand { Connection = connection };
                command.CommandTimeout = SqlTimeOutInSeconds;
                if (hasTransaction)
                {
                    // Start a local transaction.
                    transaction = connection.BeginTransaction(transName);
                    command.Transaction = transaction;
                }
            }
        }

        /// <summary>
        /// Closes the connection as appropriate per keepOpen specification for transactions.
        /// </summary>
        private void CloseConnectionAsAppropriate()
        {
            if (!keepOpen)
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        public void CommitTransaction()
        {
            transaction.Commit();
        }

        // TODO: Remove Dead Code
        public List<SqlParameter> GetParameterListForUpsert(DataModel objectToUse)
        {
            List<SqlParameter> result = new List<SqlParameter>();
            try
            {
                foreach (PropertyInfo propertyInfo in objectToUse.GetType().GetProperties())
                {
                    if (!Attribute.IsDefined(propertyInfo, typeof(ExcludeAsSqlParam)))
                    {
                        object paramValue = propertyInfo.GetValue(objectToUse);


                        if (objectToUse.RuleSet.ContainsKey(propertyInfo.PropertyType))
                        {
                            var ruleType = objectToUse.RuleSet[propertyInfo.PropertyType];

                            if (ruleType.GetType() == typeof(RulesCaster))
                            {
                                paramValue = Convert.ChangeType(paramValue, (ruleType as RulesCaster).CastTypeParameter);
                            }
                            else { }
                        }

                        result.Add(new SqlParameter("@" + propertyInfo.Name, paramValue));
                    }
                }
            }
            catch (Exception ex)
            {
                throw(ex);
            }
            return result;
        }




        /// <summary>
        /// Rollbacks the transaction.
        /// </summary>
        public void RollbackTransaction()
        {
            //if (transaction != null && connection.State == ConnectionState.Open)
            //{
            //    transaction.Rollback();
            //}
            //else
            //{
            //    //TODO: log error couldn't rollback transaction
            //}
            try
            {
                transaction.Rollback();
            }
            catch (Exception e)
            {
                //TODO: log error couldn't rollback transaction
                throw(e);
            }
        }

        /// <summary>
        /// Sets the transaction.
        /// </summary>
        /// <param name="transactionName">Name of the transaction.</param>
        public void SetTransaction(string transactionName)
        {
            //command.Transaction = new SqlTransaction();
            transName = transactionName;
            hasTransaction = true;

            // Start a local transaction
            if (connection.State == ConnectionState.Open)
            {
                transaction = connection.BeginTransaction(transName);
                command.Transaction = transaction;
            }

        }



        /// <summary>
        /// Clears the transaction string and bool setting.  Does not end existing transactions.
        /// </summary>
        public void ClearTransaction()
        {
            transName = "";
            hasTransaction = false;
        }


        public DataSet GetDataSet(string storedProcedureName, List<SqlParameter> parameters)
        {
            int returnValue = -1; // don't get return value
            return GetDataSet(storedProcedureName, parameters, ref returnValue);
        }

        public static DataSet GetDataSetStatic(string storedProcedureName, List<SqlParameter> parameters)
        {
            int returnValue = -1; // don't get return value
            return GetDataSetStatic(storedProcedureName, parameters, ref returnValue);
        }
        public static DataSet GetDataSetStatic(string storedProcedureName, List<SqlParameter> parameters, ref int returnValue)
        {

            DBLayer dbLayer= new DBLayer();
            DataSet result = dbLayer.GetDataSet(storedProcedureName, parameters, ref returnValue);
            dbLayer.Dispose();
            return result;
        }


        /// <summary>
        /// Gets a data set.
        /// </summary>
        /// <param name="storedProcedureName">Name of the stored procedure.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The representing DataSet</returns>
        public DataSet GetDataSet(string storedProcedureName, List<SqlParameter> parameters, ref int returnValue)
        {
            DataSet ds = new DataSet();
            OpenConnectionAsAppropriate();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = storedProcedureName;
            command.Parameters.Clear();
            try
            {
                if (parameters != null)
                {
                    //command.Parameters.Add(parameters);
                    foreach (SqlParameter p in parameters)
                    {
                        command.Parameters.Add(p);
                    }
                    if (returnValue >= 0)
                    {
                        var param = new SqlParameter("ReturnValue", DBNull.Value);
                        param.Direction = ParameterDirection.ReturnValue;
                        command.Parameters.Add(param);
                    }
                }

                dataAdapter = new SqlDataAdapter(command);

                ShowSQLCall(command);

                dataAdapter.Fill(ds);
                // need to think through no return value
                if (returnValue >= 0)
                    returnValue = (dataAdapter.SelectCommand.Parameters["ReturnValue"] == null ? 0 : Convert.ToInt32(dataAdapter.SelectCommand.Parameters["ReturnValue"].Value));


            }
            catch (Exception e)
            {
                throw(e);
            }

            command.Parameters.Clear();

            CloseConnectionAsAppropriate();
            return ds;
        }

        public string ShowSQLCall(SqlCommand command)
        {
            string result = "\r\n" + command.CommandText + " ";
            // this was not picking the params off so am sending them through as a separate param for now.
            //foreach (SqlParameter param in command.Parameters)
            foreach (SqlParameter param in command.Parameters)
            {
                result += param.ParameterName + " = " + param.Value + ",";
            }
            result = result.Substring(0, result.Length - 1);
            result += "\r\n";
            Debug.WriteLine(result);
            return result;
        }
      
        public DataSet GetDataSetUsingReader(string storedProcedureName, List<SqlParameter> parameters)
        {
            DataSet ds = new DataSet();
            OpenConnectionAsAppropriate();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = storedProcedureName;
            command.Parameters.Clear();

            if (parameters != null)
            {
                //command.Parameters.Add(parameters);
                foreach (SqlParameter p in parameters)
                {
                    command.Parameters.Add(p);
                }
            }
            SqlDataReader reader = command.ExecuteReader();

            

            DataTable dt = new DataTable();

         
            int currentFieldCount = 0;
            while (reader.Read())
            {
                if (currentFieldCount != reader.FieldCount)
                {
                    if (dt.Columns.Count > 0)
                    {
                        ds.Tables.Add(dt);
                    }
                    currentFieldCount = reader.FieldCount;  // not reliable but good enough for this one reporting sproc
                    dt = CreateDataTableFromReader(reader);

                }

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var row = dt.NewRow();
                    row[i] = (IDataRecord)reader[i];
                }

            }
            ds.Tables.Add(dt);
            // CHRIS: did I write this and is this necessary? This is why I have to pass the params through to ShowSQL()
            command.Parameters.Clear();

            CloseConnectionAsAppropriate();
            return ds;
        }

        private DataTable CreateDataTableFromReader(SqlDataReader reader)
        {
            DataTable dt = new DataTable();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                dt.Columns.Add(new DataColumn(((IDataRecord)reader).GetName(i), ((IDataRecord)reader).GetFieldType(i)));
            }
            return dt;
        }

        /// <summary>
        /// Gets the data set.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <returns>The representing DataSet</returns>
        public DataSet GetDataSet(string sql)
        {
            DataSet ds = new DataSet();
            OpenConnectionAsAppropriate();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            command.Parameters.Clear();

            dataAdapter = new SqlDataAdapter(command);

            try
            {

                dataAdapter.Fill(ds);

               
                command.Parameters.Clear();
                CloseConnectionAsAppropriate();
            }
            catch (Exception e)
            {
                throw(e);
            }
            return ds;
        }

        public void ExecuteNonQuery(string storedProcedureName, string PassthroughParams)
        {
            OpenConnectionAsAppropriate();
            command.CommandType = CommandType.Text;

            command.CommandText = "EXEC " + storedProcedureName + " " + PassthroughParams;
            command.Parameters.Clear();

            ShowSQLCall(command);
            command.ExecuteNonQuery();

           
            CloseConnectionAsAppropriate();
        }

     
        public void ExecuteNonQuery(string storedProcedureName, Dictionary<string, object> parms)
        {
            List<SqlParameter> newParams = new List<SqlParameter>();
            foreach (var o in parms)
            {
                newParams.Add(new SqlParameter(o.Key.ToString(), o.Value));
            }

            ExecuteNonQuery(storedProcedureName, newParams);
        }

        public void ExecuteNonQuery(string storedProcedureName, List<SqlParameter> parameters)
        {
            long returnValue = -1;
            ExecuteNonQuery(storedProcedureName, parameters, ref returnValue);
        }

        /// <summary>
        /// Executes a SQL non query.
        /// </summary>
        /// <param name="storedProcedureName">Name of the stored procedure.</param>
        /// <param name="parameters">The parameters.</param>
        public void ExecuteNonQuery(string storedProcedureName, List<SqlParameter> parameters, ref long returnValue)
        {
            try
            {
                OpenConnectionAsAppropriate();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = storedProcedureName;
                command.Parameters.Clear();

                if (parameters != null)
                {
                    //command.Parameters.Add(parameters);
                    foreach (SqlParameter p in parameters)
                    {
                        command.Parameters.Add(p);
                    }
                    if (returnValue >= 0)
                    {
                        var param = new SqlParameter("ReturnValue", DBNull.Value);
                        param.Direction = ParameterDirection.ReturnValue;
                        command.Parameters.Add(param);
                    }
                }



                ShowSQLCall(command);
                command.ExecuteNonQuery();
                if (returnValue >= 0)
                    returnValue = (command.Parameters["ReturnValue"] == null ? 0 : Convert.ToInt32(command.Parameters["ReturnValue"].Value));

                command.Parameters.Clear();

            }
            catch (Exception ex)
            {
                if (!storedProcedureName.ToUpper().Contains("spInsertLog".ToUpper()))
                    throw(ex);
            }
            CloseConnectionAsAppropriate();
        }

        /// <summary>
        /// Executes a SQL non query.
        /// </summary>
        /// <param name="sqlCommand">The SQL command.</param>
        public void ExecuteNonQuery(string sqlCommand)
        {
            OpenConnectionAsAppropriate();
            command.CommandType = CommandType.Text;
            command.CommandText = sqlCommand;
            command.Parameters.Clear();

            command.ExecuteNonQuery();

         

            command.Parameters.Clear();

            CloseConnectionAsAppropriate();
        }

        /// <summary>
        /// Executes the scalar SQL call.
        /// </summary>
        /// <param name="storedProcedureName">Name of the stored procedure.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Scalar value</returns>
        public object ExecuteScalar(string storedProcedureName, List<SqlParameter> parameters)
        {
            object retVal = null;

            OpenConnectionAsAppropriate();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = storedProcedureName;
            command.Parameters.Clear();

            if (parameters != null)
            {
                //command.Parameters.Add(parameters);
                foreach (SqlParameter p in parameters)
                {
                    command.Parameters.Add(p);
                }
            }
           
            retVal = command.ExecuteScalar();
            command.Parameters.Clear();

            CloseConnectionAsAppropriate();

            return retVal;
        }


        /// <summary>
        /// Forces connection close, and kills all other resources.  WARNING: This does NOT do anything with transactions.
        /// </summary>
        public void Dispose()
        {
            CloseConnection();

            if (command != null)
            {
                command.Connection = null;
                command = null;
            }
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void CloseConnection()
        {
            if (connection != null)
            {
                connection.Close();
            }
        }


        #endregion

        /******************************************************************************************************/



        public static bool ValidateHasRows(DataSet ds, int TableCount = 1)
        {
            if (ds == null)
                return false;

            if (ds.Tables.Count < TableCount)
                return false;

            int tableCounter = 0;
            bool hasDataInEachTable = true;
            foreach (DataTable table in ds.Tables)
            {
                if (table.Rows.Count == 0)
                {
                    hasDataInEachTable = false;
                    break;
                }
                tableCounter++;
                if (tableCounter >= TableCount)
                    break;
            }

            return hasDataInEachTable;
        }

        public static void ExecuteNonquery(string v, List<SqlParameter> list)
        {

            DBLayer dbLayer= new DBLayer();
            dbLayer.ExecuteNonQuery(v, list);
            dbLayer.Dispose();
        }
        public static void ExecuteNonquery(string sql)
        {
            DBLayer dbLayer= new DBLayer();
            dbLayer.ExecuteNonQuery(sql);
            dbLayer.Dispose();

        }
    }

}