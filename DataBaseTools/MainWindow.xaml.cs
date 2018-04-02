using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using vcblog_DataHelper;
using vcblog_DataHelper.ClassPackages;

namespace DataBaseTools
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        MysqlDataTools mySQLDataTools = null;       //数据库连接助手
        List<DataList> tableLists = new List<DataList>();
        List<DataList> columnsLists = new List<DataList>();

        public MainWindow()
        {
            InitializeComponent();

            //开启数据库管理任务，把数据库操作添加到队列中进行统一管理
            mySQLDataTools = MysqlDataTools.Instance;
            mySQLDataTools.SendMessageEvent += UiLoginMessage;
            mySQLDataTools.Start();

        }

        private void UiLoginMessage(DataToolsUIMsg message)
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                switch (message.type)
                {
                    case "ShowTables":
                        //显示某个数据库的数据表
                        if (null != message.dataSet && message.dataSet.Tables[0].Rows.Count > 0)
                        {
                            TablesListView.BeginInit();
                            //TablesListView.Items.Clear();
                            tableLists.Clear();
                            int i = 0;
                            foreach (DataRow row in message.dataSet.Tables[0].Rows)
                            {
                                i++;
                                tableLists.Add(new DataList(i.ToString(), row[0].ToString()));
                            }
                            TablesListView.ItemsSource = tableLists;
                            TablesListView.EndInit();
                        }
                        break;
                    case "ShowTableColumns":
                        //显示某个数据库的数据表的所有字段
                        if (null != message.dataSet && message.dataSet.Tables[0].Rows.Count > 0)
                        {
                            ColumnsListView.BeginInit();
                            columnsLists.Clear();
                            int i = 0;
                            foreach (DataRow row in message.dataSet.Tables[0].Rows)
                            {
                                i++;
                                columnsLists.Add(new DataList(i.ToString(), row[0].ToString(), row[1].ToString()));
                            }
                            ColumnsListView.ItemsSource = columnsLists;
                            ColumnsListView.EndInit();
                        }
                        break;
                    case "ShowDataBases":
                        //显示所有数据库
                        if (null != message.dataSet && message.dataSet.Tables[0].Rows.Count > 0)
                        {
                            DataBaseList.BeginInit();
                            DataBaseList.Items.Clear();
                            foreach (DataRow row in message.dataSet.Tables[0].Rows)
                            {
                                DataBaseList.Items.Add(row[0].ToString());
                            }
                            DataBaseList.EndInit();
                        }
                        if (DataBaseList.Items.Count > 0)
                        {
                            DataBaseList.SelectedIndex = 0;
                        }
                        break;
                    case "ShowTableData":
                        //显示表中数据
                        if (null != message.dataSet && message.dataSet.Tables[0].Rows.Count > 0)
                        {
                            AllTableData.BeginInit();
                            AllTableData.ItemsSource = message.dataSet.Tables[0].DefaultView;
                            AllTableData.EndInit();
                        }
                        else
                        {
                            AllTableData.BeginInit();
                            AllTableData.ItemsSource = null;
                            AllTableData.EndInit();
                        }
                        break;
                    case "exception":
                        MessageBox.Show(message.message);
                        Status1.Content = message.message;
                        break;
                }
            }));
        }

        /// <summary>
        /// 选中表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TablesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesListView.SelectedIndex >= 0)
            {
                DataToolsQueueMsg msg = new DataToolsQueueMsg();
                msg.type = DataBaseType.ShowTableColumns;
                msg.whereName = DataBaseList.Text;
                msg.whereValue = tableLists[TablesListView.SelectedIndex].Title;
                msg.marks = "ShowTableColumns";
                mySQLDataTools.Enqueue(msg);

                //获取表中数据
                DataToolsQueueMsg msgData = new DataToolsQueueMsg();
                msgData.type = DataBaseType.ExecuteSQLFromDataBase;
                msgData.whereName = DataBaseList.Text; 
                msgData.sql = "select * from " + tableLists[TablesListView.SelectedIndex].Title + " LIMIT 100;";
                msgData.marks = "ShowTableData";
                mySQLDataTools.Enqueue(msgData);
            }
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            DataToolsQueueMsg msg = new DataToolsQueueMsg();
            msg.type = DataBaseType.ShowDataBase;
            msg.marks = "ShowDataBases";
            mySQLDataTools.Enqueue(msg);

        }

        private void DataBaseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataToolsQueueMsg msg = new DataToolsQueueMsg();
            msg.type = DataBaseType.ShowTables;
            msg.marks = "ShowTables";
            msg.whereName = DataBaseList.SelectedItem.ToString();
            mySQLDataTools.Enqueue(msg);
        }
    }
}
