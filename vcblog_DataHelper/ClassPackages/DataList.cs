using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vcblog_DataHelper.ClassPackages
{
    public class DataList : INotifyPropertyChanged
    {
        private string num;
        private string title;
        private string dataType;
        public event PropertyChangedEventHandler PropertyChanged;

        public DataList(){}

        public DataList(string num, string title)
        {
            this.num = num;
            this.title = title;
        }

        public DataList(string num, string title,string dataType)
        {
            this.num = num;
            this.title = title;
            this.dataType = dataType;
        }

        public string Num
        {
            get
            {
                return num;
            }
            set
            {
                num = value;
                if (this.PropertyChanged != null)//激发事件
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Num"));
                }
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                if (this.PropertyChanged != null)//激发事件
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Title"));
                }
            }
        }

        public string DataType
        {
            get
            {
                return dataType;
            }
            set
            {
                dataType = value;
                if (this.PropertyChanged != null)//激发事件
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DataType"));
                }
            }
        }
    }
}
