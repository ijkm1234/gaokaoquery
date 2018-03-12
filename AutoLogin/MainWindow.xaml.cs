using System;
using System.Collections.Generic;
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
using System.Net;
using System.IO;
using Microsoft.Win32;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Globalization;

namespace AutoLogin
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<StuInfo> StuList=new List<StuInfo>();
        private List<GradeInfo> GradeList=new List<GradeInfo>();
        public MainWindow()
        {
            InitializeComponent();
            StuTable.ItemsSource = StuList;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            RequestHelper helper = new RequestHelper();
            var username = Regex.Replace(UserName.Text, @"\s", "");
            var id= Regex.Replace(ID.Text, @"\s", "");
            string htmlString = helper.Request(username,id);
            TextShow.Text = (htmlString);
            Browser.NavigateToString(ConvertExtendedASCII(htmlString));
        }
        private static string ConvertExtendedASCII(string HTML)
        {
            StringBuilder sb = new StringBuilder();
            char[] s = HTML.ToCharArray();
            foreach (char c in s)
            {
                if (Convert.ToInt32(c) > 127)
                    sb.Append(string.Format("&#{0};", Convert.ToInt32(c)));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog1 = new OpenFileDialog
            {
                Filter = "文本文件|*.txt;*.csv",
                RestoreDirectory = true,
                FilterIndex = 1
            };
            openFileDialog1.FileOk += OpenFileDialog_FileOk;
            openFileDialog1.ShowDialog();
        }

        private void OpenFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            
            var fileDialog = (OpenFileDialog)sender;
            FileStream fileStream = new FileStream(fileDialog.FileName,FileMode.Open);
            StreamReader reader = new StreamReader(fileStream);
            string linestr = "";
            StuList.Clear();
            
            while (linestr != null) {
                linestr=reader.ReadLine();
                if (linestr != null)
                {
                    StuList.Add(new StringExtract().ExtractFileStr(linestr));
                }
            }
            
            reader.Close();
            fileStream.Close();
            StuTable.ItemsSource = null;
            StuTable.ItemsSource = StuList;
            
            
        }

        private void BatchQuery_Click(object sender, RoutedEventArgs e)
        {
            object obj = new object();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            GradeList.Clear();
            var stulist = StuList;
            var list = new ConcurrentBag<GradeInfo>();
            Parallel.ForEach(stulist,item=>
            {
                    RequestHelper helper = new RequestHelper();
                    string gradeHtmlString = helper.Request(item.UserName, item.ID);
                    var grade = new StringExtract().ExtractGradeStr(gradeHtmlString);
                    grade.Order = item.Order;
                    list.Add(grade);
            });
            stopwatch.Stop();
            GradeList = list.AsEnumerable().ToList();
            MessageBox.Show("耗时："+stopwatch.ElapsedMilliseconds.ToString()+"\n"+"查询条数："+GradeList.Count.ToString());
            GradeList.Sort((x, y) => x.Order.CompareTo(y.Order));
            GradeTable.ItemsSource = null;
            GradeTable.ItemsSource = GradeList;
    }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = "文本文件|*.txt|分隔值文件|*.csv",
                FilterIndex = 2,
                RestoreDirectory=true
            };
            saveFileDialog1.FileOk += SaveFileDialog1_FileOk;
            saveFileDialog1.ShowDialog();
        }

        private void SaveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            var saveFileDialog1 = (SaveFileDialog)sender;
            FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Create);
            StreamWriter writer = new StreamWriter(fs, Encoding.UTF8);
            foreach (var item in GradeList)
            {
               string str = item.Order+",";
                str += item.Name + ",";
                str += item.Total + ",";
                str += item.Ranking+",";
                str += item.School;
                writer.WriteLine(str);
            }
            writer.Close();
            fs.Close();

        }
    }

    public class RequestHelper
    {
        private CookieContainer Co { get; set; }
        public string Request(string userName, string id)
        {
            string code = GetVerificationCode(InRequest());
            string htmlString = OutRequest(userName, id, code);
            return htmlString;
        }
        private string InRequest()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://cx.ahzsks.cn/pugao/pgcj2017_in.php");
                request.Method = "GET";
                request.Host = "cx.ahzsks.cn";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:58.0) Gecko/20100101 Firefox/58.0";
                request.Accept = " text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.CookieContainer = new CookieContainer();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Co = request.CookieContainer;
                Stream stream = response.GetResponseStream();
                using (var reader = new StreamReader(stream, Encoding.GetEncoding("gb2312")))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        private string GetVerificationCode(string htmlString)
        {
            string code = htmlString.Substring(htmlString.IndexOf("name=\"yzm\" id=\"yzm\"  size=\"10\" maxlength=\"4\"/>") +46, 4);
            return code;

        }
        private string OutRequest(string userName, string id,string code)
        {
            try
            {
                string formData = string.Format("ksh={0}&sfzh={1}&yzm={2}",userName,id,code);
                byte[] form = Encoding.ASCII.GetBytes(formData);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://cx.ahzsks.cn/pugao/pgcj2017_out.php");
                request.Method = "POST";
                request.Host = "cx.ahzsks.cn";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:58.0) Gecko/20100101 Firefox/58.0";
                request.Accept = " text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.ContentLength = form.Length;
                request.ContentType = "application/x-www-form-urlencoded";
                request.CookieContainer = Co;

                using (Stream requeststream = request.GetRequestStream())
                {
                    requeststream.Write(form,0, form.Length);
                }
                
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                using (var reader = new StreamReader(stream, Encoding.GetEncoding("gb2312")))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {

                throw;
            }
        } 
    }
    public class StuInfo
    {
        public int Order { get; set; }
        public string UserName { get; set; }
        public string ID { get; set; }
    }

    public class GradeInfo
    {
        public int Order { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Ranking { get; set; }
        public string Total { get; set; }
        public string Yuwen { get; set; }
        public string Math { get; set; }
        public string English { get; set; }
        public string Lizong { get; set; }
        public string SubjectiveYuwen { get; set; }
        public string SubjectiveMath { get; set; }
        public string SubjectiveEnglish { get; set; }
        public string SubjectiveLizong { get; set; }
        public string NationalJiafen { get; set; }
        public string  RegionJiafen { get; set; }
        public string School { get; set; }
    }

    public class StringExtract
    {
        public StuInfo ExtractFileStr(string fileString)
        {
            int.TryParse(o, out order);
            var username = Regex.Replace(elementList[1], @"\s", "");
            return stu;
        }
        public GradeInfo ExtractGradeStr(string htmlString)
        {
            GradeInfo gradeInfo = new GradeInfo();
            var totalMatch = Regex.Match(htmlString, @"总分.*?(>(\d*\.?\d*)</td>.*?)>(\d*\.?\d*)</td>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);
            gradeInfo.Total = totalMatch.Groups[2].Value;
            gradeInfo.Ranking = totalMatch.Groups[3].Value;
            var name = Regex.Match(htmlString, @"姓名：.*?([\u4E00-\u9FA5]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);
            gradeInfo.Name = name.Groups[1].Value;
            var school = Regex.Match(htmlString, @"录取院校名称.*?([\u4E00-\u9FA5]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);
            gradeInfo.School = school.Groups[1].Value;
            return gradeInfo;
        }
        
    }
    

}

