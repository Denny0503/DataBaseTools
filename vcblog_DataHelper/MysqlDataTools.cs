using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace vcblog_DataHelper
{
    /// <summary>
    /// 数据表语句结构体
    /// </summary>
    public struct SQLStruct
    {
        public string tableName;
        public string[] columnName;
        public string[] dataTypes;
        public string unique;
    }

    /// <summary>
    /// 返回界面线程的消息类型
    /// </summary>
    public struct DataToolsUIMsg
    {
        public string type;
        public string message;
        public Exception ex;
        public DataSet dataSet;
        public List<string> valuesList;
    }

    /// <summary>
    /// 数据库操作类型，增、删、改、查、建表，创建数据库
    /// </summary>
    public enum DataBaseType
    {
        Insert,     //插入数据
                    //InsertNotSame,     //插入不同数据
        Delete,     //删除数据
        Update,     //更新数据
        Query,      //查询数据
        QueryLimit,      //查询数据，返回规定的结果数，即查找结果的前几行
        CreateTable,    //创建数据表
        CreateDataBase,     //创建数据库
        ExecuteSQL,         //执行sql语句
        ExecuteSQLToDataSet,         //执行sql语句，返回dataset结果集
        UpdateDataTable,    //从DataTable更新数据
        InsertDataTable,    //从DataTable插入数据
        GetColumnName,      //获取表中的字段
        InsertOrUpdate,     //插入记录，存在则更新，不能忽略传递ID主键
        ShowTables,         //显示数据库表
        ShowDataBase,       //显示数据库列表
    };

    /// <summary>
    /// 队列处理消息类型
    /// </summary>
    public struct DataToolsQueueMsg
    {
        public DataBaseType type;        //操作类型
        public List<string> values;        //数据集合
        public List<string> parameters;    //列集合
        public string message;             //操作的表名或者数据库名称
        public string whereName;           //筛选条件
        public string whereValue;          //筛选值
        public string sql;                 //执行sql语句
        public DataTable dataTable;        //从DataTable更新数据
        public string marks;               //获取的数据消息标记，用于区分调用者
    }

    public class MysqlDataTools
    {
        public static readonly MysqlDataTools Instance = new MysqlDataTools();      //数据操作工具类，单例模式
        private MyDataHelper myDataHelper = MyDataHelper.Instance;      //自定义数据库操作类
        private static Queue<DataToolsQueueMsg> ListQueue = new Queue<DataToolsQueueMsg>();          //处理队列
        private static bool STOP_THREAD = true;   //停止队列处理线程
        private static bool Queue_THREAD = false;   //队列消息处理线程是否已开启
        private string filePath = @"Configure/DataBaseInit.txt";         //数据表文件
        public static List<SQLStruct> listSQL = new List<SQLStruct>();           //数据库建表语句

        /// <summary>
        /// 定义委托事件原型，用于通知消息
        /// </summary>
        /// <param name="message"></param>
        public delegate void SendMessageHandler(DataToolsUIMsg message);
        public event SendMessageHandler SendMessageEvent;

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="message"></param>
        protected void OnUiShowMessage(DataToolsUIMsg message)
        {
            SendMessageEvent?.Invoke(message);
        }

        private MysqlDataTools() { }

        /// <summary>
        /// 从文件初始化建表语句
        /// </summary>
        public void InitFromFile()
        {
            if (File.Exists(filePath))
            {
                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                StreamReader rs = new StreamReader(fs);

                try
                {
                    while (!rs.EndOfStream)
                    {
                        string temp = rs.ReadLine().Replace(" ", "");
                        if (!temp.Contains("!") && !temp.Equals(""))
                        {
                            SQLStruct sqlStruct = new SQLStruct();
                            sqlStruct.tableName = temp;

                            temp = rs.ReadLine().Replace(" ", ""); ;
                            string[] str = System.Text.RegularExpressions.Regex.Split(temp, @"[;]+",
                                   System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            sqlStruct.columnName = new string[str.Length];
                            for (int i = 0; i < str.Length; i++)
                            {
                                sqlStruct.columnName[i] = str[i];
                            }

                            temp = rs.ReadLine().Replace(" ", ""); ;
                            string[] str2 = System.Text.RegularExpressions.Regex.Split(temp, @"[;]+",
                                   System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            sqlStruct.dataTypes = new string[str2.Length];
                            for (int i = 0; i < str2.Length; i++)
                            {
                                sqlStruct.dataTypes[i] = str2[i];
                            }

                            temp = rs.ReadLine().Replace(" ", ""); ;
                            if (!temp.Contains("*"))
                            {
                                sqlStruct.unique = temp;
                            }
                            else
                                sqlStruct.unique = "";

                            listSQL.Add(sqlStruct);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DataToolsUIMsg msg = new DataToolsUIMsg();
                    msg.type = "exception";
                    msg.message = ex.ToString();
                    OnUiShowMessage(msg);
                }
                finally
                {
                    rs.Close();
                    fs.Close();
                }
            }
        }

        /// <summary>
        /// 创建数据库表
        /// </summary>
        private void CreateTables()
        {
            try
            {
                InitFromFile();
                if (listSQL.Count > 0)
                {
                    for (int i = 0; i < listSQL.Count; i++)
                    {
                        string sql = "create table if not exists " + listSQL[i].tableName + "( " +
                            listSQL[i].columnName[0] + " " + listSQL[i].dataTypes[0] + " not null primary key auto_increment,";
                        for (int j = 1; j < listSQL[i].columnName.Length; j++)
                        {
                            sql += listSQL[i].columnName[j] + " " + listSQL[i].dataTypes[j] + " ,";
                        }

                        if (!listSQL[i].unique.Equals(""))
                        {
                            sql += listSQL[i].unique + " );";
                        }
                        else
                        {
                            sql = sql.Remove(sql.Length - 1, 1);
                            sql += " );";
                        }

                        DataToolsQueueMsg queuein_SQL = new DataToolsQueueMsg();
                        queuein_SQL.type = DataBaseType.ExecuteSQL;
                        queuein_SQL.sql = sql;
                        ListQueue.Enqueue(queuein_SQL);
                    }
                }
            }
            catch (Exception ex)
            {
                DataToolsUIMsg msg = new DataToolsUIMsg();
                msg.type = "exception";
                msg.ex = ex;
                OnUiShowMessage(msg);
            }
        }

        //private DataSet getDataSet()
        //{
        //    //string sql = "SELECT column_name,data_type FROM information_schema.columns WHERE table_name='History_tb' AND table_schema = 'elplatingdata';";
        //    //return myDataHelper.GetDataSet(sql, CommandType.Text, null);
        //}

        /// <summary>
        /// 启动队列处理线程，不断监听和处理队列内的数据
        /// </summary>
        public void Start()
        {
            if (!Queue_THREAD)
            {
                //启动队列处理线程
                STOP_THREAD = false;
                Thread threadQueue = new Thread(ThreadReadQueue);
                threadQueue.IsBackground = true;
                threadQueue.Start();
                Queue_THREAD = true;
            }
        }

        /// <summary>
        /// 停止队列处理线程，若队列消息不为空，不能停止线程
        /// </summary>
        /// <returns>返回是否成功的标记</returns>
        public bool Stop()
        {
            if (ListQueue.Count > 0)
            {
                return false;
            }
            else
            {
                STOP_THREAD = true;         //停止队列消息处理线程
                Queue_THREAD = false;       //队列消息处理线程已关闭
                return true;
            }
        }

        /// <summary>
        /// 添加数据进队列
        /// </summary>
        /// <param name="queueinfo"></param>
        public void Enqueue(DataToolsQueueMsg queueinfo)
        {
            ListQueue.Enqueue(queueinfo);
        }

        /// <summary>
        /// 队列处理线程，不停进行队列处理，直到通知线程结束
        /// </summary>
        private void ThreadReadQueue()
        {
            while (!STOP_THREAD)
            {
                if (ListQueue.Count > 0)
                {
                    try
                    {
                        //从队列中取出  
                        DataToolsQueueMsg queueinfo = ListQueue.Dequeue();
                        switch (queueinfo.type)
                        {
                            case DataBaseType.Insert:
                                myDataHelper.Insert(queueinfo.message, queueinfo.values);
                                break;
                            case DataBaseType.Delete:
                                myDataHelper.Delete(queueinfo.message, queueinfo.whereName, queueinfo.whereValue);
                                break;
                            case DataBaseType.Update:
                                myDataHelper.Update(queueinfo.message, queueinfo.parameters, queueinfo.values, queueinfo.whereName, queueinfo.whereValue);
                                break;
                            case DataBaseType.Query:
                                DataToolsUIMsg msg = new DataToolsUIMsg();
                                msg.dataSet = myDataHelper.Query(queueinfo.message, queueinfo.parameters, queueinfo.whereName, queueinfo.whereValue);
                                msg.type = queueinfo.marks;
                                OnUiShowMessage(msg);
                                break;
                            case DataBaseType.CreateTable:
                                myDataHelper.CreateTable(queueinfo.message, queueinfo.parameters);
                                break;
                            case DataBaseType.CreateDataBase:
                                myDataHelper.CreateDataBase(queueinfo.message, queueinfo.parameters);
                                break;
                            case DataBaseType.ExecuteSQL:
                                myDataHelper.ExecuteSQL(queueinfo.sql);
                                break;
                            case DataBaseType.ExecuteSQLToDataSet:
                                DataToolsUIMsg resultExecute = new DataToolsUIMsg();
                                resultExecute.type = queueinfo.marks;
                                resultExecute.dataSet = myDataHelper.ExecuteSQLToDataSet(queueinfo.sql);
                                OnUiShowMessage(resultExecute);
                                break;
                            case DataBaseType.UpdateDataTable:
                                myDataHelper.BatchUpdate(queueinfo.dataTable);
                                break;
                            case DataBaseType.InsertDataTable:
                                myDataHelper.BulkInsert(queueinfo.dataTable);
                                break;
                            case DataBaseType.GetColumnName:
                                //获取表中的字段
                                //MySQLToolsUIMsg resultColumn = new MySQLToolsUIMsg();
                                //resultColumn.valuesList = myDataHelper.GetColumnName(queueinfo.message);
                                //resultColumn.type = queueinfo.marks;
                                //OnUiShowMessage(resultColumn);
                                break;
                            case DataBaseType.InsertOrUpdate:
                                myDataHelper.InsertOrUpdate(queueinfo.message, queueinfo.values);
                                break;
                            case DataBaseType.ShowTables:
                                DataToolsUIMsg resultTables = new DataToolsUIMsg();
                                resultTables.type = queueinfo.marks;
                                resultTables.dataSet = myDataHelper.GetAllTables(queueinfo.whereName);
                                OnUiShowMessage(resultTables);
                                break;
                            case DataBaseType.ShowDataBase:
                                //显示所有的数据库列表
                                DataToolsUIMsg resultDatas = new DataToolsUIMsg();
                                resultDatas.type = queueinfo.marks;
                                resultDatas.dataSet = myDataHelper.GetAllDataBase();
                                OnUiShowMessage(resultDatas);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        DataToolsUIMsg msg = new DataToolsUIMsg();
                        msg.type = "exception";
                        msg.message = ex.ToString();
                        OnUiShowMessage(msg);
                    }
                }
            }
        }
    }
}
