using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace vcblog_DataHelper
{
    public sealed class MyDataHelper
    {
        string xmlFilePath = @"config\DataBase.xml";
        string connectionStr = "Data Source=";
        string connectSTR = "Data Source=";     //连接MySQL，不指定数据库
        string dataSourceIP = "";       //数据库连接地址
        string dataPort = "";           //端口
        string dataCharset = "";        //字符集编码
        string dataUserID = "";         //用户名
        string dataPassword = "";         //密码

        public static readonly MyDataHelper Instance = new MyDataHelper();

        public static string connectionString = "";             //数据库连接字符串
        private MySqlConnection connectionStay = null;       //数据库连接句柄
        private static XmlNode database = null;                     //数据库

        /// <summary>
        /// 批量操作每批次记录数
        /// </summary>
        public static int BatchSize = 2000;

        /// <summary>
        /// 超时时间
        /// </summary>
        public static int CommandTimeOut = 600;

        // 用于缓存参数的HASH表
        //private static Hashtable parmCache = Hashtable.Synchronized(new Hashtable());

        private MyDataHelper() { }

        /// <summary>
        /// 如果没有数据库则创建
        /// </summary>
        private void GetDataSource()
        {
            if (File.Exists(xmlFilePath))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlFilePath);
                XmlNode dataSource = doc.SelectSingleNode("/DataBaseSetting/Data-Source").FirstChild;
                XmlNode dataSourcePort = doc.SelectSingleNode("/DataBaseSetting/Port").FirstChild;
                dataSourceIP = dataSource.Value.ToString();     //数据库连接地址
                dataPort = dataSourcePort.Value.ToLower();      //端口
                connectionStr += dataSource.Value + ";Port=";
                connectionStr += dataSourcePort.Value + ";Database=";
                connectSTR += dataSource.Value + ";Port=";
                connectSTR += dataSourcePort.Value + ";User ID=";

                database = doc.SelectSingleNode("/DataBaseSetting/Database").FirstChild;
                connectionStr += database.Value + ";User ID=";

                XmlNode userID = doc.SelectSingleNode("/DataBaseSetting/User-ID").FirstChild;
                dataUserID = userID.Value.ToLower();            //用户名
                connectionStr += userID.Value + ";Password=";
                connectSTR += userID.Value + ";Password=";

                XmlNode Passwd = doc.SelectSingleNode("/DataBaseSetting/Password").FirstChild;
                dataPassword = Passwd.Value.ToString();         //密码
                connectionStr += Passwd.Value + ";Charset=";
                connectSTR += Passwd.Value + ";Charset=";

                XmlNode charset = doc.SelectSingleNode("/DataBaseSetting/Charset").FirstChild;
                dataCharset = charset.Value.ToString();         //字符集编码
                connectionStr += charset.Value;
                connectSTR += charset.Value;

                MyDataHelper.connectionString = connectSTR;
                string sqlDataBase = "create database if not exists " + database.Value + " DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;";
                try
                {
                    connectionStay = new MySqlConnection(connectSTR);
                    ExecuteNonQuery(sqlDataBase, CommandType.Text, null);
                    MyDataHelper.connectionString = connectionStr;       //设置连接字符串
                    connectionStay.Close();
                    connectionStay.Dispose();
                    connectionStay = null;
                }
                catch (Exception ex)
                {
                    MyDataHelper.connectionString = "";       //设置连接字符串
                    throw ex;
                }
            }
        }

        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        /// <param name="dataBase"></param>
        /// <returns></returns>
        public string GetConnectString(string dataBaseStr = "")
        {
            if (dataSourceIP.Equals(""))
            {
                GetDataSource();
            }
            
            if (dataBaseStr.Equals(""))
            {
                return "Data Source=" + dataSourceIP + ";Port=" + dataPort + ";User ID=" + dataUserID + ";Password=" + 
                    dataPassword + ";Charset=" + dataCharset;
            }
            else
            {
                return "Data Source=" + dataSourceIP + ";Database=" + dataBaseStr + ";Port=" + dataPort + ";User ID=" + dataUserID + 
                    ";Password=" + dataPassword + ";Charset=" + dataCharset;
            }
        }

        /// <summary>
        /// 显示数据库列表
        /// </summary>
        /// <returns></returns>
        public DataSet GetAllDataBase()
        {
            DataSet dataSet = null;
            string sql = "SHOW DATABASES; ";
            if (MyDataHelper.connectionString.Equals(""))
            {
                GetDataSource();
            }
            MyDataHelper.connectionString = GetConnectString();         //用于连接数据库，获取数据库列表
            if (!MyDataHelper.connectionString.Equals(""))
            {
                try
                {
                    connectionStay = new MySqlConnection(MyDataHelper.connectionString);
                    dataSet = GetDataSet(sql, CommandType.Text, null);
                }
                catch (Exception ex) { throw ex; }
                finally
                {
                    connectionStay.Close();
                    connectionStay.Dispose();
                    connectionStay = null;
                }
            }
            return dataSet;
        }

        /// <summary>
        /// 创建数据库，同时可以指定创建数据库时的参数
        /// </summary>
        /// <param name="dataBase"></param>
        /// <param name="parameters"></param>
        public void CreateDataBase(string dataBase, List<string> parameters)
        {
            if (MyDataHelper.connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!MyDataHelper.connectionString.Equals(""))
            {
                StringBuilder tableBuilder = new StringBuilder();
                tableBuilder.Append("create database if not exists " + dataBase);
                tableBuilder.Append(" DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci ");
                if (null != parameters)
                {
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        tableBuilder.Append("  " + parameters[i]);
                    }
                }
                tableBuilder.Append(" ;");

                try
                {
                    connectionStay = new MySqlConnection(MyDataHelper.connectionString);
                    ExecuteNonQuery(tableBuilder.ToString(), CommandType.Text, null);
                }
                catch (Exception ex) { throw ex; }
                finally
                {
                    connectionStay.Close();
                    connectionStay.Dispose();
                    connectionStay = null;
                }
            }
        }

        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="parameters">表中每一字段的类型</param>
        public void CreateTable(string tableName, List<string> parameters)
        {
            if (MyDataHelper.connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!MyDataHelper.connectionString.Equals(""))
            {
                StringBuilder tableBuilder = new StringBuilder();
                tableBuilder.Append("create table if not exists " + tableName);
                tableBuilder.Append(" (id int(4) not null primary key auto_increment ");

                if (null != parameters)
                {
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        tableBuilder.Append(", " + parameters[i]);
                    }
                }
                tableBuilder.Append(" );");

                try
                {
                    connectionStay = new MySqlConnection(MyDataHelper.connectionString);
                    ExecuteNonQuery(tableBuilder.ToString(), CommandType.Text, null);
                }
                catch (Exception ex) { throw ex; }
                finally
                {
                    connectionStay.Close();
                    connectionStay.Dispose();
                    connectionStay = null;
                }
            }
        }

        /// <summary>
        /// 根据sql直接执行
        /// </summary>
        /// <param name="sql"></param>
        public void ExecuteSQL(string sql)
        {
            if (MyDataHelper.connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!MyDataHelper.connectionString.Equals(""))
            {
                try
                {
                    connectionStay = new MySqlConnection(MyDataHelper.connectionString);
                    ExecuteNonQuery(sql, CommandType.Text, null);
                }
                catch (Exception ex) { throw ex; }
                finally
                {
                    connectionStay.Close();
                    connectionStay.Dispose();
                    connectionStay = null;
                }
            }
        }

        /// <summary>
        /// 根据sql直接执行，返回结果集
        /// </summary>
        /// <param name="sql"></param>
        public DataSet ExecuteSQLToDataSet(string sql)
        {
            DataSet dataSet = null;
            if (MyDataHelper.connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!MyDataHelper.connectionString.Equals(""))
            {
                try
                {
                    connectionStay = new MySqlConnection(MyDataHelper.connectionString);
                    dataSet = GetDataSet(sql, CommandType.Text, null);
                }
                catch (Exception ex) { throw ex; }
                finally
                {
                    connectionStay.Close();
                    connectionStay.Dispose();
                    connectionStay = null;
                }
            }
            return dataSet;
        }

        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="values"></param>
        public void Insert(string tableName, List<string> values)
        {
            string columns = "";
            if (MyDataHelper.connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!MyDataHelper.connectionString.Equals(""))
            {
                if (null != values && values.Count > 0)
                    try
                    {
                        List<string> dataTypes = new List<string>();
                        connectionStay = new MySqlConnection(MyDataHelper.connectionString);
                        //获取表的列名和数据类型
                        string[] dataType = GetDataType(tableName);
                        string[] columnName = GetColumnName(tableName);
                        //获取表的列名和数据类型，忽略主键ID
                        for (int i = 1; i < columnName.Length; i++)
                        {
                            columns += columnName[i] + " ,";
                            dataTypes.Add(dataType[i]);
                        }
                        columns = columns.Remove(columns.Length - 1, 1);

                        if (values.Count == dataTypes.Count)
                        {
                            StringBuilder strBuilder = new StringBuilder();
                            strBuilder.Append("INSERT INTO " + tableName + " (" + columns + " )");

                            strBuilder.Append(" VALUES ( ");

                            for (int i = 0; i < values.Count; i++)
                            {
                                if (dataTypes[i].Contains("int") || dataTypes[i].Contains("double"))
                                {
                                    strBuilder.Append(" " + values[i] + " ,");
                                }
                                else
                                    strBuilder.Append("'" + values[i] + "' ,");
                            }

                            strBuilder = strBuilder.Remove(strBuilder.Length - 1, 1);
                            strBuilder.Append(" );");

                            ExecuteNonQuery(strBuilder.ToString(), CommandType.Text, null);
                        }
                        else
                        {
                            throw new Exception("参数个数不一致！");
                        }
                    }
                    catch (Exception ex) { throw ex; }
                    finally
                    {
                        connectionStay.Close();
                        connectionStay.Dispose();
                        connectionStay = null;
                    }
            }
        }

        /// <summary>
        /// 插入记录，如果存在则更新
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="values"></param>
        public void InsertOrUpdate(string tableName, List<string> values)
        {
            if (MyDataHelper.connectionString.Equals(""))
            {
                GetDataSource();
            }
            if (!MyDataHelper.connectionString.Equals(""))
            {
                if (values != null && values.Count > 0)
                {
                    try
                    {
                        string columns = "";
                        connectionStay = new MySqlConnection(MyDataHelper.connectionString);
                        string[] dataTypes = GetDataType(tableName);
                        string[] columnName = GetColumnName(tableName);
                        //获取表的列名和数据类型                        
                        for (int i = 0; i < columnName.Length; i++)
                        {
                            columns += columnName[i] + " ,";
                        }
                        columns = columns.Remove(columns.Length - 1, 1);

                        StringBuilder strBuilder = new StringBuilder();
                        strBuilder.Append("INSERT INTO  " + tableName + " (" + columns + " )");

                        strBuilder.Append(" VALUES ( ");

                        for (int i = 0; i < values.Count; i++)
                        {
                            if (dataTypes[i].Contains("int") || dataTypes[i].Contains("double"))
                            {

                                strBuilder.Append(" " + values[i] + " ,");
                            }
                            else
                                strBuilder.Append("'" + values[i] + "' ,");
                        }

                        strBuilder = strBuilder.Remove(strBuilder.Length - 1, 1);
                        strBuilder.Append(" )  ON DUPLICATE KEY UPDATE ");

                        for (int i = 0; i < values.Count; i++)
                        {
                            if (dataTypes[i].Contains("int") || dataTypes[i].Contains("double"))
                            {
                                strBuilder.Append(columnName[i] + "=" + values[i] + " ,");
                            }
                            else
                                strBuilder.Append(columnName[i] + "='" + values[i] + "' ,");
                        }

                        strBuilder = strBuilder.Remove(strBuilder.Length - 1, 1);
                        strBuilder.Append(";");
                        ExecuteNonQuery(strBuilder.ToString(), CommandType.Text, null);
                    }

                    catch (Exception ex) { throw ex; }
                    finally
                    {
                        connectionStay.Close();
                        connectionStay.Dispose();
                        connectionStay = null;
                    }
                }
            }
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="tableName">查询的表名，不可空</param>
        /// <param name="parameters">查询的列，为 null 表示返回所有列</param>
        /// <param name="whereName">筛选条件，可空</param>
        /// <param name="whereValue">筛选值，可空</param>
        /// <returns></returns>
        public DataSet Query(string tableName, List<string> parameters, string whereName, string whereValue)
        {
            DataSet dataSet = null;
            string sql = "";
            if (MyDataHelper.connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!MyDataHelper.connectionString.Equals(""))
            {
                try
                {
                    connectionStay = new MySqlConnection(MyDataHelper.connectionString);
                    if (!tableName.Equals(""))
                    {
                        if (null == parameters || parameters.Count <= 0)
                        {
                            //返回所有列
                            if (null == whereName || whereName.Equals(""))
                            {
                                sql = "select * from " + tableName + " ;";
                                dataSet = GetDataSet(sql, CommandType.Text, null);
                            }
                            else
                            {
                                string dataType = GetColumnNameDataType(tableName, whereName);
                                if (dataType.Contains("int") || dataType.Contains("double"))
                                {
                                    sql = "select * from " + tableName + " where " + whereName + " = " + whereValue + ";";
                                }
                                else
                                {
                                    sql = "select * from " + tableName + " where " + whereName + " = '" + whereValue + "';";
                                }

                                dataSet = GetDataSet(sql, CommandType.Text, null);
                            }
                        }
                        else
                        {
                            string dataType = GetColumnNameDataType(tableName, whereName);
                            sql = "select ";
                            for (int i = 0; i < parameters.Count; i++)
                            {
                                sql += parameters[i] + " ,";
                            }
                            sql = sql.Remove(sql.Length - 1, 1);

                            if (dataType.Contains("int") || dataType.Contains("double"))
                            {
                                sql += " from " + tableName + " where " + whereName + " = " + whereValue + ";";
                            }
                            else
                            {
                                sql += " from " + tableName + " where " + whereName + " = '" + whereValue + "';";
                            }

                            dataSet = GetDataSet(sql, CommandType.Text, null);
                        }
                    }
                }
                catch (Exception ex) { throw ex; }
                finally
                {
                    connectionStay.Close();
                    connectionStay.Dispose();
                    connectionStay = null;
                }
            }
            return dataSet;
        }

        public string GetColumnNameDataType(string tableName, string columnName)
        {
            string temp = "";
            bool flag = false;
            for (int i = 0; i < MysqlDataTools.listSQL.Count; i++)
            {
                if (MysqlDataTools.listSQL[i].tableName.Equals(tableName))
                {
                    for (int j = 0; j < MysqlDataTools.listSQL[i].columnName.Length; j++)
                    {
                        if (MysqlDataTools.listSQL[i].columnName[j].Equals(columnName))
                        {
                            temp = MysqlDataTools.listSQL[i].dataTypes[j];
                            flag = true;
                            break;
                        }
                    }
                }
                if (flag)
                    break;
            }
            return temp;
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="whereName">删除的列名</param>
        /// <param name="whereValue">删除的值</param>
        public void Delete(string tableName, string whereName, string whereValue)
        {
            if (MyDataHelper.connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!MyDataHelper.connectionString.Equals(""))
            {
                try
                {
                    connectionStay = new MySqlConnection(MyDataHelper.connectionString);
                    string dataType = GetColumnNameDataType(tableName, whereName);
                    string sql = "";

                    if (dataType.Contains("int") || dataType.Contains("double"))
                    {
                        sql = "delete from " + tableName + " where " + whereName + " = " + whereValue + ";";
                    }
                    else
                    {
                        sql = "delete from " + tableName + " where " + whereName + " = '" + whereValue + "';";
                    }

                    ExecuteNonQuery(sql, CommandType.Text, null);
                }
                catch (Exception ex) { throw ex; }
                finally
                {
                    connectionStay.Close();
                    connectionStay.Dispose();
                    connectionStay = null;
                }
            }
        }

        /// <summary>
        /// 修改数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="parameters">表的列名</param>
        /// <param name="values">更改的值</param>
        public void Update(string tableName, List<string> parameters, List<string> values, string whereName, string whereValue)
        {
            if (MyDataHelper.connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!MyDataHelper.connectionString.Equals(""))
            {
                if (null != parameters && parameters.Count > 0)
                {
                    try
                    {
                        connectionStay = new MySqlConnection(MyDataHelper.connectionString);
                        List<string> dataTypes = new List<string>();
                        //获取表的列名和数据类型
                        string[] dataType = GetDataType(tableName);
                        string[] columnName = GetColumnName(tableName);
                        //获取表的列名和数据类型，忽略主键ID
                        for (int i = 1; i < columnName.Length; i++)
                        {
                            dataTypes.Add(dataType[i]);
                        }

                        if (values != null)
                        {
                            StringBuilder strBuilder = new StringBuilder();
                            strBuilder.Append("UPDATE " + tableName + " SET ");

                            for (int i = 0; i < values.Count; i++)
                            {
                                if (dataTypes[i].Contains("int") || dataTypes[i].Contains("double"))
                                {
                                    strBuilder.Append(parameters[i] + "=" + values[i] + " , ");
                                }
                                else
                                    strBuilder.Append(parameters[i] + "='" + values[i] + "' ,");

                            }

                            strBuilder = strBuilder.Remove(strBuilder.Length - 1, 1);

                            //获取表的数据类型
                            string type = GetColumnNameDataType(tableName, whereName);
                            strBuilder.Append(" where " + whereName + " = ");
                            if (type.Contains("int") || type.Contains("double"))
                            {
                                strBuilder.Append(" " + whereValue + " ; ");
                            }
                            else
                                strBuilder.Append(" '" + whereValue + "'; ");

                            ExecuteNonQuery(strBuilder.ToString(), CommandType.Text, null);
                        }
                    }
                    catch (Exception ex) { throw ex; }
                    finally
                    {
                        connectionStay.Close();
                        connectionStay.Dispose();
                        connectionStay = null;
                    }
                }
            }
        }

        public string[] GetDataType(string tableName)
        {
            int i = 0;
            for (; i < MysqlDataTools.listSQL.Count; i++)
            {
                if (MysqlDataTools.listSQL[i].tableName.Equals(tableName, StringComparison.CurrentCultureIgnoreCase))
                {
                    break;
                }
            }
            if (i < MysqlDataTools.listSQL.Count)
                return MysqlDataTools.listSQL[i].dataTypes;
            else return null;
        }

        public string[] GetColumnName(string tableName)
        {
            int i = 0;
            for (; i < MysqlDataTools.listSQL.Count; i++)
            {
                if (MysqlDataTools.listSQL[i].tableName.Equals(tableName, StringComparison.CurrentCultureIgnoreCase))
                {
                    break;
                }
            }
            if (i < MysqlDataTools.listSQL.Count)
                return MysqlDataTools.listSQL[i].columnName;
            else return null;
        }

        /// <summary>
        /// 用执行的数据库连接执行一个返回数据集的sql命令
        /// </summary>
        /// <remarks>
        /// 举例:
        ///  MySqlDataReader r = ExecuteReader(connString, CommandType.StoredProcedure, "PublishOrders", new MySqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="cmdType">命令类型(存储过程, 文本, 等等)</param>
        /// <param name="cmdText">存储过程名称或者sql命令语句</param>
        /// <param name="commandParameters">执行命令所用参数的集合</param>
        /// <returns>包含结果的读取器</returns>
        private MySqlDataReader ExecuteReader(string cmdText, CommandType cmdType = CommandType.Text, params MySqlParameter[] commandParameters)
        {
            //创建一个MySqlCommand对象
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader = null;

            if (connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!connectionString.Equals(""))
            {
                try
                {
                    PrepareCommand(cmd, connectionStay, null, cmdType, cmdText, commandParameters);
                    //调用 MySqlCommand  的 ExecuteReader 方法
                    reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    //清除参数
                    cmd.Parameters.Clear();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return reader;
        }

        /// <summary>
        /// 用指定的数据库连接字符串执行一个命令并返回一个数据集的第一列
        /// </summary>
        /// <remarks>
        ///例如:
        ///  Object obj = ExecuteScalar(connString, CommandType.StoredProcedure, "PublishOrders", new MySqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="cmdType">命令类型(存储过程, 文本, 等等)</param>
        /// <param name="cmdText">存储过程名称或者sql命令语句</param>
        /// <param name="commandParameters">执行命令所用参数的集合</param>
        /// <returns>用 Convert.To{Type}把类型转换为想要的 </returns>
        private object ExecuteScalar(string cmdText, CommandType cmdType = CommandType.Text, params MySqlParameter[] commandParameters)
        {
            if (connectionString.Equals(""))
            {
                GetDataSource();
            }

            object val = null;

            if (!connectionString.Equals(""))
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand();
                    PrepareCommand(cmd, connectionStay, null, cmdType, cmdText, commandParameters);
                    val = cmd.ExecuteScalar();
                    cmd.Parameters.Clear();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return val;
        }


        /// <summary>
        ///  给定连接的数据库用假设参数执行一个sql命令（不返回数据集）
        /// </summary>
        /// <param name="cmdType">命令类型(存储过程, 文本, 等等)</param>
        /// <param name="cmdText">存储过程名称或者sql命令语句</param>
        /// <param name="commandParameters">执行命令所用参数的集合</param>
        /// <returns>执行命令所影响的行数</returns>
        private int ExecuteNonQuery(string cmdText, CommandType cmdType = CommandType.Text, params MySqlParameter[] commandParameters)
        {
            int val = 0;
            if (connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!connectionString.Equals(""))
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand();

                    PrepareCommand(cmd, connectionStay, null, cmdType, cmdText, commandParameters);
                    val = cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                    cmd.Dispose();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return val;
        }

        /// <summary>
        /// 获取所有表
        /// </summary>
        /// <returns></returns>
        public DataSet GetAllTables(string dataBaseStr)
        {
            DataSet ds = null;

            try
            {
                MyDataHelper.connectionString = GetConnectString(dataBaseStr);
                ds = ExecuteSQLToDataSet("SHOW TABLES;");
            }
            catch (Exception ex) { throw ex; }

            return ds;

            //DataSet ds = null;
            //string xmlFilePath = @"config\DataBase.xml";
            //if (File.Exists(xmlFilePath))
            //{
            //    XmlDocument doc = new XmlDocument();
            //    doc.Load(xmlFilePath);
            //    XmlNode database = doc.SelectSingleNode("/DataBaseSetting/Database").FirstChild;

            //    string sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='" + database.Value + "' and table_type='base table'";

            //    try
            //    {
            //        ds = GetDataSet(sql, CommandType.Text, null);
            //    }
            //    catch (Exception ex) { throw ex; }
            //}
            //return ds;
        }

        /// <summary>
        /// 获取表中的所有字段
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public List<string> GetColumnNameFromSQL(string tableName)
        {
            List<string> columnNames = new List<string>();
            if (connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!connectionString.Equals(""))
            {
                try
                {
                    connectionStay = new MySqlConnection(MyDataHelper.connectionString);
                    //获取表的列名和数据类型
                    string str = "SELECT column_name FROM information_schema.columns WHERE table_name='" + tableName + "' " +
                        " AND table_schema = '" + database.Value + "';";
                    MySqlDataReader reader = ExecuteReader(str, CommandType.Text, null);
                    while (reader.Read())
                    {
                        columnNames.Add(reader[0].ToString());
                    }
                    reader.Close();
                }
                catch (Exception ex) { throw ex; }
                finally
                {
                    connectionStay.Close();
                    connectionStay.Dispose();
                    connectionStay = null;
                }
            }
            return columnNames;
        }

        /// <summary>
        /// 返回DataSet
        /// </summary>
        /// <param name="cmdType">命令类型(存储过程, 文本, 等等)</param>
        /// <param name="cmdText">存储过程名称或者sql命令语句</param>
        /// <param name="commandParameters">执行命令所用参数的集合</param>
        /// <returns></returns>
        private DataSet GetDataSet(string cmdText, CommandType cmdType = CommandType.Text, params MySqlParameter[] commandParameters)
        {
            DataSet ds = null;
            if (connectionString.Equals(""))
            {
                GetDataSource();
            }

            if (!connectionString.Equals(""))
            {
                try
                {
                    ds = new DataSet();
                    MySqlCommand cmd = new MySqlCommand();
                    PrepareCommand(cmd, connectionStay, null, cmdType, cmdText, commandParameters);
                    MySqlDataAdapter adapter = new MySqlDataAdapter();
                    adapter.SelectCommand = cmd;
                    adapter.Fill(ds);
                    cmd.Parameters.Clear();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return ds;
        }

        /// <summary>
        /// 准备执行一个命令
        /// </summary>
        /// <param name="cmd">sql命令</param>
        /// <param name="conn">OleDb连接</param>
        /// <param name="trans">OleDb事务</param>
        /// <param name="cmdType">命令类型例如 存储过程或者文本</param>
        /// <param name="cmdText">命令文本,例如:Select * from Products</param>
        /// <param name="cmdParms">执行命令的参数</param>
        private void PrepareCommand(MySqlCommand cmd, MySqlConnection conn, MySqlTransaction trans, CommandType cmdType, string cmdText, MySqlParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = cmdType;
            cmd.CommandTimeout = 240;
            if (cmdParms != null)
            {
                foreach (MySqlParameter parameter in cmdParms)
                {
                    if ((parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.Input) &&
                        (parameter.Value == null))
                    {
                        parameter.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(parameter);
                }
            }
        }

        #region 批量操作

        /// <summary>
        ///使用MySqlDataAdapter批量更新数据
        /// </summary>
        /// <param name="table">数据表</param>
        public void BatchUpdate(DataTable table)
        {
            if (connectionString.Equals(""))
            {
                GetDataSource();
            }
            MySqlTransaction transaction = null;
            if (!connectionString.Equals(""))
            {
                try
                {
                    MySqlCommand command = connectionStay.CreateCommand();
                    command.CommandTimeout = CommandTimeOut;
                    command.CommandType = CommandType.Text;
                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    MySqlCommandBuilder commandBulider = new MySqlCommandBuilder(adapter);
                    commandBulider.ConflictOption = ConflictOption.OverwriteChanges;

                    connectionStay.Open();
                    transaction = connectionStay.BeginTransaction();
                    //设置批量更新的每次处理条数
                    adapter.UpdateBatchSize = BatchSize;
                    //设置事物
                    adapter.SelectCommand.Transaction = transaction;

                    if (table.ExtendedProperties["SQL"] != null)
                    {
                        adapter.SelectCommand.CommandText = table.ExtendedProperties["SQL"].ToString();
                    }
                    adapter.Update(table);
                    transaction.Commit();/////提交事务
                }
                catch (MySqlException ex)
                {
                    if (transaction != null) transaction.Rollback();
                    throw ex;
                }
            }
        }

        /// <summary>
        ///大批量数据插入,返回成功插入行数
        /// </summary>
        /// <param name="table">数据表</param>
        /// <returns>返回成功插入行数</returns>
        public int BulkInsert(DataTable table)
        {
            if (connectionString.Equals(""))
            {
                GetDataSource();
            }
            MySqlTransaction transaction = null;
            int insertCount = 0;
            if (!connectionString.Equals(""))
            {
                try
                {
                    if (string.IsNullOrEmpty(table.TableName)) throw new Exception("请给DataTable的TableName属性附上表名称");
                    if (table.Rows.Count == 0) return 0;

                    string tmpPath = Path.GetTempFileName();
                    string csv = DataTableToCsv(table);
                    File.WriteAllText(tmpPath, csv);
                    //connectionStay.Open();
                    transaction = connectionStay.BeginTransaction();
                    MySqlBulkLoader bulk = new MySqlBulkLoader(connectionStay)
                    {
                        FieldTerminator = ",",
                        FieldQuotationCharacter = '"',
                        EscapeCharacter = '"',
                        LineTerminator = "\r\n",
                        FileName = tmpPath,
                        NumberOfLinesToSkip = 0,
                        TableName = table.TableName,
                    };
                    bulk.Columns.AddRange(table.Columns.Cast<DataColumn>().Select(colum => colum.ColumnName).ToList());
                    insertCount = bulk.Load();
                    transaction.Commit();
                    File.Delete(tmpPath);
                }
                catch (Exception ex)
                {
                    if (transaction != null) transaction.Rollback();
                    throw ex;
                }
            }
            return insertCount;
        }

        /// <summary>
        ///将DataTable转换为标准的CSV
        /// </summary>
        /// <param name="table">数据表</param>
        /// <returns>返回标准的CSV</returns>
        public string DataTableToCsv(DataTable table)
        {
            //以半角逗号（即,）作分隔符，列为空也要表达其存在。
            //列内容如存在半角逗号（即,）则用半角引号（即""）将该字段值包含起来。
            //列内容如存在半角引号（即"）则应替换成半角双引号（""）转义，并用半角引号（即""）将该字段值包含起来。
            StringBuilder sb = new StringBuilder();
            DataColumn colum;
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    colum = table.Columns[i];
                    if (i != 0) sb.Append(",");
                    if (colum.DataType == typeof(string) && row[colum].ToString().Contains(","))
                    {
                        sb.Append("\"" + row[colum].ToString().Replace("\"", "\"\"") + "\"");
                    }
                    else sb.Append(row[colum].ToString());
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion
    }
}
