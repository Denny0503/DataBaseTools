using System;
using System.Collections.Generic;
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

namespace DataBaseTools
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        MysqlDataTools mySQLDataTools = null;       //数据库连接助手

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
                            TablesListView.Items.Clear();
                            foreach (DataRow row in message.dataSet.Tables[0].Rows)
                            {
                                ListViewItem item = new ListViewItem();
                                item.Content = row[0].ToString();
                                TablesListView.Items.Add(item);
                            }
                            TablesListView.EndInit();
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
                        if(DataBaseList.Items.Count > 0)
                        {
                            DataBaseList.SelectedIndex = 0;
                            MessageBox.Show("读取数据库列表成功！", "提示");
                        }
                        break;
                    case "exception":
                        MessageBox.Show(message.message);
                        Status1.Content = message.message;
                        break;
                }
            }));
        }

        private void TablesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

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
